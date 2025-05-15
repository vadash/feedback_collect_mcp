# User Feedback Collection MCP Server

This project implements a Model Context Protocol (MCP) server that enables AI assistants to collect user feedback through a WPF GUI application. When the MCP tool is used, it launches a graphical interface where users can provide text feedback and optionally attach an image.

## Components

1. **MCP Server** - A Node.js application that implements the MCP protocol and acts as an interface between the AI assistant and the user feedback collection GUI.

2. **Feedback Collection GUI** - A .NET 8 WPF application that provides a user-friendly interface for collecting feedback and optionally attaching images.

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

## Configuration

To use this MCP server with Claude Desktop or another MCP client, add the following configuration:

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

## How It Works

1. The AI assistant calls the `collect_feedback` tool from the MCP server.
2. The MCP server launches the WPF GUI application.
3. The user enters feedback text and optionally attaches an image.
4. The user clicks "Submit Feedback" to send the feedback back to the MCP server.
5. The MCP server formats the feedback data and returns it to the AI assistant.
6. The assistant can now process and respond to the feedback.

## Tool Parameters

The `collect_feedback` tool accepts the following parameters:

- `title` (optional): Custom title for the feedback window (default: "AI Feedback Collection")
- `prompt` (optional): Custom prompt text displayed to the user (default: "Please provide your feedback or describe your issue:")

## Example Usage

```javascript
// Example of how an AI assistant might use the tool
const feedback = await collectFeedback({
  title: "Report an Issue",
  prompt: "Please describe the problem you're experiencing:"
});

// The AI can then process the feedback
console.log(`User reported: ${feedback.text}`);
if (feedback.hasImage) {
  console.log("User attached an image to illustrate the issue");
}
``` 