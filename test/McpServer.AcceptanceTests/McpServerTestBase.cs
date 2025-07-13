using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Testing;
using Xunit.Abstractions;

namespace OmniSharp.McpServer.AcceptanceTests
{
    public abstract class McpServerTestBase : JsonRpcTestBase
    {
        private Process? _serverProcess;
        private JsonRpcServer? _client;
        
        protected McpServerTestBase(ITestOutputHelper outputHelper) : base(
            new JsonRpcTestOptions(
                new TestLoggerFactory(outputHelper),
                new TestLoggerFactory(outputHelper)
            ))
        {
        }

        protected async Task<JsonRpcServer> StartServerAndConnectAsync(CancellationToken cancellationToken = default)
        {
            // Kill any existing MCP server processes to avoid conflicts
            KillExistingMcpServerProcesses();
            
            // Wait a bit to ensure file locks are released
            await Task.Delay(500, cancellationToken);
            
            var serverPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "McpServer", "bin", "Debug", "net8.0", "OmniSharp.Extensions.McpServer");
            if (OperatingSystem.IsWindows())
            {
                serverPath += ".exe";
            }

            // Build the server first to ensure it exists
            var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "McpServer", "McpServer.csproj"));
            
            // Ensure the project file exists
            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"MCP server project file not found at: {projectPath}. Current directory: {AppContext.BaseDirectory}");
            }
            
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            buildProcess.Start();
            
            // Capture both output and error for debugging
            var output = buildProcess.StandardOutput.ReadToEndAsync();
            var error = buildProcess.StandardError.ReadToEndAsync();
            
            await buildProcess.WaitForExitAsync(cancellationToken);

            if (buildProcess.ExitCode != 0)
            {
                var errorText = await error;
                var outputText = await output;
                throw new InvalidOperationException($"Failed to build MCP server. Exit code: {buildProcess.ExitCode}\nError: {errorText}\nOutput: {outputText}");
            }

            // Start the server process
            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = serverPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _serverProcess.Start();

            // Give the server a moment to start
            await Task.Delay(500, cancellationToken);

            if (_serverProcess.HasExited)
            {
                var serverError = await _serverProcess.StandardError.ReadToEndAsync();
                var serverOutput = await _serverProcess.StandardOutput.ReadToEndAsync();
                throw new InvalidOperationException($"Server exited immediately. Exit code: {_serverProcess.ExitCode}\nError: {serverError}\nOutput: {serverOutput}");
            }

            // Create client connection
            _client = await JsonRpcServer.From(
                options =>
                {
                    options.WithInput(_serverProcess.StandardOutput.BaseStream)
                          .WithOutput(_serverProcess.StandardInput.BaseStream)
                          .WithLoggerFactory(TestOptions.ClientLoggerFactory);
                },
                cancellationToken);

            return _client;
        }

        public new void Dispose()
        {
            _client?.Dispose();
            
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(5000);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
                finally
                {
                    _serverProcess.Dispose();
                }
            }
            
            // Kill any remaining MCP server processes
            KillExistingMcpServerProcesses();
            
            base.Dispose();
        }
        
        private void KillExistingMcpServerProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains("OmniSharp.Extensions.McpServer", StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Contains("dotnet", StringComparison.OrdinalIgnoreCase) && 
                               TryGetProcessCommandLine(p)?.Contains("McpServer", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
                
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000); // Wait up to 1 second for process to exit
                    }
                    catch
                    {
                        // Ignore errors when killing processes
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // Ignore any errors when trying to find/kill processes
            }
        }
        
        private string? TryGetProcessCommandLine(Process process)
        {
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }
    }

    internal class TestLoggerFactory : ILoggerFactory
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestLoggerFactory(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_outputHelper, categoryName);
        }

        public void Dispose() { }
    }

    internal class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _categoryName;

        public TestLogger(ITestOutputHelper outputHelper, string categoryName)
        {
            _outputHelper = outputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _outputHelper.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            if (exception != null)
            {
                _outputHelper.WriteLine(exception.ToString());
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}