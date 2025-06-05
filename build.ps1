#!/usr/bin/env pwsh
# Build script for the User Feedback MCP Server and WPF application

Write-Host "Building User Feedback Collection MCP Server..." -ForegroundColor Green

# Check if Node.js is installed
try {
    $nodeVersion = node -v
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Cyan
} catch {
    Write-Host "Error: Node.js is not installed or not in PATH. Please install Node.js 16.0.0 or higher." -ForegroundColor Red
    exit 1
}

# Check if .NET 9 SDK is installed
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -notlike "9.*") {
        Write-Host "Warning: .NET version is $dotnetVersion. This project requires .NET 9.0 SDK." -ForegroundColor Yellow
    } else {
        Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Cyan
    }
} catch {
    Write-Host "Error: .NET SDK is not installed or not in PATH. Please install .NET 9.0 SDK." -ForegroundColor Red
    exit 1
}

# Create folders if they don't exist
if (-not (Test-Path -Path "dist")) {
    New-Item -ItemType Directory -Path "dist" | Out-Null
}
if (-not (Test-Path -Path "feedback_data")) {
    New-Item -ItemType Directory -Path "feedback_data" | Out-Null
}

# Install Node.js dependencies
Write-Host "Installing Node.js dependencies..." -ForegroundColor Cyan
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error installing Node.js dependencies. Please check the error messages above." -ForegroundColor Red
    exit 1
}

# Build the TypeScript MCP server
Write-Host "Building TypeScript MCP server..." -ForegroundColor Cyan
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error building TypeScript MCP server. Please check the error messages above." -ForegroundColor Red
    exit 1
}

# Build the WPF application
Write-Host "Building WPF Feedback Application..." -ForegroundColor Cyan
Push-Location -Path "FeedbackApp"
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error building WPF application. Please check the error messages above." -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

# Verify the built binaries exist
if (-not (Test-Path -Path "dist/index.js")) {
    Write-Host "Error: MCP server build failed. dist/index.js not found." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -Path "FeedbackApp/bin/Release/net9.0-windows/FeedbackApp.exe")) {
    Write-Host "Error: WPF application build failed. FeedbackApp.exe not found." -ForegroundColor Red
    exit 1
}

# Create a sample configuration file for Claude Desktop
$configPath = "claude-desktop-config-example.json"
$absolutePathToProject = (Get-Item -Path ".").FullName
$configContent = @{
    mcpServers = @{
        userFeedback = @{
            command = "node"
            args = @("$absolutePathToProject/dist/index.js")
        }
    }
} | ConvertTo-Json -Depth 3

Set-Content -Path $configPath -Value $configContent

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Sample Claude Desktop configuration saved to: $configPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "To use this MCP server with Claude Desktop:" -ForegroundColor White
Write-Host "1. Copy the configuration from $configPath to your Claude Desktop config file" -ForegroundColor White
Write-Host "2. Update the path in the configuration to point to your project location" -ForegroundColor White 