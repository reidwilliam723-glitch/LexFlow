using Lexon.Service;

namespace Lexon.Service;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Lexon Service Starting...");

        // Build service composition using shared composer
        var composition = await LexonServiceComposer.BuildAsync();
        var lexonService = composition.Service;

        await lexonService.StartAsync();

        Console.WriteLine("Lexon Service started successfully");
        if (composition.AIProvider != null)
        {
            Console.WriteLine("OpenAI provider initialized successfully");
            Console.WriteLine("Writing assistance hotkeys registered");
        }
        else
        {
            Console.WriteLine("Running with dictionary-only suggestions (no API key configured)");
        }
        Console.WriteLine("Press Ctrl+C to exit");

        // Register Ctrl+C handler for graceful shutdown
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nShutting down...");
            cts.Cancel();
        };

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }

        await lexonService.StopAsync();
        Console.WriteLine("Lexon Service stopped");
    }
}
