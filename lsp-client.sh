#!/bin/bash

# Simple LSP client for testing the sample server
# Usage: ./lsp-client.sh [command]
# Commands: init, open, folding, semantic, shutdown

SERVER_DLL="/Users/mason/resources/omnisharp-csharp-lsp/sample/SampleServer/bin/Debug/net8.0/SampleServer.dll"
DOTNET="/usr/local/share/dotnet/dotnet"
TEST_FILE="/Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs"

send_message() {
    local content="$1"
    local length=${#content}
    echo -e "Content-Length: $length\r\n\r\n$content"
}

case "$1" in
    "run")
        # Run server interactively
        echo "Starting language server..."
        echo "Send LSP messages in the format: Content-Length: XX<enter><enter>{json}"
        $DOTNET $SERVER_DLL
        ;;
    
    "test")
        # Run a complete test sequence
        (
            send_message '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"capabilities":{},"rootUri":"file:///Users/mason/resources/omnisharp-csharp-lsp"}}'
            sleep 1
            send_message '{"jsonrpc":"2.0","method":"initialized","params":{}}'
            sleep 0.5
            
            # Read the test file
            TEXT=$(cat "$TEST_FILE" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g' | tr '\n' '\\' | sed 's/\\/\\n/g')
            send_message "{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":\"file://$TEST_FILE\",\"languageId\":\"csharp\",\"version\":1,\"text\":\"$TEXT\"}}}"
            sleep 0.5
            
            send_message "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"textDocument/foldingRange\",\"params\":{\"textDocument\":{\"uri\":\"file://$TEST_FILE\"}}}"
            sleep 1
            
            send_message '{"jsonrpc":"2.0","id":99,"method":"shutdown","params":null}'
            sleep 0.5
            send_message '{"jsonrpc":"2.0","method":"exit","params":null}'
        ) | $DOTNET $SERVER_DLL 2>&1 | grep -E "(Content-Length:|{)" | head -50
        ;;
        
    *)
        echo "Usage: $0 [run|test]"
        echo "  run  - Start the server for manual testing"
        echo "  test - Run an automated test sequence"
        exit 1
        ;;
esac