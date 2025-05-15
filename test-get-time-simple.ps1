#!/usr/bin/env pwsh
# Simple test script for the get_time tool in the MCP server

Write-Host "Testing get_time tool in MCP server with simple approach..." -ForegroundColor Cyan
Write-Host ""

# MCP server executable path
$mcpServerPath = Join-Path (Get-Location) "dist/index.js"

# Function to create a properly formatted MCP request
function Create-McpRequest {
    param (
        [string]$toolName,
        [hashtable]$parameters = @{}
    )
    
    $requestId = [Guid]::NewGuid().ToString()
    
    $request = @{
        jsonrpc = "2.0"
        id = $requestId
        method = "callTool"
        params = @{
            name = $toolName
            args = $parameters
        }
    }
    
    return $request | ConvertTo-Json -Depth 10
}

# Define test cases
$testCases = @(
    @{ Name = "Default format"; Parameters = @{} },
    @{ Name = "ISO format"; Parameters = @{ format = "iso" } },
    @{ Name = "Date format"; Parameters = @{ format = "date" } },
    @{ Name = "Time format"; Parameters = @{ format = "time" } },
    @{ Name = "Unix format"; Parameters = @{ format = "unix" } },
    @{ Name = "With timezone UTC"; Parameters = @{ timezone = "UTC" } }
)

# Run each test case
foreach ($test in $testCases) {
    Write-Host "Test: $($test.Name)" -ForegroundColor Yellow
    
    # Create MCP request
    $request = Create-McpRequest -toolName "get_time" -parameters $test.Parameters
    
    # Start the MCP server process
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = "node"
    $processInfo.Arguments = $mcpServerPath
    $processInfo.RedirectStandardInput = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true  # Optional: capture error output
    $processInfo.UseShellExecute = $false
    
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null
    
    # Send request to the MCP server
    $process.StandardInput.WriteLine($request)
    $process.StandardInput.Flush()
    $process.StandardInput.Close()
    
    # Read and parse the response
    $response = $process.StandardOutput.ReadToEnd()
    
    # Wait for the process to exit
    $process.WaitForExit()
    
    # Parse and display the response
    try {
        $parsedResponse = $response | ConvertFrom-Json
        Write-Host "Response:" -ForegroundColor Green
        $parsedResponse | ConvertTo-Json -Depth 10 | Write-Host
    }
    catch {
        Write-Host "Error parsing response: $_" -ForegroundColor Red
        Write-Host "Raw response: $response" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "All tests completed." -ForegroundColor Green 