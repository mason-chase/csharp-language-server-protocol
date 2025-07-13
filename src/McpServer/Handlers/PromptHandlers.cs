using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.McpServer.Protocol;

namespace OmniSharp.Extensions.McpServer.Handlers;

public class ListPromptsHandler : IJsonRpcRequestHandler<ListPromptsParams, ListPromptsResult>
{
    private readonly ILogger<ListPromptsHandler> _logger;
    
    // Prompt names
    private const string PROMPT_ANALYZE_CODE = "analyze_code";
    private const string PROMPT_EXPLAIN_LSP = "explain_lsp";
    private const string PROMPT_REVIEW_PR = "review_pr";
    
    // Prompt descriptions
    private const string DESC_ANALYZE_CODE = "Analyze C# code and provide insights";
    private const string DESC_EXPLAIN_LSP = "Explain Language Server Protocol concepts";
    private const string DESC_REVIEW_PR = "Review code changes for a pull request";
    
    // Argument names
    private const string ARG_FILE_PATH = "filePath";
    private const string ARG_CODE = "code";
    private const string ARG_CONCEPT = "concept";
    private const string ARG_BRANCH = "branch";
    
    // Argument descriptions
    private const string DESC_ARG_FILE_PATH = "Path to the C# file to analyze";
    private const string DESC_ARG_CODE = "C# code snippet to analyze";
    private const string DESC_ARG_CONCEPT = "LSP concept to explain";
    private const string DESC_ARG_BRANCH = "Git branch to review";
    
    // Log messages
    private const string LOG_LISTING_PROMPTS = "Listing available prompts";

    public ListPromptsHandler(ILogger<ListPromptsHandler> logger)
    {
        _logger = logger;
    }

    public Task<ListPromptsResult> Handle(ListPromptsParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(LOG_LISTING_PROMPTS);
        
        var prompts = new List<Prompt>
        {
            new Prompt
            {
                Name = PROMPT_ANALYZE_CODE,
                Description = DESC_ANALYZE_CODE,
                Arguments = new List<PromptArgument>
                {
                    new() { Name = ARG_FILE_PATH, Description = DESC_ARG_FILE_PATH, Required = false },
                    new() { Name = ARG_CODE, Description = DESC_ARG_CODE, Required = false }
                }
            },
            new Prompt
            {
                Name = PROMPT_EXPLAIN_LSP,
                Description = DESC_EXPLAIN_LSP,
                Arguments = new List<PromptArgument>
                {
                    new() { Name = ARG_CONCEPT, Description = DESC_ARG_CONCEPT, Required = true }
                }
            },
            new Prompt
            {
                Name = PROMPT_REVIEW_PR,
                Description = DESC_REVIEW_PR,
                Arguments = new List<PromptArgument>
                {
                    new() { Name = ARG_BRANCH, Description = DESC_ARG_BRANCH, Required = true }
                }
            }
        };

        return Task.FromResult(new ListPromptsResult { Prompts = prompts });
    }
}

public class GetPromptHandler : IJsonRpcRequestHandler<GetPromptParams, GetPromptResult>
{
    private readonly ILogger<GetPromptHandler> _logger;
    
    // Prompt names (reuse from ListPromptsHandler)
    private const string PROMPT_ANALYZE_CODE = "analyze_code";
    private const string PROMPT_EXPLAIN_LSP = "explain_lsp";
    private const string PROMPT_REVIEW_PR = "review_pr";
    
    // Argument names (reuse from ListPromptsHandler)
    private const string ARG_FILE_PATH = "filePath";
    private const string ARG_CODE = "code";
    private const string ARG_CONCEPT = "concept";
    private const string ARG_BRANCH = "branch";
    
    // Roles
    private const string ROLE_USER = "user";
    private const string ROLE_ASSISTANT = "assistant";
    
    // Content type
    private const string CONTENT_TYPE_TEXT = "text";
    
    // Prompt templates
    private const string TEMPLATE_ANALYZE_FILE = "Please analyze the following C# file and provide insights about its structure, patterns, and potential improvements:\n\nFile: {0}\n\nFocus on:\n1. Code organization\n2. Design patterns used\n3. Potential improvements\n4. Best practices";
    
