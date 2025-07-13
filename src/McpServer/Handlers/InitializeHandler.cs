using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.McpServer.Protocol;
using OmniSharp.Extensions.McpServer.Services;

namespace OmniSharp.Extensions.McpServer.Handlers;

public class InitializeHandler : IJsonRpcRequestHandler<InitializeParams, InitializeResult>
{
    private readonly ILogger<InitializeHandler> _logger;
    private readonly IServerStateService _serverState;

    public InitializeHandler(ILogger<InitializeHandler> logger, IServerStateService serverState)
    {
        _logger = logger;
        _serverState = serverState;
    }

    public Task<InitializeResult> Handle(InitializeParams request, CancellationToken cancellationToken)
    {
        if (_serverState.IsInitialized)
        {
            throw new InvalidOperationException("Server is already initialized");
        }

        _logger.LogInformation("Initializing MCP server for client: {ClientName} v{ClientVersion}", 
            request.ClientInfo.Name, request.ClientInfo.Version ?? "unknown");

        _serverState.MarkInitialized();

        var result = new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new ServerInfo
            {
                Name = "OmniSharp MCP Server",
                Version = "1.0.0"
            },
            Capabilities = new ServerCapabilities
            {
                Tools = new JObject(),
                Resources = new ResourcesCapability
                {
                    Subscribe = false,
                    ListChanged = true
                },
                Prompts = new PromptsCapability
                {
                    ListChanged = true
                },
                Logging = new JObject()
            }
        };

        return Task.FromResult(result);
    }
}
