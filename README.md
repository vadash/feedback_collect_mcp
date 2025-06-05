# User Feedback Collection MCP Server

This project implements a Model Context Protocol (MCP) server that enables AI assistants to collect user feedback through a WPF GUI application. When the `mcp_claudeflow_collect_feedback` tool is used, it launches a graphical interface where users can provide text feedback and optionally attach an image.

> **Note:** This project is designed for Windows only and does not support macOS or other operating systems. It was created as a personal project to enable more efficient collaboration with AI assistants, reducing unused requests and improving workflow continuity.


AI Rules:
```
# Continuous Feedback Protocol

**Rule**: ALWAYS use `collect_feedback` (via claude-flow MCP) and AWAIT EXPLICIT USER INPUT before and after any implementation. Your actions are 100% user-feedback driven.

**Cycle (Repeat for every task/change):**

1.  **PLAN**: State intent -> `collect_feedback` (Title: "Confirm Plan: [Task]") -> **HALT for approval.**
2.  **IMPLEMENT**: (Only after approval) -> Show results -> `collect_feedback` (Title: "Review: [Task]") -> **HALT for feedback.**
3.  **ITERATE**: Implement feedback precisely (restarts cycle). Completion ONLY on explicit user "Approved."

*(Tool provides context.)*
```


## Requirements

- Node.js 16.0.0 or higher
- .NET 9.0 SDK
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
    "claude_flow": {
      "command": "node",
      "args": ["C:/path/to/your/project/dist/index.js"]
    }
  }
}
```

Replace `C:/path/to/your/project` with the absolute path to your project directory.

## MCP Tools

This server provides the following tools:

### 1. collect_feedback
Displays a WPF application to collect user feedback.
- **Purpose**: Gathers text and image feedback from the user.
- **Key Parameters**: `title` (optional window title), `prompt` (optional user prompt).
- **Usage**: Refer to the "Feedback Collection Guidelines" section for detailed usage and best practices.
- **Features**: Supports Markdown in prompts, image attachments, countdown timer, and auto-close with a default message on timeout.

## Development

### Building
```bash
npm install
npm run build
```