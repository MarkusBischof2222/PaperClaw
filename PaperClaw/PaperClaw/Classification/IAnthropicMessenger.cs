namespace PaperClaw.Classification;

public interface IAnthropicMessenger
{
    Task<string> ClassifyDocumentAsync(string textContent);
    Task<(string Json, string ExtractedText)> ClassifyImageAsync(byte[] imageData, string mediaType);
}
