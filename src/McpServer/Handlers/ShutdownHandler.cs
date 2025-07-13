using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Services;
using MediatR;

namespace OmniSharp.Extensions.McpServer.Handlers;

[Method("shutdown")]
public class ShutdownHandler : IJsonRpcRequestHandler<ShutdownParams, Unit>
{
    private readonly ILogger<ShutdownHandler> _logger;
    private readonly IServerStateService _serverState;

    public ShutdownHandler(ILogger<ShutdownHandler> logger, IServerStateService serverState)
    {
        _logger = logger;
        _serverState = serverState;
    }

    public Task<Unit> Handle(ShutdownParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutdown request received");
        _serverState.MarkShuttingDown();
        return Unit.Task;
    }
}

public record ShutdownParams : IRequest<Unit>
{
    public static ShutdownParams Instance { get; } = new();
}