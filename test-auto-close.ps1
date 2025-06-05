#!/usr/bin/env pwsh
# Test script to launch the feedback app with a prompt and test the auto-close feature

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$FeedbackAppExe = Join-Path $ScriptDir "FeedbackApp\bin\Release\net9.0-windows\FeedbackApp.exe"
$OutputPath = Join-Path $ScriptDir "auto_close_test_output.json"

# Title and Markdown content to test
$WindowTitle = "Auto-Close Test"
$PromptText = @"
# Auto-Close Test

This window will **automatically close** after 15 seconds if no feedback is provided.

When it auto-closes, it will submit a default message: "User did not provide feedback".

You can test by:
1. Waiting for auto-close (do nothing)
2. Entering feedback and verifying the timer stops
"@

# Make sure the exe exists
if (-not (Test-Path $FeedbackAppExe)) {
    Write-Host "FeedbackApp.exe not found at: $FeedbackAppExe" -ForegroundColor Red
    Write-Host "Please build the app first with: dotnet build -c Release" -ForegroundColor Yellow
    exit 1
}

# Close any existing instances
Stop-Process -Name "FeedbackApp" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Launch the app
Write-Host "Launching FeedbackApp with auto-close timer..."
& $FeedbackAppExe $WindowTitle $PromptText $OutputPath

# Wait for feedback file to be generated
$MaxWaitTime = 25 # Slightly longer than the auto-close timer
$WaitInterval = 1
$ElapsedTime = 0

while ((-not (Test-Path $OutputPath)) -and ($ElapsedTime -lt $MaxWaitTime)) {
    Start-Sleep -Seconds $WaitInterval
    $ElapsedTime += $WaitInterval
    Write-Host "Waiting for feedback file... ($ElapsedTime seconds elapsed)"
}

if (Test-Path $OutputPath) {
    Write-Host "Feedback file found: $OutputPath" -ForegroundColor Green
    
    # Display the content of the feedback file
    $FeedbackContent = Get-Content $OutputPath -Raw | ConvertFrom-Json
    Write-Host "Feedback text: $($FeedbackContent.text)" -ForegroundColor Cyan
    
    # If it matches our default message, it was auto-closed
    if ($FeedbackContent.text -eq "User did not provide feedback") {
        Write-Host "Auto-close successful! Default message received." -ForegroundColor Green
    } else {
        Write-Host "User provided custom feedback." -ForegroundColor Yellow
    }
} else {
    Write-Host "Feedback file not found after $MaxWaitTime seconds." -ForegroundColor Red
} 