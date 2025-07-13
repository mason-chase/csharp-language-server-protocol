#!/bin/bash

# Test MCP Server initialization
echo "Testing MCP Server..."

# Send initialize request
cat <<EOF | dotnet run --project src/McpServer/McpServer.csproj
Content-Length: 157

{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client"}},"id":1}
EOF