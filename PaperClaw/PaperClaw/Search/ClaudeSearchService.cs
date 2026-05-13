using System.Text.Json.Nodes;
using Anthropic.SDK.Messaging;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace PaperClaw.Search;

public class ClaudeSearchService : ISearchService
{
    private readonly ISearchMessenger _messenger;
    private readonly OutboxSearchTools _searchTools;
    private readonly string _outboxRoot;

    private static readonly IList<CommonTool> Tools =
    [
        new Anthropic.SDK.Common.Function("SearchByText",
            "Search documents by keyword in full text content",
            JsonNode.Parse("""{"type":"object","properties":{"keyword":{"type":"string","description":"Keyword or phrase to search for in document text"}},"required":["keyword"]}""")!),
        new Anthropic.SDK.Common.Function("SearchByType",
            "List documents of a specific type: Invoice, Contract, Receipt, Letter, Report, or Other",
            JsonNode.Parse("""{"type":"object","properties":{"type":{"type":"string","description":"Document type: Invoice, Contract, Receipt, Letter, Report, or Other"}},"required":["type"]}""")!),
        new Anthropic.SDK.Common.Function("SearchByDateRange",
            "List documents within a date range (YYYY-MM format)",
            JsonNode.Parse("""{"type":"object","properties":{"fromYearMonth":{"type":"string","description":"Start year-month, e.g. 2024-01"},"toYearMonth":{"type":"string","description":"End year-month, e.g. 2024-12"}},"required":["fromYearMonth","toYearMonth"]}""")!),
    ];

    public ClaudeSearchService(string apiKey, string outboxRoot)
        : this(new AnthropicSearchMessenger(apiKey), new OutboxSearchTools(outboxRoot), outboxRoot) { }

    internal ClaudeSearchService(ISearchMessenger messenger, OutboxSearchTools searchTools, string outboxRoot)
    {
        _messenger = messenger;
        _searchTools = searchTools;
        _outboxRoot = outboxRoot;
    }

    public async Task<string> SearchAsync(string question)
    {
        var systemPrompt = $$"""
            You are a document search assistant for a PDF archive organized as:
            {{_outboxRoot}}/{year}/{month:D2}/{DocumentType}/{id}/
            Each document folder contains transcript.md (metadata) and text.txt (full text).
            Current month: {{DateTime.Now:yyyy-MM}}.
            Use the search tools to find relevant documents, then summarize the results clearly.
            """;

        var messages = new List<Message>
        {
            new Message(RoleType.User, question)
        };

        var parameters = new MessageParameters
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 4096,
            SystemMessage = systemPrompt,
            Messages = messages,
            Stream = false,
            Temperature = 0.0m
        };

        while (true)
        {
            var response = await _messenger.SendAsync(parameters, Tools);
            messages.Add(response.Message);

            var toolUses = response.Content.OfType<ToolUseContent>().ToList();
            if (toolUses.Count == 0)
                return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "(no response)";

            var toolResults = new List<ContentBase>();
            foreach (var toolUse in toolUses)
            {
                var result = await DispatchTool(toolUse);
                toolResults.Add(new ToolResultContent { ToolUseId = toolUse.Id, Content = result });
            }
            messages.Add(new Message { Role = RoleType.User, Content = toolResults });
        }
    }

    private Task<string> DispatchTool(ToolUseContent toolUse)
    {
        var args = toolUse.Input!;
        return toolUse.Name switch
        {
            "SearchByText" => _searchTools.SearchByText(args["keyword"]!.ToString()!),
            "SearchByType" => _searchTools.SearchByType(args["type"]!.ToString()!),
            "SearchByDateRange" => _searchTools.SearchByDateRange(
                args["fromYearMonth"]!.ToString()!,
                args["toYearMonth"]!.ToString()!),
            _ => Task.FromResult($"Unknown tool: {toolUse.Name}")
        };
    }
}
