#!/usr/bin/env pwsh
# Simple integration test for the get_time tool

Write-Host "Testing get_time tool..." -ForegroundColor Cyan
Write-Host ""

# Create a simple JSON file to simulate a tool call
$toolCallFile = Join-Path (Get-Location) "test-get-time-call.json"

# Define tool call parameters to test
$toolCalls = @(
    @{ desc = "Default format"; params = @{} },
    @{ desc = "ISO format"; params = @{ format = "iso" } },
    @{ desc = "Date format"; params = @{ format = "date" } },
    @{ desc = "Time format"; params = @{ format = "time" } },
    @{ desc = "Unix timestamp"; params = @{ format = "unix" } },
    @{ desc = "UTC timezone"; params = @{ timezone = "UTC" } }
)

foreach ($call in $toolCalls) {
    Write-Host "Testing get_time with $($call.desc)..." -ForegroundColor Yellow
    
    # Create the tool call payload
    $toolCallJson = @{
        jsonrpc = "2.0"
        id = [Guid]::NewGuid().ToString()
        method = "callTool"
        params = @{
            name = "get_time"
            args = $call.params
        }
    } | ConvertTo-Json -Depth 5
    
    # Save the tool call to a file
    $toolCallJson | Out-File -FilePath $toolCallFile -Encoding utf8
    
    # Execute the tool call by piping it to the MCP server
    $result = Get-Content $toolCallFile | node dist/index.js 2>$null
    
    # Display the result
    Write-Host "Result:" -ForegroundColor Green
    Write-Host $result
    Write-Host "---------------------------------------------" -ForegroundColor Gray
    Write-Host ""
}

# Clean up
Remove-Item $toolCallFile -Force
Write-Host "All tests completed." -ForegroundColor Green 