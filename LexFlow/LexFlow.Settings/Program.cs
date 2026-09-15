using LexFlow.Service;
using LexFlow.Onboarding.Wizard;
using LexFlow.Profiles;
using LexFlow.Core;
using LexFlow.Core.Interfaces;
using LexFlow.Core.Models;
using Velopack;

namespace LexFlow.Settings;

static class Program
{
    private const string MutexName = @"Global\LexFlow_SingleInstance_Mutex";
    private const string MutexNameLocal = @"Local\LexFlow_SingleInstance_Mutex";
    private const string ShowSettingsEventName = @"Global\LexFlow_ShowSettings_Event";
    private const string ShowSettingsEventNameLocal = @"Local\LexFlow_ShowSettings_Event";

    private static LexFlowServiceComposer.CompositionResult? _composition;
    private static SystemTrayManager? _trayManager;
    private static SettingsForm? _settingsForm;
    private static KeyboardShortcutsForm? _shortcutsForm;
    private static CrashReporter? _crashReporter;
    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _showSettingsEvent;
    private static Thread? _showSettingsWaiter;
    private static volatile bool _shuttingDown;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Must be the first thing that runs: Velopack intercepts the install,
        // update, and uninstall hook arguments here and exits on its own.
        VelopackApp.Build().Run();

        if (!TryBecomeSingleInstance())
        {
            SignalExistingInstanceToShowSettings();
            return;
        }

        // Initialize crash reporter
        _crashReporter = new CrashReporter("LexFlow");

        // Check Group Policy for crash reporting setting
        var enableCrashReporting = CheckGroupPolicyCrashReporting();
        if (enableCrashReporting)
        {
            _crashReporter.SetupGlobalExceptionHandling();
        }

        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Build service composition using shared composer
            try
            {
                _composition = LexFlowServiceComposer.BuildAsync().GetAwaiter().GetResult();
                var lexFlowService = _composition.Service;
            }
            catch (Exception ex)
            {
                _crashReporter.LogCrash(ex, "Failed during composition", isTerminating: true);
                ShowCrashMessageBox("LexFlow failed to start during service composition.", ex);
                return;
            }

            // Check if this is first run (no profile saved or onboarding not completed)
            var isFirstRun = !(_composition.Storage.ExistsAsync(Profile.GetStorageKey(Profile.DefaultProfileId)).GetAwaiter().GetResult()) ||
                              !(_composition.Storage.ExistsAsync("onboarding_state").GetAwaiter().GetResult());

            if (isFirstRun)
            {
                // Show onboarding wizard
                try
                {
                    using var wizard = new OnboardingWizard(_composition.Storage, _composition.Profile);
                    var result = wizard.ShowDialog();

                    // If user cancels onboarding, exit the application
                    if (result != DialogResult.OK)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _crashReporter.LogCrash(ex, "Failed during onboarding", isTerminating: true);
                    ShowCrashMessageBox("LexFlow failed during the onboarding wizard.", ex);
                    return;
                }
            }

            _composition.Service.StartAsync().Wait();

            // Initialize system tray manager
            _trayManager = new SystemTrayManager();
            _trayManager.SetStatus(ServiceStatus.Active);
            _trayManager.SettingsRequested += OnSettingsRequested;
            _trayManager.ExitRequested += OnExitRequested;
            _trayManager.ToggleRequested += OnToggleRequested;
            _trayManager.KeyboardShortcutsRequested += OnKeyboardShortcutsRequested;
            _trayManager.UpdatesRequested += OnUpdatesRequested;
            _composition.KeyboardShortcutManager.ShortcutTriggered += OnShortcutTriggered;

            StartShowSettingsWaiter();
            StartUpdateChecker();

            // Show balloon tip to notify user that LexFlow is running
            _trayManager.ShowBalloonTip("LexFlow", "LexFlow is running in the background. Right-click the tray icon for options.", ToolTipIcon.Info);

