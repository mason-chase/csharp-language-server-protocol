using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Protocol;
using OmniSharp.Extensions.McpServer.Services;

namespace OmniSharp.Extensions.McpServer.Handlers;

public interface IToolProvider
{
    List<Tool> GetAvailableTools();
    Task<CallToolResult> ExecuteToolAsync(string toolName, JObject? arguments);
}

public class ToolProvider : IToolProvider
{
    private readonly ILogger<ToolProvider> _logger;
    private readonly ISolutionContext _solutionContext;

    // Tool names
    private const string TOOL_LIST_FILES = "list_files";
    private const string TOOL_READ_FILE = "read_file";
    private const string TOOL_EXECUTE_COMMAND = "execute_command";
    private const string TOOL_ANALYZE_CSHARP = "analyze_csharp";
    private const string TOOL_GET_SOLUTION_INFO = "get_solution_info";
    private const string TOOL_LIST_PROJECTS = "list_projects";
    private const string TOOL_FIND_IN_SOLUTION = "find_in_solution";

    // Tool descriptions
    private const string DESC_LIST_FILES = "List files in a directory";
    private const string DESC_READ_FILE = "Read contents of a file";
    private const string DESC_EXECUTE_COMMAND = "Execute a shell command";
    private const string DESC_ANALYZE_CSHARP = "Analyze C# code structure";
    private const string DESC_GET_SOLUTION_INFO = "Get information about the loaded solution";
    private const string DESC_LIST_PROJECTS = "List all projects in the loaded solution";
    private const string DESC_FIND_IN_SOLUTION = "Find files in the solution matching a pattern";

    // Schema property names
    private const string SCHEMA_TYPE = "type";
    private const string SCHEMA_OBJECT = "object";
    private const string SCHEMA_STRING = "string";
    private const string SCHEMA_PROPERTIES = "properties";
    private const string SCHEMA_REQUIRED = "required";
    private const string SCHEMA_DESCRIPTION = "description";

    // Parameter names
    private const string PARAM_PATH = "path";
    private const string PARAM_PATTERN = "pattern";
    private const string PARAM_COMMAND = "command";
    private const string PARAM_WORKING_DIRECTORY = "workingDirectory";
    private const string PARAM_FILE_PATH = "filePath";

    // Parameter descriptions
    private const string DESC_DIRECTORY_PATH = "Directory path to list";
    private const string DESC_FILE_PATTERN = "File pattern (e.g., *.cs)";
    private const string DESC_FILE_PATH_READ = "File path to read";
    private const string DESC_COMMAND_EXECUTE = "Command to execute";
    private const string DESC_WORKING_DIR = "Working directory";
    private const string DESC_CSHARP_FILE = "C# file to analyze";

    // Content types
    private const string CONTENT_TYPE_TEXT = "text";

    // Error messages
    private const string ERROR_UNKNOWN_TOOL = "Unknown tool: ";
    private const string ERROR_PREFIX = "Error: ";
    private const string ERROR_PATH_REQUIRED = "Path is required";
    private const string ERROR_COMMAND_REQUIRED = "Command is required";
    private const string ERROR_FILEPATH_REQUIRED = "FilePath is required";
    private const string ERROR_PROCESS_START = "Failed to start process";

    // Shell
    private const string SHELL_PATH = "/bin/bash";
    private const string SHELL_ARG_FORMAT = "-c \"{0}\"";

    // File patterns
    private const string DEFAULT_PATTERN = "*";
    private const string CURRENT_DIRECTORY = ".";

    // Analysis strings
    private const string ANALYSIS_FILE = "File: ";
    private const string ANALYSIS_LINES = "Lines: ";
    private const string ANALYSIS_NAMESPACES = "Namespaces: ";
    private const string ANALYSIS_CLASSES = "Classes: ";
    private const string ANALYSIS_METHODS = "Methods (approx): ";
    private const string KEYWORD_NAMESPACE = "namespace";
    private const string KEYWORD_CLASS = "class ";
    private const string CHAR_OPEN_PAREN = "(";
    private const string CHAR_CLOSE_PAREN = ")";
    private const string CHAR_OPEN_BRACE = "{";
    private const string OUTPUT_SEPARATOR = "\n";
    private const string ERROR_SECTION = "\n\nErrors:\n";

    public ToolProvider(ILogger<ToolProvider> logger, ISolutionContext solutionContext)
    {
        _logger = logger;
        _solutionContext = solutionContext;
    }

