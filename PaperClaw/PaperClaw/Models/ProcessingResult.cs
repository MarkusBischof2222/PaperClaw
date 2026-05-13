namespace PaperClaw.Models;

public abstract record ProcessingResult
{
    public record Success(string FilePath) : ProcessingResult;
    public record Duplicate(string FilePath) : ProcessingResult;
    public record Skipped(string FilePath, string Reason) : ProcessingResult;
    public record Failed(string FilePath, string Reason, Exception? Exception = null) : ProcessingResult;
}
