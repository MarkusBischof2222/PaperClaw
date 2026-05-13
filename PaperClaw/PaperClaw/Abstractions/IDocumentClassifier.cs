using PaperClaw.Models;

namespace PaperClaw.Abstractions;

public interface IDocumentClassifier
{
    Task<(DocumentType Type, DocumentMetadata Metadata)> ClassifyAsync(string textLayer);
    Task<(DocumentType Type, DocumentMetadata Metadata, string ExtractedText)> ClassifyImageAsync(FileInfo file);
}
