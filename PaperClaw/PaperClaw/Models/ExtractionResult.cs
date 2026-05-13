namespace PaperClaw.Models;

public abstract record ExtractionResult
{
    public record Success(string Text) : ExtractionResult;
    public record Skipped(string Reason) : ExtractionResult;
    public record Failed(string Reason, Exception? Exception = null) : ExtractionResult;
}