            // Wire quick toggle to update tray status
            _composition.QuickToggleManager.ToggleStateChanged += (sender, isEnabled) =>
            {
                UpdateTrayStatus(isEnabled);
                _trayManager?.ShowStatusToast(isEnabled ? "LexFlow enabled" : "LexFlow disabled", isEnabled);
            };
            _composition.FocusTracker.ContextChanged += (_, _) =>
                UpdateTrayStatus(_composition.QuickToggleManager.IsEnabled);

            // Run application message loop (no main form, tray-resident)
            Application.Run();
        }
        catch (Exception ex)
        {
            _crashReporter.LogCrash(ex, "Failed during startup", isTerminating: true);
            ShowCrashMessageBox("LexFlow failed to start unexpectedly.", ex);
        }
        finally
        {
            ShutdownSingleInstance();
        }
    }

    private static bool TryBecomeSingleInstance()
    {
        try
        {
            _instanceMutex = new Mutex(true, MutexName, out var createdNew);
            if (!createdNew)
            {
                _instanceMutex.Dispose();
                _instanceMutex = null;
                return false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            _instanceMutex = new Mutex(true, MutexNameLocal, out var createdNew);
            if (!createdNew)
            {
                _instanceMutex.Dispose();
                _instanceMutex = null;
                return false;
            }
        }

        try
        {
            _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        }
        catch (UnauthorizedAccessException)
        {
            _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventNameLocal);
        }

        return true;
    }

    private static void SignalExistingInstanceToShowSettings()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ShowSettingsEventName, out var globalEvent))
            {
                using (globalEvent)
                {
                    globalEvent.Set();
                }

                return;
            }
        }
        catch
        {
            // Fall through to the local event name.
        }

        try
        {
            if (EventWaitHandle.TryOpenExisting(ShowSettingsEventNameLocal, out var localEvent))
            {
                using (localEvent)
                {
                    localEvent.Set();
                }
            }
        }
        catch
        {
            // Nothing we can do — the first instance is not listening.
        }
    }

    private static void StartShowSettingsWaiter()
    {
        if (_showSettingsEvent == null)
        {
            return;
        }

        _showSettingsWaiter = new Thread(() =>
        {
            while (!_shuttingDown)
            {
                try
                {
                    if (_showSettingsEvent.WaitOne(250) && !_shuttingDown)
                    {
                        _trayManager?.InvokeOnUiThread(() => OnSettingsRequested(null, EventArgs.Empty));
                    }
                }
                catch
                {
                    break;
                }
            }
        })
        {
            IsBackground = true,
            Name = "LexFlow.ShowSettingsWaiter"
        };
        _showSettingsWaiter.Start();
    }

    private static void ShutdownSingleInstance()
    {
        _shuttingDown = true;
        try
        {
            _showSettingsEvent?.Set();
        }
        catch
        {
            // Ignore — we are shutting down.
        }

        if (_showSettingsWaiter != null && _showSettingsWaiter.IsAlive)
        {
            _showSettingsWaiter.Join(500);
        }

        _showSettingsEvent?.Dispose();
        _showSettingsEvent = null;

        try
        {
            _instanceMutex?.ReleaseMutex();
        }
        catch
        {
            // Mutex may already be released.
        }

        _instanceMutex?.Dispose();
        _instanceMutex = null;
    }

    private static void ShowCrashMessageBox(string message, Exception ex)
    {
        var fullMessage = $"{message}\n\nException: {ex.GetType().Name}\n{ex.Message}";
        MessageBox.Show(
            fullMessage,
            "LexFlow Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void UpdateTrayStatus(bool isEnabled)
    {
        if (_trayManager == null || _composition == null)
        {
            return;
        }

        if (!isEnabled)
        {
            _trayManager.SetStatus(ServiceStatus.Inactive);
            return;
        }

        var app = _composition.FocusTracker.GetCurrentContext().ApplicationName;
        var overrides = AppCategoryMapper.ParseOverrides(_composition.Profile.GetSetting<List<string>>("AppCategoryOverrides", []));
        var tone = AppCategoryMapper.Resolve(app, overrides);
        _trayManager.SetStatus(ServiceStatus.Active, $"Tone: {tone}");
    }

    private static void OnShortcutTriggered(object? sender, LexFlow.Input.ShortcutTriggeredEventArgs e)
    {
        if (e.ShortcutName != "OpenSettings")
        {
            return;
        }

        // Swallow Ctrl+Shift+S so the foreground app does not also receive it.
        e.EventArgs.Handled = true;
        _trayManager?.InvokeOnUiThread(() => OnSettingsRequested(null, EventArgs.Empty));
    }

    private static void OnSettingsRequested(object? sender, EventArgs e)
    {
        try
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_composition!.Profile, _composition.PrivacyGuard, _composition.Storage, _composition.ThemeManager, _composition.SuggestionPipeline, _composition.SuggestionOverlay, _composition.PersonalizationManager, _composition.TextExpansionManager, _composition.EditConfirmation, provider => LexFlowServiceComposer.ApplyAiProvider(_composition, provider), _composition.CloudAiLog);
                _settingsForm.FormClosed += (s, args) => _settingsForm = null;
            }

            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }

            _settingsForm.Show();
            _settingsForm.BringToFront();
            _settingsForm.TopMost = true;
            _settingsForm.Activate();
            _settingsForm.TopMost = false;

            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LexFlow");
                Directory.CreateDirectory(dir);
                File.AppendAllText(
                    Path.Combine(dir, "placement.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} SETTINGS_OPEN visible={_settingsForm.Visible} disposed={_settingsForm.IsDisposed}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never throw.
            }
        }
        catch (Exception ex)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LexFlow");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "placement.log"), $"{DateTime.Now:HH:mm:ss.fff} SETTINGS_OPEN_FAILED {ex}\n");
            }
            catch
            {
                // Logging must never throw.
            }

            using var owner = new Form { TopMost = true, ShowInTaskbar = false };
            owner.Show();
            MessageBox.Show(
                owner,
                $"Could not open Settings.\n\n{ex.GetType().Name}: {ex.Message}",
                "LexFlow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void OnExitRequested(object? sender, EventArgs e)
    {
        ShutdownGracefully();
    }

    /// <summary>
    /// Stops the service and removes the tray icon before letting the message loop
    /// unwind. The Velopack update path exits through here too, so this must stay
    /// reachable without going through the tray menu.
    /// </summary>
    private static void ShutdownGracefully()
    {
        _composition?.Service.StopAsync().Wait();
        _trayManager?.Dispose();
        Application.Exit();
    }

    private static void OnToggleRequested(object? sender, EventArgs e)
    {
        _composition?.QuickToggleManager.Toggle();
    }

    private static void OnKeyboardShortcutsRequested(object? sender, EventArgs e)
    {
        if (_shortcutsForm == null || _shortcutsForm.IsDisposed)
        {
            _shortcutsForm = new KeyboardShortcutsForm(_composition.KeyboardShortcutManager);
            _shortcutsForm.FormClosed += (s, args) => _shortcutsForm = null;
        }
        _shortcutsForm.Show();
        _shortcutsForm.BringToFront();
    }

    private static void StartUpdateChecker()
    {
        if (!_composition!.Profile.GetSetting("EnableAutoUpdates", true))
        {
            return;
        }

        _ = Task.Run(() => UpdateChecker.CheckOnStartupAsync(InvokeOnUiThread, ShutdownGracefully));
    }

    private static void OnUpdatesRequested(object? sender, EventArgs e)
    {
        _ = Task.Run(() => UpdateChecker.CheckInteractiveAsync(InvokeOnUiThread, ShutdownGracefully));
    }

    private static void InvokeOnUiThread(Action action)
    {
        if (_shuttingDown)
        {
            return;
        }

        _trayManager?.InvokeOnUiThread(action);
    }

    private static bool CheckGroupPolicyCrashReporting()
    {
        try
        {
            var gpoManager = new GroupPolicyManager();
            gpoManager.Initialize();
            return gpoManager.PrivacySettings.EnableCrashReporting;
        }
        catch
        {
            // Default to enabled if Group Policy check fails
            return true;
        }
    }
}
