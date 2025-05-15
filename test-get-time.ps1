#!/usr/bin/env pwsh
# Test script for the get_time tool in the MCP server

Write-Host "Testing get_time tool in MCP server..." -ForegroundColor Cyan
Write-Host ""

# Use Node.js to communicate with the MCP server using stdio
$scriptPath = Join-Path (Get-Location) "test-get-time.js"

# Create a temporary JavaScript file to test the get_time tool
$javascriptContent = @"
import { McpClient } from '@modelcontextprotocol/sdk/client/mcp.js';
import { ChildProcessClientTransport } from '@modelcontextprotocol/sdk/client/childProcess.js';
import path from 'path';
import { fileURLToPath } from 'url';

// Get current directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function main() {
  console.log('Connecting to MCP server...');
  
  // Path to the compiled MCP server
  const serverPath = path.resolve(__dirname, 'dist/index.js');
  
  // Create a transport that connects to the MCP server
  const transport = new ChildProcessClientTransport({
    command: 'node',
    args: [serverPath],
  });
  
  // Create an MCP client and connect to the server
  const client = new McpClient();
  await client.connect(transport);
  
  console.log('Connected to MCP server. Testing get_time tool...');
  
  try {
    // Test 1: Default format (full)
    console.log('\nTest 1: Default format');
    const result1 = await client.callTool('get_time', {});
    console.log(JSON.stringify(result1, null, 2));
    
    // Test 2: ISO format
    console.log('\nTest 2: ISO format');
    const result2 = await client.callTool('get_time', { format: 'iso' });
    console.log(JSON.stringify(result2, null, 2));
    
    // Test 3: Date format
    console.log('\nTest 3: Date format');
    const result3 = await client.callTool('get_time', { format: 'date' });
    console.log(JSON.stringify(result3, null, 2));
    
    // Test 4: Time format
    console.log('\nTest 4: Time format');
    const result4 = await client.callTool('get_time', { format: 'time' });
    console.log(JSON.stringify(result4, null, 2));
    
    // Test 5: Unix format
    console.log('\nTest 5: Unix format');
    const result5 = await client.callTool('get_time', { format: 'unix' });
    console.log(JSON.stringify(result5, null, 2));
    
    // Test 6: With timezone (UTC)
    console.log('\nTest 6: Timezone UTC');
    const result6 = await client.callTool('get_time', { timezone: 'UTC' });
    console.log(JSON.stringify(result6, null, 2));
  } catch (error) {
    console.error('Error testing get_time tool:', error);
  }
  
  console.log('\nDisconnecting from MCP server...');
  await client.disconnect();
  console.log('Test completed.');
}

main().catch(error => {
  console.error('Error:', error);
  process.exit(1);
});
"@

# Write the JavaScript test file
$javascriptContent | Out-File -FilePath $scriptPath -Encoding utf8

# Run the test
Write-Host "Running test script..." -ForegroundColor Yellow
node $scriptPath

# Clean up
Remove-Item $scriptPath -Force
Write-Host ""
Write-Host "Test completed and temporary files cleaned up." -ForegroundColor Green 