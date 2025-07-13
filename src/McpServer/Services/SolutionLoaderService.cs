using Microsoft.Extensions.Hosting;

namespace OmniSharp.Extensions.McpServer.Services;

public class SolutionLoaderService : IHostedService
{
    private readonly ISolutionContext _solutionContext;
    private readonly string _solutionPath;

    public SolutionLoaderService(ISolutionContext solutionContext, string solutionPath)
    {
        _solutionContext = solutionContext;
        _solutionPath = solutionPath;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _solutionContext.LoadSolutionAsync(_solutionPath);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}