    public List<Tool> GetAvailableTools()
    {
        return new List<Tool>
        {
            new Tool
            {
                Name = TOOL_LIST_FILES,
                Description = DESC_LIST_FILES,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new
                    {
                        path = new { type = SCHEMA_STRING, description = DESC_DIRECTORY_PATH },
                        pattern = new { type = SCHEMA_STRING, description = DESC_FILE_PATTERN }
                    },
                    required = new[] { PARAM_PATH }
                })
            },
            new Tool
            {
                Name = TOOL_READ_FILE,
                Description = DESC_READ_FILE,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new
                    {
                        path = new { type = SCHEMA_STRING, description = DESC_FILE_PATH_READ }
                    },
                    required = new[] { PARAM_PATH }
                })
            },
            new Tool
            {
                Name = TOOL_EXECUTE_COMMAND,
                Description = DESC_EXECUTE_COMMAND,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new
                    {
                        command = new { type = SCHEMA_STRING, description = DESC_COMMAND_EXECUTE },
                        workingDirectory = new { type = SCHEMA_STRING, description = DESC_WORKING_DIR }
                    },
                    required = new[] { PARAM_COMMAND }
                })
            },
            new Tool
            {
                Name = TOOL_ANALYZE_CSHARP,
                Description = DESC_ANALYZE_CSHARP,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new
                    {
                        filePath = new { type = SCHEMA_STRING, description = DESC_CSHARP_FILE }
                    },
                    required = new[] { PARAM_FILE_PATH }
                })
            },
            new Tool
            {
                Name = TOOL_GET_SOLUTION_INFO,
                Description = DESC_GET_SOLUTION_INFO,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new { }
                })
            },
            new Tool
            {
                Name = TOOL_LIST_PROJECTS,
                Description = DESC_LIST_PROJECTS,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new { }
                })
            },
            new Tool
            {
                Name = TOOL_FIND_IN_SOLUTION,
                Description = DESC_FIND_IN_SOLUTION,
                InputSchema = JObject.FromObject(new
                {
                    type = SCHEMA_OBJECT,
                    properties = new
                    {
                        pattern = new { type = SCHEMA_STRING, description = "File pattern to search for (e.g., **/*.cs)" }
                    },
                    required = new[] { PARAM_PATTERN }
                })
            }
        };
    }

    public async Task<CallToolResult> ExecuteToolAsync(string toolName, JObject? arguments)
    {
        try
        {
            return toolName switch
            {
                TOOL_LIST_FILES => await ListFilesAsync(arguments),
                TOOL_READ_FILE => await ReadFileAsync(arguments),
                TOOL_EXECUTE_COMMAND => await ExecuteCommandAsync(arguments),
                TOOL_ANALYZE_CSHARP => await AnalyzeCSharpAsync(arguments),
                TOOL_GET_SOLUTION_INFO => GetSolutionInfo(),
                TOOL_LIST_PROJECTS => ListProjects(),
                TOOL_FIND_IN_SOLUTION => FindInSolution(arguments),
                _ => new CallToolResult
                {
                    Content = new List<ToolContent>
                    {
                        new() { Type = CONTENT_TYPE_TEXT, Text = ERROR_UNKNOWN_TOOL + toolName }
                    },
                    IsError = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return new CallToolResult
            {
                Content = new List<ToolContent>
                {
                    new() { Type = CONTENT_TYPE_TEXT, Text = ERROR_PREFIX + ex.Message }
                },
                IsError = true
            };
        }
    }

    private Task<CallToolResult> ListFilesAsync(JObject? arguments)
    {
        var path = arguments?[PARAM_PATH]?.ToString() ?? CURRENT_DIRECTORY;
        var pattern = arguments?[PARAM_PATTERN]?.ToString() ?? DEFAULT_PATTERN;

        var files = Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(path, f))
            .OrderBy(f => f);

        return Task.FromResult(new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = string.Join(OUTPUT_SEPARATOR, files) }
            }
        });
    }

    private async Task<CallToolResult> ReadFileAsync(JObject? arguments)
    {
        var path = arguments?[PARAM_PATH]?.ToString();
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException(ERROR_PATH_REQUIRED);

        var content = await File.ReadAllTextAsync(path);
        
        return new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = content }
            }
        };
    }

    private async Task<CallToolResult> ExecuteCommandAsync(JObject? arguments)
    {
        var command = arguments?[PARAM_COMMAND]?.ToString();
        if (string.IsNullOrEmpty(command))
            throw new ArgumentException(ERROR_COMMAND_REQUIRED);

        var workingDirectory = arguments?[PARAM_WORKING_DIRECTORY]?.ToString() ?? Directory.GetCurrentDirectory();

        var startInfo = new ProcessStartInfo
        {
            FileName = SHELL_PATH,
            Arguments = string.Format(SHELL_ARG_FORMAT, command),
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException(ERROR_PROCESS_START);

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = string.IsNullOrEmpty(error) ? output : output + ERROR_SECTION + error;

        return new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = result }
            },
            IsError = process.ExitCode != 0
        };
    }

    private Task<CallToolResult> AnalyzeCSharpAsync(JObject? arguments)
    {
        var filePath = arguments?[PARAM_FILE_PATH]?.ToString();
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException(ERROR_FILEPATH_REQUIRED);

        // This is a simple analysis - in a real implementation, you'd use Roslyn
        var lines = File.ReadAllLines(filePath);
        var namespaces = lines.Where(l => l.Trim().StartsWith(KEYWORD_NAMESPACE)).Count();
        var classes = lines.Where(l => l.Contains(KEYWORD_CLASS)).Count();
        var methods = lines.Where(l => l.Contains(CHAR_OPEN_PAREN) && l.Contains(CHAR_CLOSE_PAREN) && l.Contains(CHAR_OPEN_BRACE)).Count();

        var analysis = ANALYSIS_FILE + filePath + OUTPUT_SEPARATOR +
                      ANALYSIS_LINES + lines.Length + OUTPUT_SEPARATOR +
                      ANALYSIS_NAMESPACES + namespaces + OUTPUT_SEPARATOR +
                      ANALYSIS_CLASSES + classes + OUTPUT_SEPARATOR +
                      ANALYSIS_METHODS + methods;

        return Task.FromResult(new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = analysis }
            }
        });
    }

    private CallToolResult GetSolutionInfo()
    {
        if (!_solutionContext.IsLoaded)
        {
            return new CallToolResult
            {
                Content = new List<ToolContent>
                {
                    new() { Type = CONTENT_TYPE_TEXT, Text = "No solution loaded. Start the server with a .sln file path as argument." }
                }
            };
        }

        var info = $"Solution: {_solutionContext.SolutionPath}\n" +
                   $"Directory: {_solutionContext.SolutionDirectory}\n" +
                   $"Projects: {_solutionContext.GetProjects().Count}";

        return new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = info }
            }
        };
    }

    private CallToolResult ListProjects()
    {
        if (!_solutionContext.IsLoaded)
        {
            return new CallToolResult
            {
                Content = new List<ToolContent>
                {
                    new() { Type = CONTENT_TYPE_TEXT, Text = "No solution loaded. Start the server with a .sln file path as argument." }
                }
            };
        }

        var projects = _solutionContext.GetProjects();
        var projectList = string.Join(OUTPUT_SEPARATOR, projects.Select(p => Path.GetRelativePath(_solutionContext.SolutionDirectory!, p)));

        return new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = projectList }
            }
        };
    }

    private CallToolResult FindInSolution(JObject? arguments)
    {
        if (!_solutionContext.IsLoaded)
        {
            return new CallToolResult
            {
                Content = new List<ToolContent>
                {
                    new() { Type = CONTENT_TYPE_TEXT, Text = "No solution loaded. Start the server with a .sln file path as argument." }
                }
            };
        }

        var pattern = arguments?[PARAM_PATTERN]?.ToString() ?? DEFAULT_PATTERN;
        var files = _solutionContext.FindFiles(pattern);
        var fileList = string.Join(OUTPUT_SEPARATOR, files);

        return new CallToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = CONTENT_TYPE_TEXT, Text = fileList }
            }
        };
    }
}

