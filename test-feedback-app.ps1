#!/usr/bin/env pwsh
# Test script for the Feedback Application

$outputFile = Join-Path (Get-Location) "feedback_test.json"
$title = "Test Feedback Collection"
$prompt = "Please provide your feedback about the application:"

Write-Host "Launching Feedback Application with:" -ForegroundColor Cyan
Write-Host "  Title: $title" -ForegroundColor Cyan
Write-Host "  Prompt: $prompt" -ForegroundColor Cyan
Write-Host "  Output File: $outputFile" -ForegroundColor Cyan
Write-Host ""

# Ensure the output directory exists
$outputDir = Split-Path -Path $outputFile -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDir) -and -not (Test-Path $outputDir)) {
    Write-Host "Creating output directory: $outputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$appPath = Join-Path (Get-Location) "FeedbackApp\bin\Release\net8.0-windows\FeedbackApp.exe"

# Check if the executable exists
if (-not (Test-Path $appPath)) {
    Write-Host "Error: FeedbackApp.exe not found at $appPath" -ForegroundColor Red
    Write-Host "Please build the application first using .\build.ps1" -ForegroundColor Red
    exit 1
}

# Launch the application
Write-Host "Launching application..." -ForegroundColor Yellow
& $appPath $title $prompt $outputFile

# Check if the user submitted feedback
if (Test-Path $outputFile) {
    Write-Host "Feedback received!" -ForegroundColor Green
    $feedbackContent = Get-Content $outputFile -Raw | ConvertFrom-Json
    
    Write-Host "User Feedback:" -ForegroundColor Green
    Write-Host $feedbackContent.text
    
    # Check for images in the new format
    if ($feedbackContent.hasImages -and $feedbackContent.images -and $feedbackContent.images.Count -gt 0) {
        Write-Host "$($feedbackContent.imageCount) images were attached:" -ForegroundColor Green
        foreach ($image in $feedbackContent.images) {
            Write-Host "  - $($image.path) (Type: $($image.type))" -ForegroundColor Green
        }
    }
    # Check for image in the old format for backward compatibility
    elseif ($feedbackContent.hasImage -and $feedbackContent.imagePath) {
        Write-Host "An image was attached at: $($feedbackContent.imagePath)" -ForegroundColor Green
    } 
    else {
        Write-Host "No images were attached." -ForegroundColor Yellow
    }
} else {
    Write-Host "User cancelled or no feedback was provided." -ForegroundColor Yellow
} 