using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.McpServer.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.McpServer.AcceptanceTests
{
    /// <summary>
    /// Comprehensive test that combines all MCP server functionality tests into a single test method
    /// </summary>
    public class McpServerComprehensiveTest : McpServerTestBase
    {
        public McpServerComprehensiveTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task Server_Should_Handle_Complete_Lifecycle_And_All_Operations()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); 
            var client = await StartServerAndConnectAsync(cts.Token);
            await client.SendRequest(new ListToolsParams(), cts.Token);
            var initializeParams = new InitializeParams
            {
                ProtocolVersion = "2024-11-05",
                ClientInfo = new ClientInfo
                {
                    Name = "test-client",
                    Version = "1.0.0"
                }
            };

            var initResponse = await client.SendRequest(initializeParams, cts.Token);
            
            // Verify initialization response
            Assert.NotNull(initResponse);
            Assert.Equal("2024-11-05", initResponse.ProtocolVersion);
            Assert.NotNull(initResponse.ServerInfo);
            Assert.Equal("OmniSharp MCP Server", initResponse.ServerInfo.Name);
            Assert.NotEmpty(initResponse.ServerInfo.Version);
            
            // Verify capabilities
            Assert.NotNull(initResponse.Capabilities);
            Assert.NotNull(initResponse.Capabilities.Tools);
            Assert.NotNull(initResponse.Capabilities.Resources);
            Assert.NotNull(initResponse.Capabilities.Prompts);

            // Send initialized notification to complete handshake
            client.SendNotification(McpMethods.Initialized);
            
            // Give server time to process the notification
            await Task.Delay(100, cts.Token);

            // ==========================================
            // PHASE 4: Test Duplicate Initialization
            // ==========================================
            
            try
            {
                await client.SendRequest(initializeParams, cts.Token);
                Assert.True(false, "Server should reject duplicate initialize requests");
            }
            catch (InternalErrorException ex)
            {
                // When we throw exceptions in handlers, they get wrapped as InternalErrorException
                Assert.NotNull(ex);
            }

            // ==========================================
            // PHASE 5: Test Tools Functionality
            // ==========================================

            var toolsResponse = await client.SendRequest(new ListToolsParams(), cts.Token);

            Assert.NotNull(toolsResponse);
            Assert.NotNull(toolsResponse.Tools);
            Assert.NotEmpty(toolsResponse.Tools);

            var toolNames = toolsResponse.Tools.Select(t => t.Name).ToList();
            Assert.Contains("list_files", toolNames);
            Assert.Contains("read_file", toolNames);
            Assert.Contains("execute_command", toolNames);
            Assert.Contains("analyze_csharp", toolNames);

            var resourcesResponse = await client.SendRequest(new ListResourcesParams(), cts.Token);
            
            Assert.NotNull(resourcesResponse);
            Assert.NotNull(resourcesResponse.Resources);

            // ==========================================
            // PHASE 7: Test Prompts Functionality
            // ==========================================
            
            var promptsResponse = await client.SendRequest(new ListPromptsParams(), cts.Token);
            
            Assert.NotNull(promptsResponse);
            Assert.NotNull(promptsResponse.Prompts);

            // ==========================================
            // PHASE 8: Test Concurrent Requests
            // ==========================================
            
            var toolsTask = client.SendRequest(new ListToolsParams(), cts.Token);
            var resourcesTask = client.SendRequest(new ListResourcesParams(), cts.Token);
            var promptsTask = client.SendRequest(new ListPromptsParams(), cts.Token);
            
            await Task.WhenAll(toolsTask, resourcesTask, promptsTask);
            
            // All concurrent requests should succeed
            var toolsResult = await toolsTask;
            var resourcesResult = await resourcesTask;
            var promptsResult = await promptsTask;
            
            Assert.NotNull(toolsResult);
            Assert.NotNull(resourcesResult);
            Assert.NotNull(promptsResult);

            // ==========================================
            // PHASE 9: Test Multiple Sequential Operations
            // ==========================================
            
            // Send multiple requests in sequence to ensure server remains stable
            for (int i = 0; i < 5; i++)
            {
                var seqToolsResponse = await client.SendRequest(new ListToolsParams(), cts.Token);
                Assert.NotNull(seqToolsResponse);
                Assert.NotEmpty(seqToolsResponse.Tools);
            }

            // ==========================================
            // PHASE 10: Test Server Shutdown (Optional)
            // ==========================================
            
            // Note: We're not testing shutdown/exit here as it would terminate the server
            // and prevent further testing. In a real scenario, this would be tested separately.
            
            // Instead, verify the server is still responsive after all operations
            var finalResponse = await client.SendRequest(new ListToolsParams(), cts.Token);
            Assert.NotNull(finalResponse);
            Assert.NotEmpty(finalResponse.Tools);
        }
    }
}
