#!/usr/bin/env python3

import json
import subprocess
import sys
import time

def send_request(proc, request):
    """Send a request to the language server"""
    content = json.dumps(request)
    content_length = len(content.encode('utf-8'))
    
    message = f"Content-Length: {content_length}\r\n\r\n{content}"
    proc.stdin.write(message.encode('utf-8'))
    proc.stdin.flush()
    print(f"Sent: {request.get('method', request.get('id'))}")

def read_response(proc):
    """Read a response from the language server"""
    # Read headers
    headers = {}
    while True:
        line = proc.stdout.readline().decode('utf-8').strip()
        if not line:
            break
        key, value = line.split(': ', 1)
        headers[key] = value
    
    # Read content
    content_length = int(headers.get('Content-Length', 0))
    if content_length > 0:
        content = proc.stdout.read(content_length).decode('utf-8')
        return json.loads(content)
    return None

def main():
    # Start the language server
    server_path = "/Users/mason/resources/omnisharp-csharp-lsp/sample/SampleServer/bin/Debug/net8.0/SampleServer.dll"
    proc = subprocess.Popen(
        ["/usr/local/share/dotnet/dotnet", server_path],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE
    )
    
    print("Language server started...")
    time.sleep(1)
    
    # Initialize
    init_request = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
            "processId": None,
            "clientInfo": {"name": "Test Client", "version": "1.0.0"},
            "rootUri": "file:///Users/mason/resources/omnisharp-csharp-lsp",
            "capabilities": {
                "textDocument": {
                    "foldingRange": {
                        "dynamicRegistration": True,
                        "rangeLimit": 5000,
                        "lineFoldingOnly": True
                    },
                    "semanticTokens": {
                        "dynamicRegistration": True,
                        "requests": {"full": True}
                    }
                }
            },
            "trace": "verbose"
        }
    }
    
    send_request(proc, init_request)
    response = read_response(proc)
    print(f"Initialize response: {json.dumps(response, indent=2)}")
    
    # Send initialized notification
    send_request(proc, {"jsonrpc": "2.0", "method": "initialized", "params": {}})
    
    # Open a document
    with open("/Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs", "r") as f:
        text = f.read()
    
    did_open = {
        "jsonrpc": "2.0",
        "method": "textDocument/didOpen",
        "params": {
            "textDocument": {
                "uri": "file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs",
                "languageId": "csharp",
                "version": 1,
                "text": text
            }
        }
    }
    
    send_request(proc, did_open)
    time.sleep(0.5)
    
    # Request folding ranges
    folding_request = {
        "jsonrpc": "2.0",
        "id": 2,
        "method": "textDocument/foldingRange",
        "params": {
            "textDocument": {
                "uri": "file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs"
            }
        }
    }
    
    send_request(proc, folding_request)
    response = read_response(proc)
    print(f"\nFolding ranges response: {json.dumps(response, indent=2)}")
    
    # Request semantic tokens
    semantic_tokens_request = {
        "jsonrpc": "2.0",
        "id": 3,
        "method": "textDocument/semanticTokens/full",
        "params": {
            "textDocument": {
                "uri": "file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs"
            }
        }
    }
    
    send_request(proc, semantic_tokens_request)
    response = read_response(proc)
    print(f"\nSemantic tokens response: {json.dumps(response, indent=2)}")
    
    # Shutdown
    send_request(proc, {"jsonrpc": "2.0", "id": 99, "method": "shutdown", "params": None})
    response = read_response(proc)
    print(f"\nShutdown response: {response}")
    
    # Exit
    send_request(proc, {"jsonrpc": "2.0", "method": "exit", "params": None})
    
    proc.terminate()
    print("\nLanguage server terminated.")

if __name__ == "__main__":
    main()