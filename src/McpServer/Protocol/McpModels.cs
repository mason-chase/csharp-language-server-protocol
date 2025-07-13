using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;

namespace OmniSharp.Extensions.McpServer.Protocol;

// MCP Protocol Models based on https://github.com/modelcontextprotocol/specification

public static class McpConstants
{
    // Protocol version
    public const string PROTOCOL_VERSION = "2024-11-05";

    // Method names
    public const string METHOD_INITIALIZE = "initialize";
    public const string METHOD_TOOLS_LIST = "tools/list";
    public const string METHOD_TOOLS_CALL = "tools/call";
    public const string METHOD_RESOURCES_LIST = "resources/list";
    public const string METHOD_RESOURCES_READ = "resources/read";
    public const string METHOD_PROMPTS_LIST = "prompts/list";
    public const string METHOD_PROMPTS_GET = "prompts/get";

    // JSON property names
    public const string PROP_PROTOCOL_VERSION = "protocolVersion";
    public const string PROP_CAPABILITIES = "capabilities";
    public const string PROP_CLIENT_INFO = "clientInfo";
    public const string PROP_SERVER_INFO = "serverInfo";
    public const string PROP_EXPERIMENTAL = "experimental";
    public const string PROP_TOOLS = "tools";
    public const string PROP_RESOURCES = "resources";
    public const string PROP_PROMPTS = "prompts";
    public const string PROP_LOGGING = "logging";
    public const string PROP_SUBSCRIBE = "subscribe";
    public const string PROP_LIST_CHANGED = "listChanged";
    public const string PROP_NAME = "name";
    public const string PROP_VERSION = "version";
    public const string PROP_DESCRIPTION = "description";
    public const string PROP_INPUT_SCHEMA = "inputSchema";
    public const string PROP_ARGUMENTS = "arguments";
    public const string PROP_CONTENT = "content";
    public const string PROP_CONTENTS = "contents";
    public const string PROP_IS_ERROR = "isError";
    public const string PROP_TYPE = "type";
    public const string PROP_TEXT = "text";
    public const string PROP_URI = "uri";
    public const string PROP_MIME_TYPE = "mimeType";
    public const string PROP_BLOB = "blob";
    public const string PROP_REQUIRED = "required";
    public const string PROP_ROLE = "role";
    public const string PROP_MESSAGES = "messages";

    // Default values
    public const string DEFAULT_CLIENT_NAME = "Unknown";
    public const string DEFAULT_SERVER_NAME = "OmniSharp MCP Server";
    public const string DEFAULT_SERVER_VERSION = "1.0.0";
    public const string DEFAULT_CONTENT_TYPE = "text";
    public const string DEFAULT_ROLE = "user";
}

[Method(McpConstants.METHOD_INITIALIZE)]
public class InitializeParams : IRequest<InitializeResult>
{
    [JsonProperty(McpConstants.PROP_PROTOCOL_VERSION)]
    public string ProtocolVersion { get; set; } = McpConstants.PROTOCOL_VERSION;

    [JsonProperty(McpConstants.PROP_CAPABILITIES)]
    public ClientCapabilities Capabilities { get; set; } = new();

    [JsonProperty(McpConstants.PROP_CLIENT_INFO)]
    public ClientInfo ClientInfo { get; set; } = new();
}

public class InitializeResult
{
    [JsonProperty(McpConstants.PROP_PROTOCOL_VERSION)]
    public string ProtocolVersion { get; set; } = McpConstants.PROTOCOL_VERSION;

    [JsonProperty(McpConstants.PROP_CAPABILITIES)]
    public ServerCapabilities Capabilities { get; set; } = new();

    [JsonProperty(McpConstants.PROP_SERVER_INFO)]
    public ServerInfo ServerInfo { get; set; } = new();
}

public class ClientCapabilities
{
    [JsonProperty(McpConstants.PROP_EXPERIMENTAL)]
    public JObject? Experimental { get; set; }
}

public class ServerCapabilities
{
    [JsonProperty(McpConstants.PROP_TOOLS)]
    public JObject? Tools { get; set; }
    
    [JsonProperty(McpConstants.PROP_RESOURCES)]
    public ResourcesCapability? Resources { get; set; }
    
    [JsonProperty(McpConstants.PROP_PROMPTS)]
    public PromptsCapability? Prompts { get; set; }
    
    [JsonProperty(McpConstants.PROP_LOGGING)]
    public JObject? Logging { get; set; }
}

public class ResourcesCapability
{
    [JsonProperty(McpConstants.PROP_SUBSCRIBE)]
    public bool Subscribe { get; set; }
    
    [JsonProperty(McpConstants.PROP_LIST_CHANGED)]
    public bool ListChanged { get; set; }
}

public class PromptsCapability
{
    [JsonProperty(McpConstants.PROP_LIST_CHANGED)]
    public bool ListChanged { get; set; }
}

