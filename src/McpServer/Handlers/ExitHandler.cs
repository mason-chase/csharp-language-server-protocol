using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Services;

namespace OmniSharp.Extensions.McpServer.Handlers;

[Method("exit")]
public class ExitHandler : IJsonRpcNotificationHandler<ExitParams>
{
    private readonly ILogger<ExitHandler> _logger;
    private readonly IServerStateService _serverState;

    public ExitHandler(ILogger<ExitHandler> logger, IServerStateService serverState)
    {
        _logger = logger;
        _serverState = serverState;
    }

    public Task<Unit> Handle(ExitParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exit notification received");
        
        // Exit code: 0 if shutdown was requested first, 1 otherwise
        var exitCode = _serverState.IsShuttingDown ? 0 : 1;
        
        if (exitCode != 0)
        {
            _logger.LogWarning("Exit called without prior shutdown request");
        }
        
        // Signal the server to exit gracefully
        Task.Run(() => 
        {
            Thread.Sleep(100); // Give time for response to be sent
            Environment.Exit(exitCode);
        });
        
        return Unit.Task;
    }
}

public record ExitParams : IRequest<Unit>
{
    public static ExitParams Instance { get; } = new();
}