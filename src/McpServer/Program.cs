using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Handlers;
using OmniSharp.Extensions.McpServer.Services;
using Serilog;

namespace OmniSharp.Extensions.McpServer;

public class Program
{
    /// <summary>
    /// Log starting MCP Server
    /// </summary>
    private const string LogServerStarting = "Starting MCP Server...";

    /// <summary>
    /// Log stopping MCP Server
    /// </summary>
    private const string LogServerStopped = "MCP Server stopped";

    /// <summary>
    /// Log Format
    /// </summary>
    private const string LogOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static void Main(string[] args)
    {
        MainAsync(args).Wait();
    }

    private static async Task MainAsync(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            // Don't write to console as stdout is used for JSON-RPC communication
            .WriteTo.File("mcp-server.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: LogOutputTemplate,
                formatProvider: new CultureInfo("en-US"))
            .WriteTo.Debug(outputTemplate: LogOutputTemplate,
                           formatProvider: new CultureInfo("en-US")  )
            .CreateLogger();

        Log.Information(LogServerStarting);

        // Parse command line arguments
        string? solutionPath = null;
        if (args.Length > 0 && args[0].EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            solutionPath = args[0];
            if (!Path.IsPathFullyQualified(solutionPath))
            {
                solutionPath = Path.GetFullPath(solutionPath);
            }
            Log.Information("Solution path provided: {SolutionPath}", solutionPath);
        }

        try
        {
            // Check if another instance is already running
            var currentProcess = Process.GetCurrentProcess();
            var existingProcesses = Process.GetProcessesByName(currentProcess.ProcessName)
                .Where(p => p.Id != currentProcess.Id)
                .ToList();

            if (existingProcesses.Count != 0)
            {
                Log.Warning("Another instance of MCP Server may already be running with PID: {PIDs}",
                    string.Join(", ", existingProcesses.Select(p => p.Id)));
            }

            var server = await JsonRpcServer.From(
                options => options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithServices(services =>
                    {
                        services.AddLogging(logging => logging
                            .AddSerilog(Log.Logger)
                            .SetMinimumLevel(LogLevel.Debug));
                        // Register solution context as singleton
                        services.AddSingleton<ISolutionContext>(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<SolutionContext>>();
                            var context = new SolutionContext(logger);

                            // Load solution if provided
                            if (!string.IsNullOrEmpty(solutionPath))
                            {
                                context.LoadSolutionAsync(solutionPath).GetAwaiter().GetResult();
                            }

                            return context;
                        });

                        services.AddSingleton<IServerStateService, ServerStateService>();
                        services.AddSingleton<IToolProvider, ToolProvider>();
                        services.AddSingleton<IResourceProvider, ResourceProvider>();
                    })
                    .WithHandler<InitializeHandler>()
                    .WithHandler<ListToolsHandler>()
                    .WithHandler<CallToolHandler>()
                    .WithHandler<ListResourcesHandler>()
                    .WithHandler<ReadResourceHandler>()
                    .WithHandler<ListPromptsHandler>()
                    .WithHandler<GetPromptHandler>()
                    .WithHandler<ShutdownHandler>()
                    .WithHandler<ExitHandler>()
            );

            // Keep the server running until exit is called or Ctrl+C is pressed
            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                tcs.SetResult(true);
            };
            
            // Wait for either the server to exit or Ctrl+C
            await tcs.Task;
            
            server.Dispose();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in MCP server");
            throw;
        }
        finally
        {
            Log.Information(LogServerStopped);
            await Log.CloseAndFlushAsync();
        }
    }
}
