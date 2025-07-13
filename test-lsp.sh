#!/bin/bash

# Start the language server and send LSP messages

# Path to the sample server
SERVER_PATH="/Users/mason/resources/omnisharp-csharp-lsp/sample/SampleServer/bin/Debug/net8.0/SampleServer.dll"

# Create a test initialization request
cat << 'EOF' | /usr/local/share/dotnet/dotnet "$SERVER_PATH"
Content-Length: 2062

{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"clientInfo":{"name":"Test Client","version":"1.0.0"},"rootPath":"/Users/mason/resources/omnisharp-csharp-lsp","rootUri":"file:///Users/mason/resources/omnisharp-csharp-lsp","capabilities":{"workspace":{"applyEdit":true,"workspaceEdit":{"documentChanges":true},"didChangeConfiguration":{"dynamicRegistration":true},"didChangeWatchedFiles":{"dynamicRegistration":true},"symbol":{"dynamicRegistration":true},"executeCommand":{"dynamicRegistration":true}},"textDocument":{"synchronization":{"dynamicRegistration":true,"willSave":true,"willSaveWaitUntil":true,"didSave":true},"completion":{"dynamicRegistration":true,"completionItem":{"snippetSupport":true}},"hover":{"dynamicRegistration":true},"signatureHelp":{"dynamicRegistration":true},"definition":{"dynamicRegistration":true},"references":{"dynamicRegistration":true},"documentHighlight":{"dynamicRegistration":true},"documentSymbol":{"dynamicRegistration":true},"codeAction":{"dynamicRegistration":true},"codeLens":{"dynamicRegistration":true},"formatting":{"dynamicRegistration":true},"rangeFormatting":{"dynamicRegistration":true},"onTypeFormatting":{"dynamicRegistration":true},"rename":{"dynamicRegistration":true},"documentLink":{"dynamicRegistration":true},"typeDefinition":{"dynamicRegistration":true},"implementation":{"dynamicRegistration":true},"colorProvider":{"dynamicRegistration":true},"foldingRange":{"dynamicRegistration":true,"rangeLimit":5000,"lineFoldingOnly":true}}},"trace":"verbose","workspaceFolders":[{"uri":"file:///Users/mason/resources/omnisharp-csharp-lsp","name":"omnisharp-csharp-lsp"}]}}
Content-Length: 52

{"jsonrpc":"2.0","method":"initialized","params":{}}
Content-Length: 358

{"jsonrpc":"2.0","method":"textDocument/didOpen","params":{"textDocument":{"uri":"file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs","languageId":"csharp","version":1,"text":"using System;\n\nnamespace TestSample\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"Hello from Language Server!\");\n        }\n    }\n}"}}}
Content-Length: 93

{"jsonrpc":"2.0","id":2,"method":"textDocument/foldingRange","params":{"textDocument":{"uri":"file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs"}}}
EOF