using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Lexon.Core;
using Lexon.Core.Learning;
using Lexon.Core.Models;
using Lexon.Privacy;
using Lexon.Storage;
using Xunit;

namespace Lexon.Tests;

public class PrivacyGuardGapTests
{
    [Fact]
    public void BlocksPasswordFieldsAndBlockedApps()
    {
        var guard = new PrivacyGuard();
        guard.AddBlockedApplication("notepad");

        Assert.True(guard.ShouldBlockAssistance(new TextContext { IsPasswordField = true, ApplicationName = "chrome" }));
        Assert.True(guard.ShouldBlockAssistance(new TextContext { WindowTitle = "Acme Password Vault", ApplicationName = "chrome" }));
        Assert.True(guard.ShouldBlockAssistance(new TextContext { ApplicationName = "notepad", FullText = "hello" }));
        Assert.False(guard.ShouldBlockAssistance(new TextContext { ApplicationName = "winword", FullText = "hello" }));
    }

    [Fact]
    public void NullContextFailsClosed()
    {
        var guard = new PrivacyGuard();
        Assert.True(guard.ShouldBlockAssistance(null!));
    }

    [Fact]
    public void DefaultRemoteToolsStayBlocked()
    {
        var guard = new PrivacyGuard();
        Assert.True(guard.IsApplicationBlocked("putty"));
        Assert.True(guard.ShouldBlockAssistance(new TextContext { ApplicationName = "mstsc" }));
    }
}

public class UpdatePolicyTests
{
    [Fact]
    public void DetectsNewerVersionAndFormatsNotes()
    {
        Assert.True(UpdatePolicy.IsNewer("2.1.0", "2.0.0"));
        Assert.False(UpdatePolicy.IsNewer("2.0.0", "2.0.0"));
        Assert.False(UpdatePolicy.ShouldCheckNow(DateTime.UtcNow, DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(24)));
        Assert.True(UpdatePolicy.ShouldCheckNow(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, TimeSpan.FromHours(24)));

        var manifest = UpdatePolicy.ParseManifest("""
            {"version":"2.1.0","releaseNotes":"Privacy guard on rewrite.","downloadUrl":"https://example.com/lexon.zip","sha256":"abc","privacyNotes":"Rewrite no longer sends blocked fields."}
            """);
        Assert.NotNull(manifest);
        Assert.Equal("2.1.0", manifest!.Version);

        var text = UpdatePolicy.FormatNotification(new UpdateCheckResult
        {
            CurrentVersion = "2.0.0",
            LatestVersion = "2.1.0",
            ReleaseNotes = "Privacy guard on rewrite.",
            PrivacyNotes = "Rewrite no longer sends blocked fields.",
            Sha256 = "abc",
            AutoInstall = false
        });
        Assert.Contains("2.1.0", text);
        Assert.Contains("Privacy/security", text);
        Assert.DoesNotContain("installing now", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sha256MustMatchBeforeTreatingFileAsLegitimate()
    {
        var bytes = Encoding.UTF8.GetBytes("payload");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        Assert.True(UpdatePolicy.MatchesSha256(bytes, hash));
        Assert.False(UpdatePolicy.MatchesSha256(bytes, "00"));
        Assert.False(UpdatePolicy.MatchesSha256(bytes, null));
    }

    [Fact]
    public async Task CheckForUpdates_NotifiesWithoutInstalling()
    {
        var json = """{"version":"9.9.9","releaseNotes":"Test channel.","downloadUrl":"https://example.test/app.zip","sha256":"deadbeef","privacyNotes":"Test."}""";
        var handler = new StaticJsonHandler(json);
        using var http = new HttpClient(handler);
        var manager = new UpdateManager(http, "2.0.0", "https://example.test/updates");
        manager.Initialize(true, "https://example.test/updates", TimeSpan.Zero);

        UpdateCheckResult? raised = null;
        manager.UpdateFound += (_, result) => raised = result;

        var check = await manager.CheckForUpdatesAsync(forceCheck: true);

        Assert.True(check.Success);
        Assert.True(check.UpdateAvailable);
        Assert.False(check.AutoInstall);
        Assert.Equal("9.9.9", check.LatestVersion);
        Assert.Equal("https://example.test/app.zip", check.DownloadUrl);
        Assert.NotNull(raised);
        Assert.Equal(1, handler.Calls);
    }

    private sealed class StaticJsonHandler : HttpMessageHandler
    {
        private readonly string _json;
        public int Calls { get; private set; }

        public StaticJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }
}

public class CloudAiActivityLogTests
{
    [Fact]
    public void RecordsProviderAndAppWithoutText()
    {
        var log = new CloudAiActivityLog();
        log.Record("OpenAI", "winword", "rewrite");
        var entry = Assert.Single(log.Snapshot());
        Assert.Equal("OpenAI", entry.Provider);
        Assert.Equal("winword", entry.ApplicationName);
        Assert.Equal("rewrite", entry.Action);
        Assert.DoesNotContain("secret password", entry.ApplicationName);
    }
}

public class PersonalizationExportTests
{
    [Fact]
    public async Task RoundTripsLearningFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "lexon-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            var storage = new EncryptedStorage(path);
            var manager = new PersonalizationManager(storage, new FeedbackCollector());
            await manager.InitializeAsync();
            manager.RecordCharactersInserted(42);
            var json = manager.ExportLearningData();
            Assert.Contains("ExportDate", json);

            var other = new PersonalizationManager(storage, new FeedbackCollector());
            Assert.True(other.ImportLearningData(json));
            Assert.False(other.ImportLearningData("{not json"));
        }
        finally
        {
            try { Directory.Delete(path, true); } catch { }
        }
    }
}