public class ListToolsHandler : IJsonRpcRequestHandler<ListToolsParams, ListToolsResult>
{
    private readonly IToolProvider _toolProvider;
    private readonly ILogger<ListToolsHandler> _logger;

    private const string LOG_LISTING_TOOLS = "Listing available tools";

    public ListToolsHandler(IToolProvider toolProvider, ILogger<ListToolsHandler> logger)
    {
        _toolProvider = toolProvider;
        _logger = logger;
    }

    public Task<ListToolsResult> Handle(ListToolsParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(LOG_LISTING_TOOLS);
        
        return Task.FromResult(new ListToolsResult
        {
            Tools = _toolProvider.GetAvailableTools()
        });
    }
}

public class CallToolHandler : IJsonRpcRequestHandler<CallToolParams, CallToolResult>
{
    private readonly IToolProvider _toolProvider;
    private readonly ILogger<CallToolHandler> _logger;

    private const string LOG_CALLING_TOOL = "Calling tool: {ToolName}";

    public CallToolHandler(IToolProvider toolProvider, ILogger<CallToolHandler> logger)
    {
        _toolProvider = toolProvider;
        _logger = logger;
    }

    public async Task<CallToolResult> Handle(CallToolParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LOG_CALLING_TOOL, request.Name);
        
        return await _toolProvider.ExecuteToolAsync(request.Name, request.Arguments);
    }
}