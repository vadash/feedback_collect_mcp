#!/usr/bin/env pwsh
# Test script for directly launching the WPF application with arguments

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$feedbackFile = Join-Path (Get-Location) "feedback_data\direct_test_${timestamp}.json"
$title = "Direct Test"
$prompt = "Please provide your feedback for direct testing:"

Write-Host "Testing direct launching of WPF application..." -ForegroundColor Cyan
Write-Host "Parameters:" -ForegroundColor Cyan
Write-Host "  Title: $title" -ForegroundColor Cyan
Write-Host "  Prompt: $prompt" -ForegroundColor Cyan
Write-Host "  Output File: $feedbackFile" -ForegroundColor Cyan

# Create feedback directory if it doesn't exist
$feedbackDir = Split-Path -Path $feedbackFile -Parent
if (-not (Test-Path $feedbackDir)) {
    Write-Host "Creating feedback directory: $feedbackDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $feedbackDir -Force | Out-Null
}

# Remove existing feedback file if it exists
if (Test-Path $feedbackFile) {
    Write-Host "Removing existing feedback file: $feedbackFile" -ForegroundColor Yellow
    Remove-Item $feedbackFile -Force
}

# Get the path to the WPF application
$appPath = Join-Path (Get-Location) "FeedbackApp\bin\Release\net8.0-windows\FeedbackApp.exe"

# Check if the application exists
if (-not (Test-Path $appPath)) {
    Write-Host "Error: WPF application not found at $appPath" -ForegroundColor Red
    Write-Host "Please build the application first using .\build.ps1" -ForegroundColor Red
    exit 1
}

# Launch the application directly with arguments (exactly how the MCP server will do it)
Write-Host "Launching application directly with arguments..." -ForegroundColor Yellow
& $appPath $title $prompt $feedbackFile

# Wait a moment to ensure the file is fully written
Start-Sleep -Seconds 1

# Check if feedback was provided
if (Test-Path $feedbackFile) {
    Write-Host "Feedback file created successfully!" -ForegroundColor Green
    
    # Try to read the feedback
    try {
        $feedbackContent = Get-Content $feedbackFile -Raw | ConvertFrom-Json
        
        Write-Host "Feedback content:" -ForegroundColor Green
        Write-Host "Text: $($feedbackContent.text)" -ForegroundColor White
        
        # Handle new format with multiple images
        if ($null -ne $feedbackContent.hasImages) {
            Write-Host "Has Images: $($feedbackContent.hasImages)" -ForegroundColor White
            Write-Host "Image Count: $($feedbackContent.imageCount)" -ForegroundColor White
            
            if ($feedbackContent.hasImages -and $feedbackContent.images -and $feedbackContent.images.Count -gt 0) {
                Write-Host "Images:" -ForegroundColor White
                foreach ($image in $feedbackContent.images) {
                    Write-Host "  - Path: $($image.path)" -ForegroundColor White
                    Write-Host "    Type: $($image.type)" -ForegroundColor White
                }
            }
        }
        # Handle old format with single image for backward compatibility
        else {
            Write-Host "Has Image: $($feedbackContent.hasImage)" -ForegroundColor White
            Write-Host "Image Path: $($feedbackContent.imagePath)" -ForegroundColor White
            Write-Host "Image Type: $($feedbackContent.imageType)" -ForegroundColor White
        }
        
        Write-Host "Timestamp: $($feedbackContent.timestamp)" -ForegroundColor White
    }
    catch {
        Write-Host "Error reading feedback file: $_" -ForegroundColor Red
        Write-Host "Raw content:" -ForegroundColor Yellow
        Get-Content $feedbackFile -Raw
    }
}
else {
    Write-Host "Feedback file was not created. The user likely cancelled." -ForegroundColor Yellow
} 