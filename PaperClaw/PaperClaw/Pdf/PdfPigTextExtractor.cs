using PaperClaw.Abstractions;
using PaperClaw.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace PaperClaw.Pdf;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public ExtractionResult TryExtract(FileInfo file)
    {
        try
        {
            using var document = PdfDocument.Open(file.FullName);
            var pages = document.GetPages();
            var text = string.Join("\n", pages.Select(p => p.Text));
            return new ExtractionResult.Success(text);
        }
        catch (PdfDocumentEncryptedException)
        {
            return new ExtractionResult.Skipped("Password-protected PDF");
        }
        catch (Exception ex)
        {
            return new ExtractionResult.Failed("Could not read PDF", ex);
        }
    }
}
