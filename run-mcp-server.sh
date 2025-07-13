#!/bin/bash

# MCP Server Launcher Script
# This script ensures the dotnet command is available and runs the MCP server

# Check if dotnet is in PATH
if ! command -v dotnet &> /dev/null; then
    # Try common dotnet installation paths
    if [ -x "/usr/local/share/dotnet/dotnet" ]; then
        export PATH="/usr/local/share/dotnet:$PATH"
    elif [ -x "$HOME/.dotnet/dotnet" ]; then
        export PATH="$HOME/.dotnet:$PATH"
    else
        echo "Error: dotnet SDK not found. Please install .NET SDK from https://dotnet.microsoft.com/download" >&2
        exit 1
    fi
fi

# Verify dotnet is working
if ! dotnet --version &> /dev/null; then
    echo "Error: dotnet command is not working properly" >&2
    exit 1
fi

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Create log directory if it doesn't exist
mkdir -p "$SCRIPT_DIR/logs"

# Build the project first (suppress output unless there are errors)
cd "$SCRIPT_DIR"
echo "Building MCP server..." >&2
BUILD_OUTPUT=$(dotnet build src/McpServer/McpServer.csproj --verbosity quiet 2>&1)
BUILD_EXIT_CODE=$?

if [ $BUILD_EXIT_CODE -ne 0 ]; then
    echo "Build failed:" >&2
    echo "$BUILD_OUTPUT" >&2
    exit $BUILD_EXIT_CODE
fi

# Run the MCP server
# --no-build: Skip building since we already built
# --verbosity quiet: Suppress build output
# 2>: Redirect stderr to log file
exec dotnet run --project src/McpServer/McpServer.csproj --no-build --verbosity quiet "$@" 2>"$SCRIPT_DIR/logs/mcp-server-stderr.log"