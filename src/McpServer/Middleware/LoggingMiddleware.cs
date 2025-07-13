using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;

namespace OmniSharp.Extensions.McpServer.Middleware;

public class LoggingMiddleware : IRequestProcessIdentifier
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public RequestProcessType Identify(IHandlerDescriptor descriptor)
    {
        // Log all requests
        return RequestProcessType.Parallel;
    }
}