namespace PaperClaw.Classification;

public interface IAnthropicMessenger
{
    Task<string> ClassifyDocumentAsync(string textContent);
}
