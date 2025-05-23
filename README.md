# User Feedback Collection MCP Server

This project implements a Model Context Protocol (MCP) server that enables AI assistants to collect user feedback through a WPF GUI application. When the `mcp_claudeflow_collect_feedback` tool is used, it launches a graphical interface where users can provide text feedback and optionally attach an image.

> **Note:** This project is designed for Windows only and does not support macOS or other operating systems. It was created as a personal project to enable more efficient collaboration with AI assistants, reducing unused requests and improving workflow continuity.


Example Rules
```
# Feedback Collection Guidelines

Use the `mcp_claudeflow_collect_feedback` tool to gather user input during development.

## When to Collect Feedback
- Before implementing any changes
- After implementing changes
- When requested by the user

## Best Practices
- Use clear, concise titles for feedback requests
- Ask specific questions related to implementation
- Wait for user input before proceeding to next tasks
- Follow feedback precisely when received
- When no feedback is given, proceed with best judgment
- For additional change requests, start a new feedback cycle
- The tool provides temporal context (current time, timezone, date) 
```


## Requirements

- Node.js 16.0.0 or higher
- .NET 8.0 SDK
- Windows operating system (for WPF application)

## Setup Instructions

### Building the MCP Server

1. Install Node.js dependencies:
   ```
   npm install
   ```

2. Build the TypeScript code:
   ```
   npm run build
   ```

### Building the WPF Feedback Application

1. Navigate to the FeedbackApp directory:
   ```
   cd FeedbackApp
   ```

2. Build the WPF application:
   ```
   dotnet build -c Release
   ```
   (Note: Ensure you are in the `FeedbackApp` directory before running this command.)

## Configuration

To use this MCP server with Cursor or another MCP client, add the following configuration:

```json
{
  "mcpServers": {
    "userFeedback": {
      "command": "node",
      "args": ["C:/path/to/your/project/dist/index.js"]
    }
  }
}
```

Replace `C:/path/to/your/project` with the absolute path to your project directory.

## MCP Tools

This server provides the following tools:

### 1. mcp_claudeflow_collect_feedback
Displays a WPF application to collect user feedback.
- **Purpose**: Gathers text and image feedback from the user.
- **Key Parameters**: `title` (optional window title), `prompt` (optional user prompt).
- **Usage**: Refer to the "Feedback Collection Guidelines" section for detailed usage and best practices.
- **Features**: Supports Markdown in prompts, image attachments, countdown timer, and auto-close with a default message on timeout.

### 2. get_time
Returns the current date and time.
- **Parameters**: `format` (optional: "full", "iso", "date", "time", "unix"), `timezone` (optional).

### 3. take_screenshot
Takes a screenshot of a specified webpage.
- **Parameters**: `url`, `fullPage` (optional), `waitTime` (optional), `actions` (optional array for page interaction).

### 4. get_console_errors
Collects JavaScript console errors from a webpage.
- **Parameters**: `url`, `actions` (optional array for page interaction).

## Development

### Building
```bash
npm install
npm run build
```