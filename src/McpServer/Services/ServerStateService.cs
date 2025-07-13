namespace OmniSharp.Extensions.McpServer.Services;

public interface IServerStateService
{
    bool IsInitialized { get; }
    bool IsShuttingDown { get; }
    void MarkInitialized();
    void MarkShuttingDown();
}

public class ServerStateService : IServerStateService
{
    private bool _isInitialized;
    private bool _isShuttingDown;

    public bool IsInitialized => _isInitialized;
    public bool IsShuttingDown => _isShuttingDown;

    public void MarkInitialized()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Server is already initialized");
        }
        _isInitialized = true;
    }
    
    public void MarkShuttingDown()
    {
        _isShuttingDown = true;
    }
}