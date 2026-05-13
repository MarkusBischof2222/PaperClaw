using PaperClaw.Abstractions;
using PaperClaw.Models;

namespace PaperClaw.Processing;

public class DocumentProcessor
{
    private readonly IInputSource _inputSource;
    private readonly IPdfTextExtractor _extractor;
    private readonly IDocumentClassifier _classifier;
    private readonly IOutputTarget _outputTarget;

    public DocumentProcessor(
        IInputSource inputSource,
        IPdfTextExtractor extractor,
        IDocumentClassifier classifier,
        IOutputTarget outputTarget)
    {
        _inputSource = inputSource;
        _extractor = extractor;
        _classifier = classifier;
        _outputTarget = outputTarget;
    }

    public async Task<IReadOnlyList<ProcessingResult>> ProcessAllAsync()
    {
        var results = new List<ProcessingResult>();
        foreach (var file in _inputSource.GetPendingFiles())
            results.Add(await ProcessFileAsync(file));
        return results;
    }

    private async Task<ProcessingResult> ProcessFileAsync(FileInfo file)
    {
        if (!file.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return new ProcessingResult.Skipped(file.FullName, "Not a PDF file");

        var extraction = _extractor.TryExtract(file);

        if (extraction is ExtractionResult.Skipped skipped)
            return new ProcessingResult.Skipped(file.FullName, skipped.Reason);

        if (extraction is ExtractionResult.Failed failed)
            return new ProcessingResult.Failed(file.FullName, failed.Reason, failed.Exception);

        var text = ((ExtractionResult.Success)extraction).Text;

        try
        {
            var (type, metadata) = await _classifier.ClassifyAsync(text);
            var saveResult = await _outputTarget.SaveAsync(file, text, metadata, type);
            file.Delete();
            return saveResult == SaveResult.Duplicate
                ? new ProcessingResult.Duplicate(file.FullName)
                : new ProcessingResult.Success(file.FullName);
        }
        catch (Exception ex)
        {
            return new ProcessingResult.Failed(file.FullName, "Processing failed", ex);
        }
    }
}
