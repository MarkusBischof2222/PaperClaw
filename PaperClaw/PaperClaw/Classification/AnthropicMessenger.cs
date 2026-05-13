using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System.Text.RegularExpressions;

namespace PaperClaw.Classification;

public class AnthropicMessenger : IAnthropicMessenger
{
    private readonly AnthropicClient _client;

    public AnthropicMessenger(string apiKey)
    {
        _client = new AnthropicClient(apiKey);
    }

    public async Task<string> ClassifyDocumentAsync(string textContent)
    {
        var prompt = $"""
            Analyze the document text below and respond with ONLY a valid JSON object (no markdown, no explanation, no trailing text).
            Every field value must be a plain string — never an object or array.
            The JSON must contain exactly these string fields:
            - "type": one of "Invoice", "Contract", "Receipt", "Letter", "Report", "Other"
            - "title": document title as a single string
            - "date": document date as a single string (ISO format if possible)
            - "sender": sender name or company as a single string
            - "recipient": recipient name or company as a single string
            - "summary": one to two sentence summary as a single string
            - "key_references": key amounts, invoice numbers, or references as a single string

            Document text:
            {textContent}
            """;

        var parameters = new MessageParameters
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 1024,
            Messages = [new Message(RoleType.User, prompt)],
            Stream = false,
            Temperature = 0.0m
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters);
        var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "{}";
        return ExtractJson(text);
    }

    private static string ExtractJson(string text)
    {
        // Strip markdown code fences if present
        var match = Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```");
        if (match.Success)
            return match.Groups[1].Value;

        // Find raw JSON object
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }
}
