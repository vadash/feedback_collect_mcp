#!/usr/bin/env pwsh
# Test script for the MCP server with the feedback app

# Step 1: Ensure both components are built
Write-Host "Checking if components are built..." -ForegroundColor Cyan

$mcpServerPath = Join-Path (Get-Location) "dist\index.js"
$wpfAppPath = Join-Path (Get-Location) "FeedbackApp\bin\Release\net8.0-windows\FeedbackApp.exe"

$needBuild = $false

if (-not (Test-Path $mcpServerPath)) {
    Write-Host "MCP server not built. Please run .\build.ps1 first." -ForegroundColor Red
    $needBuild = $true
}

if (-not (Test-Path $wpfAppPath)) {
    Write-Host "WPF application not built. Please run .\build.ps1 first." -ForegroundColor Red
    $needBuild = $true
}

if ($needBuild) {
    exit 1
}

Write-Host "All components are built." -ForegroundColor Green

# Step 2: Test the get_time tool directly
Write-Host "`nTesting get_time tool..." -ForegroundColor Cyan

Write-Host "Running MCP server with get_time tool call (default parameters)..." -ForegroundColor Yellow
$getTimeRequest = @{
    jsonrpc = "2.0"
    id = [Guid]::NewGuid().ToString()
    method = "callTool"
    params = @{
        name = "get_time"
        args = @{}
    }
} | ConvertTo-Json -Depth 5

# We'll use a temporary file to avoid stdin issues
$requestFile = Join-Path $env:TEMP "get-time-request.json"
$getTimeRequest | Out-File -FilePath $requestFile -Encoding utf8

# Execute the request
$getTimeResult = Get-Content $requestFile | node $mcpServerPath
Write-Host "get_time result:" -ForegroundColor Green
Write-Host $getTimeResult
Write-Host ""

# Test with different format
Write-Host "Testing get_time tool with ISO format..." -ForegroundColor Yellow
$getTimeIsoRequest = @{
    jsonrpc = "2.0"
    id = [Guid]::NewGuid().ToString()
    method = "callTool"
    params = @{
        name = "get_time"
        args = @{
            format = "iso"
        }
    }
} | ConvertTo-Json -Depth 5

# Update the request file
$getTimeIsoRequest | Out-File -FilePath $requestFile -Encoding utf8

# Execute the request
$getTimeIsoResult = Get-Content $requestFile | node $mcpServerPath
Write-Host "get_time ISO format result:" -ForegroundColor Green
Write-Host $getTimeIsoResult
Write-Host ""

# Clean up
Remove-Item $requestFile -Force

# Step 3: Prepare feedback directory
$feedbackDir = Join-Path (Get-Location) "feedback_data"
if (-not (Test-Path $feedbackDir)) {
    Write-Host "Creating feedback data directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $feedbackDir -Force | Out-Null
}

# Step 4: Test launching the WPF app directly with the same parameters the MCP server would use
Write-Host "`nTesting WPF application with MCP-like parameters..." -ForegroundColor Cyan

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$feedbackFile = Join-Path $feedbackDir "mcp_test_${timestamp}.json"
$title = "MCP Feedback Test"
$prompt = "Please provide your feedback (This simulates the MCP server launching the app):"

Write-Host "Using parameters:" -ForegroundColor Yellow
Write-Host "  Title: $title" -ForegroundColor Yellow
Write-Host "  Prompt: $prompt" -ForegroundColor Yellow
Write-Host "  Output File: $feedbackFile" -ForegroundColor Yellow
Write-Host ""

if (Test-Path $feedbackFile) {
    Remove-Item $feedbackFile -Force
}

# Launch the WPF application with the parameters
Write-Host "Launching application..." -ForegroundColor Yellow
& $wpfAppPath $title $prompt $feedbackFile

# Check if feedback was submitted
if (Test-Path $feedbackFile) {
    Write-Host "`nFeedback received!" -ForegroundColor Green
    $feedbackContent = Get-Content $feedbackFile -Raw | ConvertFrom-Json
    
    Write-Host "User Feedback:" -ForegroundColor Green
    Write-Host $feedbackContent.text
    
    # Check for new format with multiple images
    if ($null -ne $feedbackContent.hasImages) {
        if ($feedbackContent.hasImages -and $feedbackContent.images -and $feedbackContent.images.Count -gt 0) {
            Write-Host "$($feedbackContent.imageCount) images were attached:" -ForegroundColor Green
            foreach ($image in $feedbackContent.images) {
                Write-Host "  - $($image.path) (Type: $($image.type))" -ForegroundColor Green
            }
        } else {
            Write-Host "No images were attached." -ForegroundColor Yellow
        }
    }
    # Check for old format with single image for backward compatibility
    elseif ($feedbackContent.hasImage) {
        Write-Host "An image was attached at: $($feedbackContent.imagePath)" -ForegroundColor Green
    } else {
        Write-Host "No images were attached." -ForegroundColor Yellow
    }
    
    Write-Host "`nMCP Server Integration Test: SUCCESS" -ForegroundColor Green
    Write-Host "The WPF app is working correctly with the parameters that the MCP server will pass to it." -ForegroundColor Green
} else {
    Write-Host "`nUser cancelled or no feedback was provided." -ForegroundColor Yellow
    Write-Host "Test cannot be completed. Please try again and submit feedback." -ForegroundColor Yellow
} 