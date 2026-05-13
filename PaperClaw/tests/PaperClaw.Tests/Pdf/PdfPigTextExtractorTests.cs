using NUnit.Framework;
using PaperClaw.Models;
using PaperClaw.Pdf;

namespace PaperClaw.Tests.Pdf;

[TestFixture]
public class PdfPigTextExtractorTests
{
    private PdfPigTextExtractor _extractor = null!;
    private static string TestDataDir => Path.Combine(AppContext.BaseDirectory, "TestData");

    [SetUp]
    public void SetUp() => _extractor = new PdfPigTextExtractor();

    [Test]
    public void ExtractsTextFromFinanzamtBescheid()
    {
        var file = new FileInfo(Path.Combine(TestDataDir, "finanzamt-bescheid.pdf"));
        Assume.That(file.Exists, "finanzamt-bescheid.pdf not found in TestData");

        var result = _extractor.TryExtract(file);

        Assert.That(result, Is.InstanceOf<ExtractionResult.Success>());
        var success = (ExtractionResult.Success)result;
        Assert.That(success.Text, Is.Not.Empty);
    }

    [Test]
    public void ExtractsTextFromStadtwerkeStromrechnung()
    {
        var file = new FileInfo(Path.Combine(TestDataDir, "stadtwerke-stromrechnung.pdf"));
        Assume.That(file.Exists, "stadtwerke-stromrechnung.pdf not found in TestData");

        var result = _extractor.TryExtract(file);

        Assert.That(result, Is.InstanceOf<ExtractionResult.Success>());
        var success = (ExtractionResult.Success)result;
        Assert.That(success.Text, Is.Not.Empty);
    }

    [Test]
    public void ReturnsFailedForCorruptFile()
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllBytes(tempPath, [0x00, 0x01, 0x02, 0x03, 0xFF]);
        var pdfPath = Path.ChangeExtension(tempPath, ".pdf");
        File.Move(tempPath, pdfPath);

        try
        {
            var result = _extractor.TryExtract(new FileInfo(pdfPath));
            Assert.That(result, Is.InstanceOf<ExtractionResult.Failed>());
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }

    [Test]
    public void SkipsPasswordProtectedPdf()
    {
        var encryptedPath = Path.Combine(TestDataDir, "encrypted.pdf");
        Assume.That(File.Exists(encryptedPath), "encrypted.pdf not present in TestData, skipping test");

        var result = _extractor.TryExtract(new FileInfo(encryptedPath));

        Assert.That(result, Is.InstanceOf<ExtractionResult.Skipped>());
    }
}
