using PaperClaw.Abstractions;
using PaperClaw.Models;
using System.Security.Cryptography;
using System.Text;

namespace PaperClaw.Output;

public class FileOutputTarget : IOutputTarget
{
    private readonly string _outboxPath;

    public FileOutputTarget(string outboxPath)
    {
        _outboxPath = outboxPath;
    }

    public async Task<SaveResult> SaveAsync(FileInfo sourceFile, string textLayer, DocumentMetadata metadata, DocumentType type)
    {
        var hash = ComputeHash(textLayer);
        if (await IsDuplicateAsync(hash))
            return SaveResult.Duplicate;

        var modDate = sourceFile.LastWriteTime;
        var id = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{GenerateSuffix(6)}";
        var dir = Path.Combine(
            _outboxPath,
            modDate.Year.ToString(),
            modDate.Month.ToString("D2"),
            type.ToString(),
            id);

        Directory.CreateDirectory(dir);

        File.Copy(sourceFile.FullName, Path.Combine(dir, sourceFile.Name));
        await File.WriteAllTextAsync(Path.Combine(dir, "text.txt"), textLayer, Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(dir, "transcript.md"), BuildTranscript(metadata, type), Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(dir, "log.txt"), BuildLog(sourceFile, textLayer, metadata, type), Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(dir, "hash.txt"), hash, Encoding.UTF8);

        return SaveResult.Saved;
    }

    private async Task<bool> IsDuplicateAsync(string hash)
    {
        if (!Directory.Exists(_outboxPath))
            return false;

        foreach (var hashFile in Directory.EnumerateFiles(_outboxPath, "hash.txt", SearchOption.AllDirectories))
        {
            var existing = (await File.ReadAllTextAsync(hashFile)).Trim();
            if (existing == hash)
                return true;
        }
        return false;
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildLog(FileInfo sourceFile, string textLayer, DocumentMetadata metadata, DocumentType type)
    {
        return $"""
Processed:  {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Source:     {sourceFile.Name}
Type:       {type}
Title:      {metadata.Title}
Date:       {metadata.Date}
Sender:     {metadata.Sender}
Recipient:  {metadata.Recipient}
Summary:    {metadata.Summary}
References: {metadata.KeyReferences}
Text:       {textLayer.Length} characters
""";
    }

    private static string BuildTranscript(DocumentMetadata metadata, DocumentType type)
    {
        return $"""
# {metadata.Title}

**Type:** {type}
**Date:** {metadata.Date}
**Sender:** {metadata.Sender}
**Recipient:** {metadata.Recipient}
**Summary:** {metadata.Summary}
**Key Amounts/References:** {metadata.KeyReferences}
""";
    }

    private static string GenerateSuffix(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Random.Shared.GetItems<char>(chars, length));
    }
}
