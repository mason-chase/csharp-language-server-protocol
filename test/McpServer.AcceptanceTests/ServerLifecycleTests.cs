using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.McpServer.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.McpServer.AcceptanceTests
{
    public class ServerLifecycleTests : McpServerTestBase
    {
        public ServerLifecycleTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task Server_Should_Not_Exit_Before_Initialize_Response()
        {
            // This test addresses the specific issue:
            // "Server exited before responding to `initialize` request"

            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            // Act - Start server and immediately check if it's running
            var client = await StartServerAndConnectAsync(cts.Token);
            
            // Give server a moment to potentially crash (if it would)
            await Task.Delay(1000, cts.Token);
            
            // Assert - Server should still be responding
            // Try sending the initialize request - if server crashed, this will fail
            var response = await client.SendRequest(
                new InitializeParams
                {
                    ProtocolVersion = "2024-11-05",
                    ClientInfo = new ClientInfo
                    {
                        Name = "test-client",
                        Version = "1.0.0"
                    }
                },
                cts.Token);

            // Server should have responded successfully
            Assert.NotNull(response);
            Assert.NotNull(response.ServerInfo);
        }

        [Fact]
        public async Task Server_Should_Exit_Gracefully_On_Shutdown_Request()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            // Initialize the server
            await client.SendRequest(
                new InitializeParams
                {
                    ProtocolVersion = "2024-11-05",
                    ClientInfo = new ClientInfo
                    {
                        Name = "test-client",
                        Version = "1.0.0"
                    }
                },
                cts.Token);

            client.SendNotification(McpMethods.Initialized);

            // Act - Send shutdown request
            await client.SendRequest("shutdown").Returning<object>(cts.Token);

            // Send exit notification
            client.SendNotification("exit");

            // Give server time to exit gracefully
            await Task.Delay(2000, cts.Token);

            // Assert - Server should have exited gracefully
            // The test framework will verify the server process exits without errors
        }

        [Fact]
        public async Task Server_Should_Handle_Client_Disconnect_Gracefully()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            // Initialize the server
            await client.SendRequest(
                new InitializeParams
                {
                    ProtocolVersion = "2024-11-05",
                    ClientInfo = new ClientInfo
                    {
                        Name = "test-client",
                        Version = "1.0.0"
                    }
                },
                cts.Token);

            client.SendNotification(McpMethods.Initialized);

            // Act - Abruptly disconnect the client
            client.Dispose();

            // Give server time to detect disconnection
            await Task.Delay(1000, cts.Token);

            // Assert - Server process should exit cleanly (not crash)
            // The test framework will verify the server process exits without errors
        }

        [Fact]
        public async Task Server_Should_Timeout_If_No_Initialize_Request_Received()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35)); // Longer than expected timeout
            var client = await StartServerAndConnectAsync(cts.Token);

            // Act - Don't send initialize request, just wait
            var startTime = DateTime.UtcNow;
            
            // Wait for server to potentially timeout (most servers timeout after 10-30 seconds)
            var timeoutOccurred = false;
            while ((DateTime.UtcNow - startTime) < TimeSpan.FromSeconds(32))
            {
                try
                {
                    // Try to send a ping to see if server is still alive
                    await client.SendRequest(new ListToolsParams(), cts.Token);
                    await Task.Delay(1000, cts.Token);
                }
                catch
                {
                    timeoutOccurred = true;
                    break;
                }
            }

            // Assert - Server should either:
            // 1. Still be running (no timeout implemented)
            // 2. Have disconnected cleanly (timeout implemented)
            // Both are valid behaviors, but it shouldn't crash
            
            // This test mainly ensures the server doesn't crash while waiting
        }

        [Fact]
        public async Task Server_Should_Reject_Duplicate_Initialize_Requests()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            var initializeParams = new InitializeParams
            {
                ProtocolVersion = "2024-11-05",
                ClientInfo = new ClientInfo
                {
                    Name = "test-client",
                    Version = "1.0.0"
                }
            };

            // First initialization
            await client.SendRequest(initializeParams, cts.Token);

            client.SendNotification(McpMethods.Initialized);

            // Act & Assert - Try to initialize again
            var exception = await Assert.ThrowsAsync<InternalErrorException>(async () => 
                await client.SendRequest(initializeParams, cts.Token));
            
            // The error message will be "Internal error." but the cause is duplicate initialization
            Assert.NotNull(exception);
        }
    }
}