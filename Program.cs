using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace McpTools
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await using var filesystem = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
            {
                Name = "Filesystem MCP Server",
                Command = @"npx",
                Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
                EnvironmentVariables = new Dictionary<string, string?>()
                {
                    ["PATH"] = Path.Combine(
                        Environment.ExpandEnvironmentVariables("%USERPROFILE%"),
                        $@"Downloads\node-v22.16.0-win-x64\;{Environment.GetEnvironmentVariable("PATH")}"
                    ),
                },
            }));

            var client = new OpenAIClient(new("0"), new OpenAIClientOptions { Endpoint = new("http://127.0.0.1:11434/v1/") });

            var chatClient = client
                .GetChatClient("mistral-small3.2:24b-instruct-2506-q4_K_M")
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            var options = new ChatOptions { Tools = [.. await filesystem.ListToolsAsync()] };
            var updates = new List<ChatResponseUpdate>();

            while (true)
            {
                Console.Write("> ");
                var prompt = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(prompt))
                    break;

                var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

                await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options))
                {
                    await Console.Out.WriteAsync(update.ToString());
                    updates.Add(update);
                }

                Console.WriteLine();
            }
        }
    }
}
