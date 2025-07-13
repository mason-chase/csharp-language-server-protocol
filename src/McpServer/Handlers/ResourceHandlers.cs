using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Protocol;

namespace OmniSharp.Extensions.McpServer.Handlers;

public interface IResourceProvider
{
    List<Resource> GetAvailableResources();
    Task<ReadResourceResult> ReadResourceAsync(string uri);
}

public class ResourceProvider : IResourceProvider
{
    private readonly ILogger<ResourceProvider> _logger;
    
    // URI schemes
    private const string URI_SCHEME_FILE = "file://";
    private const string URI_SCHEME_PROJECT = "project://";
    
    // MIME types
    private const string MIME_TYPE_CSHARP = "text/x-csharp";
    private const string MIME_TYPE_XML = "text/xml";
    private const string MIME_TYPE_JSON = "application/json";
    private const string MIME_TYPE_TEXT = "text/plain";
    
    // File extensions
    private const string EXT_CS = ".cs";
    private const string EXT_CSPROJ = ".csproj";
    private const string EXT_SLN = ".sln";
    private const string EXT_JSON = ".json";
    private const string EXT_XML = ".xml";
    
    // Resource names
    private const string RESOURCE_SOLUTION = "LSP Solution";
    private const string RESOURCE_README = "README";
    private const string RESOURCE_CLAUDE_MD = "CLAUDE.md Documentation";
    
    // Resource descriptions
    private const string DESC_SOLUTION = "The main OmniSharp LSP solution file";
    private const string DESC_README = "Project README with overview and setup instructions";
    private const string DESC_CLAUDE_MD = "Claude Code guidance documentation";
    
    // Paths
    private const string PATH_SOLUTION = "/Users/mason/resources/omnisharp-csharp-lsp/LSP.sln";
    private const string PATH_README = "/Users/mason/resources/omnisharp-csharp-lsp/README.md";
    private const string PATH_CLAUDE_MD = "/Users/mason/resources/omnisharp-csharp-lsp/CLAUDE.md";
    
    // Error messages
    private const string ERROR_UNSUPPORTED_SCHEME = "Unsupported URI scheme: ";
    private const string ERROR_FILE_NOT_FOUND = "File not found: ";
    private const string ERROR_RESOURCE_NOT_FOUND = "Resource not found: ";

    public ResourceProvider(ILogger<ResourceProvider> logger)
    {
        _logger = logger;
    }

    public List<Resource> GetAvailableResources()
    {
        var resources = new List<Resource>
        {
            new Resource
            {
                Uri = URI_SCHEME_FILE + PATH_SOLUTION,
                Name = RESOURCE_SOLUTION,
                Description = DESC_SOLUTION,
                MimeType = MIME_TYPE_XML
            },
            new Resource
            {
                Uri = URI_SCHEME_FILE + PATH_README,
                Name = RESOURCE_README,
                Description = DESC_README,
                MimeType = MIME_TYPE_TEXT
            },
            new Resource
            {
                Uri = URI_SCHEME_FILE + PATH_CLAUDE_MD,
                Name = RESOURCE_CLAUDE_MD,
                Description = DESC_CLAUDE_MD,
                MimeType = MIME_TYPE_TEXT
            }
        };

        // Add all C# files from src directory
        if (Directory.Exists("/Users/mason/resources/omnisharp-csharp-lsp/src"))
        {
            var csFiles = Directory.GetFiles("/Users/mason/resources/omnisharp-csharp-lsp/src", "*.cs", SearchOption.AllDirectories)
                .Take(20) // Limit to first 20 files for demo
                .Select(file => new Resource
                {
                    Uri = URI_SCHEME_FILE + file,
                    Name = Path.GetFileName(file),
                    Description = Path.GetRelativePath("/Users/mason/resources/omnisharp-csharp-lsp", file),
                    MimeType = MIME_TYPE_CSHARP
                });
            
            resources.AddRange(csFiles);
        }

        return resources;
    }

    public async Task<ReadResourceResult> ReadResourceAsync(string uri)
    {
        if (!uri.StartsWith(URI_SCHEME_FILE))
        {
            throw new NotSupportedException(ERROR_UNSUPPORTED_SCHEME + uri);
        }

        var filePath = uri.Substring(URI_SCHEME_FILE.Length);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(ERROR_FILE_NOT_FOUND + filePath);
        }

        var content = await File.ReadAllTextAsync(filePath);
        var mimeType = GetMimeType(filePath);

        return new ReadResourceResult
        {
            Contents = new List<ResourceContent>
            {
                new ResourceContent
                {
                    Uri = uri,
                    MimeType = mimeType,
                    Text = content
                }
            }
        };
    }

    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            EXT_CS => MIME_TYPE_CSHARP,
            EXT_CSPROJ => MIME_TYPE_XML,
            EXT_SLN => MIME_TYPE_XML,
            EXT_JSON => MIME_TYPE_JSON,
            EXT_XML => MIME_TYPE_XML,
            _ => MIME_TYPE_TEXT
        };
    }
}

public class ListResourcesHandler : IJsonRpcRequestHandler<ListResourcesParams, ListResourcesResult>
{
    private readonly IResourceProvider _resourceProvider;
    private readonly ILogger<ListResourcesHandler> _logger;
    
    private const string LOG_LISTING_RESOURCES = "Listing available resources";

    public ListResourcesHandler(IResourceProvider resourceProvider, ILogger<ListResourcesHandler> logger)
    {
        _resourceProvider = resourceProvider;
        _logger = logger;
    }

    public Task<ListResourcesResult> Handle(ListResourcesParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(LOG_LISTING_RESOURCES);
        
        return Task.FromResult(new ListResourcesResult
        {
            Resources = _resourceProvider.GetAvailableResources()
        });
    }
}

public class ReadResourceHandler : IJsonRpcRequestHandler<ReadResourceParams, ReadResourceResult>
{
    private readonly IResourceProvider _resourceProvider;
    private readonly ILogger<ReadResourceHandler> _logger;
    
    private const string LOG_READING_RESOURCE = "Reading resource: {Uri}";
    private const string LOG_ERROR_READING = "Error reading resource {Uri}";

    public ReadResourceHandler(IResourceProvider resourceProvider, ILogger<ReadResourceHandler> logger)
    {
        _resourceProvider = resourceProvider;
        _logger = logger;
    }

    public async Task<ReadResourceResult> Handle(ReadResourceParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LOG_READING_RESOURCE, request.Uri);
        
        try
        {
            return await _resourceProvider.ReadResourceAsync(request.Uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_ERROR_READING, request.Uri);
            throw;
        }
    }
}