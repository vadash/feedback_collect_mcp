#!/usr/bin/env pwsh
# Test script to launch the feedback app with markdown content to test rendering

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$FeedbackAppExe = Join-Path $ScriptDir "FeedbackApp\bin\Release\net9.0-windows\FeedbackApp.exe"
$OutputPath = Join-Path $ScriptDir "test_markdown_output.json"

# Title and Markdown content to test
$WindowTitle = "Implementation Plan for Email Alias Generator"
$PromptText = @"
Based on my analysis of the codebase, I'd like to implement an email alias generator with the following
components and features:

1. **Core Functionality:**
- Email input field with validation for Gmail addresses
- Algorithm to generate all possible dot aliases (e.g., t.est@gmail.com, te.st@gmail.com)
- Copy to clipboard functionality
- Status tracking system (available/used aliases)
- Local storage to persist user data

2. **UI Components:**
- Modern, futuristic interface with glowing accents and particle effects
- Full viewport responsive design with centered focus elements
- Two main sections: input area and aliases display grid
- Color-coded aliases (green for available, amber for used)
- Copy button on each alias with visual feedback

3. **Technical Implementation:**
- React for the UI components
- IndexedDB via idb package for persistent storage
- Custom hook for alias generation algorithm
- React context for state management
- Responsive design with CSS Grid and Flexbox

4. **File Structure:**
- src/components/ - UI components
- src/hooks/ - Custom hooks for business logic
- src/context/ - Global state management
- src/utils/ - Helper functions
- src/styles/ - CSS modules

Is this plan aligned with your vision for the email alias generator? Would you like me to adjust any
aspects before I start implementation?
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
Write-Host "Launching FeedbackApp with markdown content..."
& $FeedbackAppExe $WindowTitle $PromptText $OutputPath 