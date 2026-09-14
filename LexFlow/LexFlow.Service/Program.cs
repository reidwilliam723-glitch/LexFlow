using LexFlow.Service;

namespace LexFlow.Service;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("LexFlow Service Starting...");

        // Build service composition using shared composer
        var composition = await LexFlowServiceComposer.BuildAsync();
        var lexFlowService = composition.Service;

        await lexFlowService.StartAsync();

        Console.WriteLine("LexFlow Service started successfully");
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

        await lexFlowService.StopAsync();
        Console.WriteLine("LexFlow Service stopped");
    }
}