    private const string TEMPLATE_ANALYZE_CODE = "Please analyze the following C# code snippet and provide insights:\n\n```csharp\n{0}\n```\n\nFocus on:\n1. Code quality\n2. Potential issues\n3. Suggestions for improvement";
    
    private const string TEMPLATE_EXPLAIN_LSP = "Please explain the following Language Server Protocol concept: {0}\n\nInclude:\n1. What it is\n2. How it works\n3. Example usage\n4. Related concepts";
    
    private const string TEMPLATE_REVIEW_PR = "Please review the changes in branch '{0}' and provide:\n1. Summary of changes\n2. Code quality assessment\n3. Potential issues\n4. Suggestions for improvement";
    
    // Descriptions
    private const string DESC_PROMPT_ANALYZE = "Analyzes C# code for quality and improvements";
    private const string DESC_PROMPT_EXPLAIN = "Explains LSP concepts in detail";
    private const string DESC_PROMPT_REVIEW = "Reviews code changes in a git branch";
    
    // Error messages
    private const string ERROR_UNKNOWN_PROMPT = "Unknown prompt: ";
    private const string ERROR_MISSING_ARG = "Either filePath or code argument is required";
    
    // Log messages
    private const string LOG_GETTING_PROMPT = "Getting prompt: {PromptName}";

    public GetPromptHandler(ILogger<GetPromptHandler> logger)
    {
        _logger = logger;
    }

    public Task<GetPromptResult> Handle(GetPromptParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LOG_GETTING_PROMPT, request.Name);
        
        var result = request.Name switch
        {
            PROMPT_ANALYZE_CODE => GetAnalyzeCodePrompt(request.Arguments),
            PROMPT_EXPLAIN_LSP => GetExplainLspPrompt(request.Arguments),
            PROMPT_REVIEW_PR => GetReviewPrPrompt(request.Arguments),
            _ => throw new System.NotSupportedException(ERROR_UNKNOWN_PROMPT + request.Name)
        };

        return Task.FromResult(result);
    }

    private GetPromptResult GetAnalyzeCodePrompt(Dictionary<string, string>? arguments)
    {
        var filePath = arguments?.GetValueOrDefault(ARG_FILE_PATH);
        var code = arguments?.GetValueOrDefault(ARG_CODE);

        if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(code))
        {
            throw new System.ArgumentException(ERROR_MISSING_ARG);
        }

        var promptText = !string.IsNullOrEmpty(filePath) 
            ? string.Format(TEMPLATE_ANALYZE_FILE, filePath)
            : string.Format(TEMPLATE_ANALYZE_CODE, code);

        return new GetPromptResult
        {
            Description = DESC_PROMPT_ANALYZE,
            Messages = new List<PromptMessage>
            {
                new()
                {
                    Role = ROLE_USER,
                    Content = new PromptContent
                    {
                        Type = CONTENT_TYPE_TEXT,
                        Text = promptText
                    }
                }
            }
        };
    }

    private GetPromptResult GetExplainLspPrompt(Dictionary<string, string>? arguments)
    {
        var concept = arguments?.GetValueOrDefault(ARG_CONCEPT) ?? string.Empty;
        
        return new GetPromptResult
        {
            Description = DESC_PROMPT_EXPLAIN,
            Messages = new List<PromptMessage>
            {
                new()
                {
                    Role = ROLE_USER,
                    Content = new PromptContent
                    {
                        Type = CONTENT_TYPE_TEXT,
                        Text = string.Format(TEMPLATE_EXPLAIN_LSP, concept)
                    }
                }
            }
        };
    }

    private GetPromptResult GetReviewPrPrompt(Dictionary<string, string>? arguments)
    {
        var branch = arguments?.GetValueOrDefault(ARG_BRANCH) ?? string.Empty;
        
        return new GetPromptResult
        {
            Description = DESC_PROMPT_REVIEW,
            Messages = new List<PromptMessage>
            {
                new()
                {
                    Role = ROLE_USER,
                    Content = new PromptContent
                    {
                        Type = CONTENT_TYPE_TEXT,
                        Text = string.Format(TEMPLATE_REVIEW_PR, branch)
                    }
                }
            }
        };
    }
}