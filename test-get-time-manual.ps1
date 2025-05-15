#!/usr/bin/env pwsh
# Manual test script for the get_time tool in the MCP server

# Create a test file for the MCP server to use
$testFilePath = Join-Path $env:TEMP "mcp-get-time-test.json"

# Function to test the get_time tool
function Test-GetTime {
    param (
        [string]$description,
        [hashtable]$parameters = @{}
    )
    
    Write-Host "Testing get_time with $description..." -ForegroundColor Yellow
    
    # Create a formatted object string for the parameters
    $paramStr = ($parameters.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" }) -join ", "
    
    # Start the MCP server directly
    $command = "node dist/index.js"
    
    Write-Host "Running MCP server and sending get_time request with parameters: { $paramStr }" -ForegroundColor Cyan
    
    # Run the command and capture output in a separate PowerShell process
    $output = powershell -Command {
        # Run the server
        $proc = Start-Process -FilePath "node" -ArgumentList "dist/index.js" -NoNewWindow -PassThru -RedirectStandardInput "$env:TEMP\mcp-get-time-input.txt" -RedirectStandardOutput "$env:TEMP\mcp-get-time-output.txt"
        
        # Wait for the server to initialize
        Start-Sleep -Seconds 1
        
        # Create the request
        $requestId = [Guid]::NewGuid().ToString()
        $request = @{
            jsonrpc = "2.0"
            id = $requestId
            method = "callTool"
            params = @{
                name = "get_time"
                args = $using:parameters
            }
        } | ConvertTo-Json -Depth 5
        
        # Send the request
        $request | Out-File -FilePath "$env:TEMP\mcp-get-time-input.txt" -Encoding utf8
        
        # Wait for processing
        Start-Sleep -Seconds 2
        
        # Read response
        $response = Get-Content -Path "$env:TEMP\mcp-get-time-output.txt" -Raw
        
        # Stop the process
        Stop-Process -Id $proc.Id -Force
        
        # Return the response
        return $response
    }
    
    # Display the output
    Write-Host "Response:" -ForegroundColor Green
    Write-Host $output
    Write-Host "---------------------------------" -ForegroundColor Gray
}

# Run tests with different parameters
Test-GetTime -description "default parameters" -parameters @{}
Test-GetTime -description "ISO format" -parameters @{ format = "iso" }
Test-GetTime -description "date format" -parameters @{ format = "date" }
Test-GetTime -description "time format" -parameters @{ format = "time" }
Test-GetTime -description "unix format" -parameters @{ format = "unix" }
Test-GetTime -description "UTC timezone" -parameters @{ timezone = "UTC" }

Write-Host "All tests completed." -ForegroundColor Green 