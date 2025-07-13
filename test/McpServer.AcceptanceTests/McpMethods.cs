using OmniSharp.Extensions.McpServer.Protocol;

namespace OmniSharp.McpServer.AcceptanceTests
{
    /// <summary>
    /// Helper class to provide easy access to MCP method names
    /// </summary>
    internal static class McpMethods
    {
        public const string Initialize = McpConstants.METHOD_INITIALIZE;
        public const string Initialized = "initialized";
        public const string ToolsList = McpConstants.METHOD_TOOLS_LIST;
        public const string ResourcesList = McpConstants.METHOD_RESOURCES_LIST;
        public const string PromptsList = McpConstants.METHOD_PROMPTS_LIST;
    }
}