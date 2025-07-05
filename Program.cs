using System.Text.Json;
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
            var toolCalls = new Dictionary<string, ToolCallInfo>();

            while (true)
            {
                Console.Write("> ");
                var prompt = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(prompt))
                    break;

                await foreach (var update in chatClient.GetStreamingResponseAsync([new(ChatRole.User, prompt)], options))
                {
                    await MonitorToolCallsAsync(update, toolCalls);

                    Console.Write(update.ToString());
                    updates.Add(update);
                }

                Console.WriteLine();
            }
        }

        static async Task MonitorToolCallsAsync(ChatResponseUpdate update, Dictionary<string, ToolCallInfo> activeToolCalls)
        {
            // Check for tool calls in the update contents
            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent functionCall)
                    {
                        var callId = functionCall.CallId ?? "unknown";

                        if (!activeToolCalls.ContainsKey(callId))
                        {
                            activeToolCalls[callId] = new ToolCallInfo
                            {
                                CallId = callId,
                                FunctionName = functionCall.Name ?? "unknown",
                                StartTime = DateTime.UtcNow
                            };

                            await Console.Out.WriteLineAsync($"[TOOLCALL_START] {functionCall.Name}");
                        }

                        // Accumulate arguments if they're being streamed
                        if (functionCall.Arguments != null)
                        {
                            var argsString = functionCall.Arguments is IDictionary<string, object?> dict
                                ? JsonSerializer.Serialize(dict)
                                : functionCall.Arguments.ToString();

                            if (!String.IsNullOrEmpty(argsString))
                            {
                                activeToolCalls[callId].Arguments += argsString;
                            }
                        }
                    }
                    else if (content is FunctionResultContent functionResult)
                    {
                        var callId = functionResult.CallId ?? "unknown";

                        if (activeToolCalls.TryGetValue(callId, out var toolCall))
                        {
                            toolCall.EndTime = DateTime.UtcNow;
                            toolCall.Result = functionResult.Result?.ToString() ?? "null";

                            var duration = toolCall.EndTime - toolCall.StartTime;

                            await Console.Out.WriteLineAsync($"[TOOLCALL_END] {toolCall.FunctionName} ({duration.TotalMilliseconds:F0}ms)");

                            // Optionally show arguments and result
                            if (!String.IsNullOrWhiteSpace(toolCall.Arguments))
                            {
                                await Console.Out.WriteLineAsync($"  [ARGS] {TruncateJson(toolCall.Arguments)}");
                            }

                            if (!String.IsNullOrWhiteSpace(toolCall.Result))
                            {
                                await Console.Out.WriteLineAsync($"  [RESULT] {TruncateJson(toolCall.Result)}");
                            }

                            activeToolCalls.Remove(callId);
                        }
                    }
                }
            }
        }

        static string TruncateJson(string json, int maxLength = 100)
        {
            if (String.IsNullOrEmpty(json) || json.Length <= maxLength)
                return json;

            try
            {
                // Try to parse and minify JSON first
                var parsed = JsonDocument.Parse(json);
                var minified = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = false });

                if (minified.Length <= maxLength)
                    return minified;

                return minified[..maxLength] + "...";
            }
            catch
            {
                // If not valid JSON, just truncate
                return json.Length > maxLength ? json[..maxLength] + "..." : json;
            }
        }
    }

    internal class ToolCallInfo
    {
        public string CallId { get; set; } = String.Empty;
        public string FunctionName { get; set; } = String.Empty;
        public string Arguments { get; set; } = String.Empty;
        public string Result { get; set; } = String.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
