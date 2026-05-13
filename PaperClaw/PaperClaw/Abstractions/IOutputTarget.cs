using PaperClaw.Models;

namespace PaperClaw.Abstractions;

public interface IOutputTarget
{
    Task<SaveResult> SaveAsync(FileInfo sourceFile, string textLayer, DocumentMetadata metadata, DocumentType type);
}
