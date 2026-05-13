using System.Text.Json;
using PaperClaw.Abstractions;
using PaperClaw.Models;

namespace PaperClaw.Classification;

public class ClaudeDocumentClassifier : IDocumentClassifier
{
    private readonly IAnthropicMessenger _messenger;

    public ClaudeDocumentClassifier(IAnthropicMessenger messenger)
    {
        _messenger = messenger;
    }

    public async Task<(DocumentType Type, DocumentMetadata Metadata)> ClassifyAsync(string textLayer)
    {
        var json = await _messenger.ClassifyDocumentAsync(textLayer);
        return Parse(json);
    }

    public async Task<(DocumentType Type, DocumentMetadata Metadata, string ExtractedText)> ClassifyImageAsync(FileInfo file)
    {
        var mediaType = file.Extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png"
        };
        var imageData = await File.ReadAllBytesAsync(file.FullName);
        var (json, extractedText) = await _messenger.ClassifyImageAsync(imageData, mediaType);
        var (type, metadata) = Parse(json);
        return (type, metadata, extractedText);
    }

    internal static (DocumentType, DocumentMetadata) Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var typeStr = GetString(root, "type");
        var type = Enum.TryParse<DocumentType>(typeStr, ignoreCase: true, out var t) ? t : DocumentType.Other;

        var metadata = new DocumentMetadata(
            Title: GetString(root, "title"),
            Date: GetString(root, "date"),
            Sender: GetString(root, "sender"),
            Recipient: GetString(root, "recipient"),
            Summary: GetString(root, "summary"),
            KeyReferences: GetString(root, "key_references"));

        return (type, metadata);
    }

    private static string GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop))
            return "";
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : prop.ToString();
    }
}
