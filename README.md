# Using a MCP server with an Open AI compatible endpoint (i.e. Ollama)

A simple C# app to demo using a MCP server with OpenAI compatible endpoint enabling AI assistants to interact with tools.

## Quick Overview

Demonstrates how to:
- Connect to MCP servers (specifically the official MCP server-filesystem)
- Integrate with OpenAI-compatible APIs (configured for local models via Ollama)
- Enable language models to use MCP tools through function calling
- Create an interactive chat interface with streaming responses

## Prerequisites

- .NET 8.0 or later
- Node.js and npm/npx (for running MCP servers)
- Ollama (or another OpenAI-compatible API endpoint)
- A compatible language model (e.g., `Mistral-Small-3.2-24B-Instruct-2506`)

## Dependencies

This project uses the following NuGet packages:
- `Microsoft.Extensions.AI` - For AI abstractions and chat client functionality
- `ModelContextProtocol.Client` - For MCP client implementation
- `OpenAI` - For OpenAI API compatibility

## Configuration

The application is pre-configured to:
1. Run the MCP filesystem server via npx (ajust your Node.js path accordingly)
2. Connect to a local OpenAI-compatible API at `http://127.0.0.1:11434/v1/` (i.e. default Ollama endpoint)
3. Use the `mistral-small3.2:24b-instruct-2506-q4_K_M` model from the Ollama registry

### Customization

To modify the configuration:

**MCP Server**: Change the server type or arguments in the `McpClientFactory.CreateAsync` update the following (you can find reference servers at https://github.com/modelcontextprotocol/servers?tab=readme-ov-file#-reference-servers):
```csharp
Command = @"npx",
Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
```

**API Endpoint**: Update the OpenAI client configuration:
```csharp
Endpoint = new("http://127.0.0.1:11434/v1/")
```

**Model**: Change the model name in `GetChatClient` as follows (you can view Ollama models supporting tools at https://ollama.com/search?c=tools)
```csharp
.GetChatClient("your-model-name")
```

## Usage

1. Clone the repository:
   ```bash
   git clone https://github.com/dranger003/McpTools.git
   cd McpTools
   ```

2. Ensure Ollama is running with your desired model:
   ```bash
   ollama run mistral-small3.2:24b-instruct-2506-q4_K_M
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Interact with the chat interface:
   ```
   > List the files in /tmp
   [AI responds with file listing]
   > Read the contents of example.txt
   [AI reads and displays file contents]
   ```

6. Press `<Enter>` on an empty line to exit the console app.

## How It Works

1. **MCP Connection**: The application spawns an MCP filesystem server as a subprocess, providing access to file system operations.

2. **Tool Registration**: Available MCP tools are automatically discovered and registered with the chat client.

3. **Function Invocation**: When the AI needs to perform file operations, it uses function calling to invoke the appropriate MCP tools.

4. **Streaming Responses**: The chat interface streams responses in real-time for better user experience.

## Platform Notes

The current configuration includes Windows-specific paths for Node.js. If running on Linux or macOS, you may need to adjust the PATH configuration.
