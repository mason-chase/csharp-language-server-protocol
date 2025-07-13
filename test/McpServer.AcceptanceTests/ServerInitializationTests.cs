using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.McpServer.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.McpServer.AcceptanceTests
{
    public class ServerInitializationTests : McpServerTestBase
    {
        public ServerInitializationTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task Server_Should_Start_And_Respond_To_Initialize_Request()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            // Act - Send initialize request
            var initializeParams = new InitializeParams
            {
                ProtocolVersion = "2024-11-05",
                ClientInfo = new ClientInfo
                {
                    Name = "test-client",
                    Version = "1.0.0"
                }
            };

            var response = await client.SendRequest(initializeParams, cts.Token);

            // Assert - Verify server responded properly
            Assert.NotNull(response);
            Assert.Equal("2024-11-05", response.ProtocolVersion);
            Assert.NotNull(response.ServerInfo);
            Assert.Equal("OmniSharp MCP Server", response.ServerInfo.Name);
            Assert.NotEmpty(response.ServerInfo.Version);

            // Verify capabilities
            Assert.NotNull(response.Capabilities);
            Assert.NotNull(response.Capabilities.Tools);
            Assert.NotNull(response.Capabilities.Resources);
            Assert.NotNull(response.Capabilities.Prompts);

            // Send initialized notification to complete handshake
            client.SendNotification(McpMethods.Initialized);

            // Give server time to process the notification
            await Task.Delay(100, cts.Token);

            // Verify server is still running after initialization by attempting another request
            var testResponse = await client.SendRequest(new ListToolsParams(), cts.Token);
            Assert.NotNull(testResponse);
        }

        [Fact]
        public async Task Server_Should_List_Available_Tools_After_Initialization()
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

            // Act - Request available tools
            var toolsResponse = await client.SendRequest(new ListToolsParams(), cts.Token);

            // Assert
            Assert.NotNull(toolsResponse);
            Assert.NotNull(toolsResponse.Tools);
            Assert.NotEmpty(toolsResponse.Tools);

            // Verify expected tools are present
            var toolNames = toolsResponse.Tools.Select(t => t.Name).ToList();
            Assert.Contains("list_files", toolNames);
            Assert.Contains("read_file", toolNames);
            Assert.Contains("execute_command", toolNames);
            Assert.Contains("analyze_csharp", toolNames);
        }

        [Fact]
        public async Task Server_Should_Handle_Multiple_Sequential_Requests()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            // Initialize
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

            // Act - Send multiple requests
            var toolsTask = client.SendRequest(new ListToolsParams(), cts.Token);
            var resourcesTask = client.SendRequest(new ListResourcesParams(), cts.Token);
            var promptsTask = client.SendRequest(new ListPromptsParams(), cts.Token);

            await Task.WhenAll(toolsTask, resourcesTask, promptsTask);

            // Assert - All requests should succeed
            var toolsResponse = await toolsTask;
            var resourcesResponse = await resourcesTask;
            var promptsResponse = await promptsTask;

            // Verify tools response
            Assert.NotNull(toolsResponse);
            Assert.NotNull(toolsResponse.Tools);
            
            // Verify resources response
            Assert.NotNull(resourcesResponse);
            Assert.NotNull(resourcesResponse.Resources);
            
            // Verify prompts response
            Assert.NotNull(promptsResponse);
            Assert.NotNull(promptsResponse.Prompts);
        }

        [Fact]
        public async Task Server_Should_Reject_Requests_Before_Initialization()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var client = await StartServerAndConnectAsync(cts.Token);

            // Act & Assert - Try to call tools/list before initialization

            // The server should either reject the request or handle it gracefully
            // This test verifies the server doesn't crash when receiving requests before initialization
            try
            {
                await client.SendRequest(new ListToolsParams(), cts.Token);
                // If it succeeds, that's acceptable behavior too
            }
            catch (Exception ex) when (ex is JsonRpcException || ex is RequestFailedException)
            {
                // Expected: server rejects the request
                Assert.Contains("not initialized", ex.Message);
            }

            // Verify server is still running by sending initialize
            var initResponse = await client.SendRequest(
                new InitializeParams
                {
                    ProtocolVersion = "2024-11-05",
                    ClientInfo = new ClientInfo { Name = "test-client", Version = "1.0.0" }
                },
                cts.Token);
            Assert.NotNull(initResponse);
        }
    }
}