public class ClientInfo
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = McpConstants.DEFAULT_CLIENT_NAME;
    
    [JsonProperty(McpConstants.PROP_VERSION)]
    public string? Version { get; set; }
}

public class ServerInfo
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = McpConstants.DEFAULT_SERVER_NAME;
    
    [JsonProperty(McpConstants.PROP_VERSION)]
    public string Version { get; set; } = McpConstants.DEFAULT_SERVER_VERSION;
}

[Method(McpConstants.METHOD_TOOLS_LIST)]
public class ListToolsParams : IRequest<ListToolsResult>
{
}

public class ListToolsResult
{
    [JsonProperty(McpConstants.PROP_TOOLS)]
    public List<Tool> Tools { get; set; } = new();
}

public class Tool
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_DESCRIPTION)]
    public string? Description { get; set; }
    
    [JsonProperty(McpConstants.PROP_INPUT_SCHEMA)]
    public JObject InputSchema { get; set; } = new();
}

[Method(McpConstants.METHOD_TOOLS_CALL)]
public class CallToolParams : IRequest<CallToolResult>
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_ARGUMENTS)]
    public JObject? Arguments { get; set; }
}

public class CallToolResult
{
    [JsonProperty(McpConstants.PROP_CONTENT)]
    public List<ToolContent> Content { get; set; } = new();
    
    [JsonProperty(McpConstants.PROP_IS_ERROR)]
    public bool? IsError { get; set; }
}

public class ToolContent
{
    [JsonProperty(McpConstants.PROP_TYPE)]
    public string Type { get; set; } = McpConstants.DEFAULT_CONTENT_TYPE;
    
    [JsonProperty(McpConstants.PROP_TEXT)]
    public string? Text { get; set; }
}

[Method(McpConstants.METHOD_RESOURCES_LIST)]
public class ListResourcesParams : IRequest<ListResourcesResult>
{
}

public class ListResourcesResult
{
    [JsonProperty(McpConstants.PROP_RESOURCES)]
    public List<Resource> Resources { get; set; } = new();
}

public class Resource
{
    [JsonProperty(McpConstants.PROP_URI)]
    public string Uri { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_DESCRIPTION)]
    public string? Description { get; set; }
    
    [JsonProperty(McpConstants.PROP_MIME_TYPE)]
    public string? MimeType { get; set; }
}

[Method(McpConstants.METHOD_RESOURCES_READ)]
public class ReadResourceParams : IRequest<ReadResourceResult>
{
    [JsonProperty(McpConstants.PROP_URI)]
    public string Uri { get; set; } = string.Empty;
}

public class ReadResourceResult
{
    [JsonProperty(McpConstants.PROP_CONTENTS)]
    public List<ResourceContent> Contents { get; set; } = new();
}

public class ResourceContent
{
    [JsonProperty(McpConstants.PROP_URI)]
    public string Uri { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_MIME_TYPE)]
    public string? MimeType { get; set; }
    
    [JsonProperty(McpConstants.PROP_TEXT)]
    public string? Text { get; set; }
    
    [JsonProperty(McpConstants.PROP_BLOB)]
    public string? Blob { get; set; }
}

[Method(McpConstants.METHOD_PROMPTS_LIST)]
public class ListPromptsParams : IRequest<ListPromptsResult>
{
}

public class ListPromptsResult
{
    [JsonProperty(McpConstants.PROP_PROMPTS)]
    public List<Prompt> Prompts { get; set; } = new();
}

public class Prompt
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_DESCRIPTION)]
    public string? Description { get; set; }
    
    [JsonProperty(McpConstants.PROP_ARGUMENTS)]
    public List<PromptArgument>? Arguments { get; set; }
}

public class PromptArgument
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_DESCRIPTION)]
    public string? Description { get; set; }
    
    [JsonProperty(McpConstants.PROP_REQUIRED)]
    public bool? Required { get; set; }
}

[Method(McpConstants.METHOD_PROMPTS_GET)]
public class GetPromptParams : IRequest<GetPromptResult>
{
    [JsonProperty(McpConstants.PROP_NAME)]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty(McpConstants.PROP_ARGUMENTS)]
    public Dictionary<string, string>? Arguments { get; set; }
}

public class GetPromptResult
{
    [JsonProperty(McpConstants.PROP_DESCRIPTION)]
    public string? Description { get; set; }
    
    [JsonProperty(McpConstants.PROP_MESSAGES)]
    public List<PromptMessage> Messages { get; set; } = new();
}

public class PromptMessage
{
    [JsonProperty(McpConstants.PROP_ROLE)]
    public string Role { get; set; } = McpConstants.DEFAULT_ROLE;
    
    [JsonProperty(McpConstants.PROP_CONTENT)]
    public PromptContent Content { get; set; } = new();
}

public class PromptContent
{
    [JsonProperty(McpConstants.PROP_TYPE)]
    public string Type { get; set; } = McpConstants.DEFAULT_CONTENT_TYPE;
    
    [JsonProperty(McpConstants.PROP_TEXT)]
    public string? Text { get; set; }
}
