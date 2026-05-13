using PaperClaw.Models;

namespace PaperClaw.Abstractions;

public interface IPdfTextExtractor
{
    ExtractionResult TryExtract(FileInfo file);
}
