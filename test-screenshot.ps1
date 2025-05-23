#!/usr/bin/env pwsh
# Test script for the screenshot functionality in the MCP server

# Define the directory path
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Create a simple HTTP server serving a test page to screenshot
$TestPort = 5030
$ServerProcess = Start-Job -ScriptBlock {
    param($Port)
    $HttpListener = New-Object System.Net.HttpListener
    $HttpListener.Prefixes.Add("http://localhost:$Port/")
    $HttpListener.Start()
    
    Write-Host "HTTP server started on http://localhost:$Port/"
    
    # Create a simple HTML test page
    $HtmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>Test Page for Screenshot Tool</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f0f0f0;
        }
        .container {
            max-width: 800px;
            margin: 0 auto;
            background-color: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
        }
        .demo-element {
            margin: 20px 0;
            padding: 15px;
            background-color: #e1f5fe;
            border-left: 5px solid #03a9f4;
            border-radius: 4px;
        }
        .timestamp {
            color: #666;
            font-size: 14px;
            margin-top: 30px;
            text-align: right;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Test Page for Screenshot Tool</h1>
        <p>This is a test page to demonstrate the screenshot functionality of the MCP server.</p>
        
        <div class="demo-element">
            <h2>Screenshot Demo</h2>
            <p>This page is served by a simple PowerShell HTTP server. The screenshot tool should be able to capture this entire page.</p>
        </div>
        
        <div class="timestamp">Generated: <span id="timestamp"></span></div>
    </div>
    
    <script>
        // Add current time to the page
        document.getElementById('timestamp').textContent = new Date().toLocaleString();
    </script>
</body>
</html>
"@
    
    try {
        while ($HttpListener.IsListening) {
            $Context = $HttpListener.GetContext()
            $Response = $Context.Response
            
            $Buffer = [System.Text.Encoding]::UTF8.GetBytes($HtmlContent)
            $Response.ContentLength64 = $Buffer.Length
            $Response.OutputStream.Write($Buffer, 0, $Buffer.Length)
            $Response.OutputStream.Close()
            Write-Host "Request served at $(Get-Date)"
        }
    }
    finally {
        if ($HttpListener) {
            $HttpListener.Close()
            Write-Host "HTTP server stopped"
        }
    }
} -ArgumentList $TestPort

# Wait a bit for server to start
Start-Sleep -Seconds 2
Write-Host "Web server started on http://localhost:$TestPort/"
Write-Host "Checking if web server is responding..."
try {
    $WebRequest = Invoke-WebRequest -Uri "http://localhost:$TestPort/" -UseBasicParsing -TimeoutSec 5
    Write-Host "Web server is responding with status code: $($WebRequest.StatusCode)"
} catch {
    Write-Host "Error connecting to web server: $_"
    Write-Host "Continuing with test anyway..."
}

