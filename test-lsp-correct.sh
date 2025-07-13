#!/bin/bash

# Correctly formatted LSP test script
# This sends properly formatted LSP messages with Content-Length headers

SERVER_PATH="/Users/mason/resources/omnisharp-csharp-lsp/sample/SampleServer/bin/Debug/net8.0/SampleServer.dll"
DOTNET="/usr/local/share/dotnet/dotnet"

# Function to send a properly formatted LSP message
send_lsp_message() {
    local json="$1"
    local length=$(echo -n "$json" | wc -c | tr -d ' ')
    printf "Content-Length: %d\r\n\r\n%s" "$length" "$json"
}

# Create a test session
{
    # Initialize request with proper capabilities
    send_lsp_message '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"capabilities":{"textDocument":{"foldingRange":{"dynamicRegistration":true},"semanticTokens":{"dynamicRegistration":true,"requests":{"full":true},"tokenTypes":["namespace","type","class","enum","interface","struct","typeParameter","parameter","variable","property","enumMember","event","function","method","macro","keyword","modifier","comment","string","number","regexp","operator"],"tokenModifiers":["declaration","definition","readonly","static","deprecated","abstract","async","modification","documentation","defaultLibrary"]}}},"rootUri":"file:///Users/mason/resources/omnisharp-csharp-lsp"}}'
    
    sleep 2
    
    # Initialized notification (this was missing the header before)
    send_lsp_message '{"jsonrpc":"2.0","method":"initialized","params":{}}'
    
    sleep 1
    
    # Open document
    send_lsp_message '{"jsonrpc":"2.0","method":"textDocument/didOpen","params":{"textDocument":{"uri":"file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs","languageId":"csharp","version":1,"text":"using System;\n\nnamespace TestSample\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"Hello from Language Server!\");\n            \n            // Test folding regions\n            #region Test Region\n            var x = 10;\n            var y = 20;\n            var sum = x + y;\n            #endregion\n            \n            // Test method\n            TestMethod();\n        }\n        \n        static void TestMethod()\n        {\n            // This should trigger the language server\n            var message = \"Testing LSP features\";\n            Console.WriteLine(message);\n        }\n    }\n}"}}}'
    
    sleep 1
    
    # Request folding ranges
    send_lsp_message '{"jsonrpc":"2.0","id":2,"method":"textDocument/foldingRange","params":{"textDocument":{"uri":"file:///Users/mason/resources/omnisharp-csharp-lsp/test-sample.cs"}}}'
    
    sleep 2
    
    # Shutdown
    send_lsp_message '{"jsonrpc":"2.0","id":99,"method":"shutdown","params":null}'
    
    sleep 1
    
    # Exit
    send_lsp_message '{"jsonrpc":"2.0","method":"exit","params":null}'
    
} | $DOTNET "$SERVER_PATH" 2>&1 | tee lsp-test-output.log

echo ""
echo "Test completed. Output saved to lsp-test-output.log"
echo ""
echo "To see just the responses, run:"
echo "grep -A5 'Content-Length' lsp-test-output.log"