# Now start the MCP server with the test to take a screenshot
try {
    $RootPath = Get-Location
    Write-Host "Running test from $RootPath"
    
    # Create test directory for screenshots if it doesn't exist
    $ScreenshotDir = Join-Path $RootPath "feedback_data/screenshots"
    if (-not (Test-Path $ScreenshotDir)) {
        New-Item -ItemType Directory -Path $ScreenshotDir -Force | Out-Null
        Write-Host "Created screenshot directory at $ScreenshotDir"
    }
    
    Write-Host "Starting the MCP server to test screenshot functionality..."
    
    # Define MCP input and output
    $McpInput = @{
        "id" = "test-screenshot";
        "method" = "execute";
        "params" = @{
            "tool" = "take_screenshot";
            "input" = @{
                "url" = "http://localhost:$TestPort/";
                "fullPage" = $true;
                "waitTime" = 2000
            }
        }
    }
    
    $McpInputJson = ConvertTo-Json -InputObject $McpInput -Depth 10
    Write-Host "MCP Request: $McpInputJson"
    Write-Host "Sending request to take screenshot of http://localhost:$TestPort/"
    
    # Check if index.js exists
    $IndexJsPath = Join-Path $RootPath "dist/index.js"
    if (-not (Test-Path $IndexJsPath)) {
        Write-Host "Error: $IndexJsPath does not exist. Make sure you have built the TypeScript code with 'npm run build'"
        exit 1
    }
    
    # Run the MCP server and provide the input
    # Use Out-File to capture any stderr output separately, then read the file
    $TempErrorFile = [System.IO.Path]::GetTempFileName()
    Write-Host "Capturing stderr to: $TempErrorFile"
    
    # Use Start-Process to better handle input/output
    $TempInputFile = [System.IO.Path]::GetTempFileName()
    $TempOutputFile = [System.IO.Path]::GetTempFileName()
    
    $McpInputJson | Out-File -FilePath $TempInputFile -Encoding utf8
    
    Write-Host "Running node $IndexJsPath with input from $TempInputFile"
    $Process = Start-Process -FilePath "node" -ArgumentList $IndexJsPath -NoNewWindow -PassThru `
        -RedirectStandardInput $TempInputFile -RedirectStandardOutput $TempOutputFile -RedirectStandardError $TempErrorFile
    
    # Wait up to 30 seconds for process to complete
    $Process.WaitForExit(30000)
    if (-not $Process.HasExited) {
        Write-Host "Process did not exit after 30 seconds, killing it."
        $Process.Kill()
    }
    
    # Read the output
    $McpOutput = Get-Content -Path $TempOutputFile -Raw
    $McpErrors = Get-Content -Path $TempErrorFile -Raw
    
    # Display stderr output
    if (-not [string]::IsNullOrEmpty($McpErrors)) {
        Write-Host "Process stderr output:"
        Write-Host $McpErrors -ForegroundColor Yellow
    }
    
    # Clean up temp files
    Remove-Item -Path $TempInputFile -Force
    Remove-Item -Path $TempOutputFile -Force
    Remove-Item -Path $TempErrorFile -Force
    
    # Parse and display the response
    Write-Host "MCP server response received"
    
    if ([string]::IsNullOrEmpty($McpOutput)) {
        Write-Host "Error: No response received from MCP server" -ForegroundColor Red
    } else {
        Write-Host "Raw MCP output:" -ForegroundColor Cyan
        Write-Host $McpOutput
        
        try {
            $Response = $McpOutput | ConvertFrom-Json
            
            if ($Response.result) {
                Write-Host "Screenshot taken successfully:" -ForegroundColor Green
                $Response.result.content | ForEach-Object {
                    if ($_.type -eq "text") {
                        Write-Host $_.text
                    } elseif ($_.type -eq "image") {
                        $ImageCount = if ($_.data) { 1 } else { 0 }
                        Write-Host "Screenshot image received (data length: $($_.data.Length))"
                        
                        # Optionally, save the image to a file to verify it worked
                        if ($_.data) {
                            $OutputImagePath = Join-Path $RootPath "feedback_data/test_output.png"
                            [System.Convert]::FromBase64String($_.data) | Set-Content -Path $OutputImagePath -Encoding Byte
                            Write-Host "Saved screenshot to $OutputImagePath for verification"
                        }
                    }
                }
            } elseif ($Response.error) {
                Write-Host "Error occurred: $($Response.error.message)" -ForegroundColor Red
            }
        } catch {
            Write-Host "Error parsing JSON response: $_" -ForegroundColor Red
            Write-Host "Response was not valid JSON"
        }
    }
    
    # Check for screenshots in the directory
    $ScreenshotFiles = Get-ChildItem -Path $ScreenshotDir -Filter "*.png"
    if ($ScreenshotFiles.Count -gt 0) {
        Write-Host "Found $($ScreenshotFiles.Count) screenshot files in $ScreenshotDir:" -ForegroundColor Green
        $ScreenshotFiles | ForEach-Object {
            Write-Host "  - $($_.FullName) ($('{0:N2}' -f ($_.Length / 1KB)) KB)"
        }
    } else {
        Write-Host "No screenshot files found in $ScreenshotDir" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Error running test: $_" -ForegroundColor Red
    Get-Job | Where-Object { $_.State -eq 'Running' } | Stop-Job
    Get-Job | Remove-Job -Force
    exit 1
}
finally {
    # Stop the HTTP server
    Stop-Job -Job $ServerProcess -ErrorAction SilentlyContinue
    Remove-Job -Job $ServerProcess -Force -ErrorAction SilentlyContinue
    Write-Host "Test complete. HTTP server stopped."
} 