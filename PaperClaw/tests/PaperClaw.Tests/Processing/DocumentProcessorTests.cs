using Moq;
using NUnit.Framework;
using PaperClaw.Abstractions;
using PaperClaw.Models;
using PaperClaw.Processing;

namespace PaperClaw.Tests.Processing;

[TestFixture]
public class DocumentProcessorTests
{
    private Mock<IInputSource> _inputSource = null!;
    private Mock<IPdfTextExtractor> _extractor = null!;
    private Mock<IDocumentClassifier> _classifier = null!;
    private Mock<IOutputTarget> _outputTarget = null!;
    private DocumentProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _inputSource = new Mock<IInputSource>();
        _extractor = new Mock<IPdfTextExtractor>();
        _classifier = new Mock<IDocumentClassifier>();
        _outputTarget = new Mock<IOutputTarget>();
        _processor = new DocumentProcessor(
            _inputSource.Object,
            _extractor.Object,
            _classifier.Object,
            _outputTarget.Object);
    }

    [Test]
    public async Task ReturnsEmptyForEmptyInbox()
    {
        _inputSource.Setup(s => s.GetPendingFiles()).Returns([]);

        var results = await _processor.ProcessAllAsync();

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SkipsNonPdfFiles()
    {
        var txtFile = new FileInfo(Path.Combine(Path.GetTempPath(), "document.txt"));
        _inputSource.Setup(s => s.GetPendingFiles()).Returns([txtFile]);

        var results = await _processor.ProcessAllAsync();

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Skipped>());
        _extractor.Verify(e => e.TryExtract(It.IsAny<FileInfo>()), Times.Never);
    }

    [Test]
    public async Task SkipsPasswordProtectedPdf()
    {
        var pdfFile = new FileInfo(Path.Combine(Path.GetTempPath(), "encrypted.pdf"));
        _inputSource.Setup(s => s.GetPendingFiles()).Returns([pdfFile]);
        _extractor.Setup(e => e.TryExtract(pdfFile))
            .Returns(new ExtractionResult.Skipped("Password-protected PDF"));

        var results = await _processor.ProcessAllAsync();

        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Skipped>());
        _classifier.Verify(c => c.ClassifyAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ReturnsFailedForCorruptPdf()
    {
        var pdfFile = new FileInfo(Path.Combine(Path.GetTempPath(), "corrupt.pdf"));
        _inputSource.Setup(s => s.GetPendingFiles()).Returns([pdfFile]);
        _extractor.Setup(e => e.TryExtract(pdfFile))
            .Returns(new ExtractionResult.Failed("Could not read PDF"));

        var results = await _processor.ProcessAllAsync();

        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Failed>());
        _classifier.Verify(c => c.ClassifyAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ProcessesValidPdfAndDeletesFromInbox()
    {
        var tempPath = Path.GetTempFileName();
        var pdfPath = Path.ChangeExtension(tempPath, ".pdf");
        if (File.Exists(pdfPath)) File.Delete(pdfPath);
        File.Move(tempPath, pdfPath);
        var pdfFile = new FileInfo(pdfPath);

        _inputSource.Setup(s => s.GetPendingFiles()).Returns([pdfFile]);
        _extractor.Setup(e => e.TryExtract(pdfFile)).Returns(new ExtractionResult.Success("text content"));
        _classifier.Setup(c => c.ClassifyAsync("text content"))
            .ReturnsAsync((DocumentType.Invoice, new DocumentMetadata("T", "D", "S", "R", "Sum", "Ref")));
        _outputTarget.Setup(o => o.SaveAsync(pdfFile, "text content", It.IsAny<DocumentMetadata>(), DocumentType.Invoice))
            .ReturnsAsync(SaveResult.Saved);

        var results = await _processor.ProcessAllAsync();

        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Success>());
        Assert.That(File.Exists(pdfPath), Is.False, "PDF should be deleted from inbox after success");
    }

    [Test]
    public async Task ReturnsDuplicateAndDeletesFileFromInbox()
    {
        var tempPath = Path.GetTempFileName();
        var pdfPath = Path.ChangeExtension(tempPath, ".pdf");
        if (File.Exists(pdfPath)) File.Delete(pdfPath);
        File.Move(tempPath, pdfPath);
        var pdfFile = new FileInfo(pdfPath);

        _inputSource.Setup(s => s.GetPendingFiles()).Returns([pdfFile]);
        _extractor.Setup(e => e.TryExtract(pdfFile)).Returns(new ExtractionResult.Success("text"));
        _classifier.Setup(c => c.ClassifyAsync("text"))
            .ReturnsAsync((DocumentType.Invoice, new DocumentMetadata("T", "D", "S", "R", "Sum", "Ref")));
        _outputTarget.Setup(o => o.SaveAsync(pdfFile, "text", It.IsAny<DocumentMetadata>(), DocumentType.Invoice))
            .ReturnsAsync(SaveResult.Duplicate);

        var results = await _processor.ProcessAllAsync();

        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Duplicate>());
        Assert.That(File.Exists(pdfPath), Is.False, "Duplicate PDF should still be removed from inbox");
    }

    [Test]
    public async Task LeavesFileInInboxWhenClassificationFails()
    {
        var tempPath = Path.GetTempFileName();
        var pdfPath = Path.ChangeExtension(tempPath, ".pdf");
        if (File.Exists(pdfPath)) File.Delete(pdfPath);
        File.Move(tempPath, pdfPath);
        var pdfFile = new FileInfo(pdfPath);

        try
        {
            _inputSource.Setup(s => s.GetPendingFiles()).Returns([pdfFile]);
            _extractor.Setup(e => e.TryExtract(pdfFile)).Returns(new ExtractionResult.Success("text"));
            _classifier.Setup(c => c.ClassifyAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("API error"));

            var results = await _processor.ProcessAllAsync();

            Assert.That(results[0], Is.InstanceOf<ProcessingResult.Failed>());
            Assert.That(File.Exists(pdfPath), Is.True, "PDF should remain in inbox when processing fails");
        }
        finally
        {
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
        }
    }

    [Test]
    public async Task ProcessesMultipleFiles()
    {
        var files = new[]
        {
            new FileInfo(Path.Combine(Path.GetTempPath(), "a.txt")),
            new FileInfo(Path.Combine(Path.GetTempPath(), "b.docx")),
            new FileInfo(Path.Combine(Path.GetTempPath(), "c.pdf"))
        };
        _inputSource.Setup(s => s.GetPendingFiles()).Returns(files);
        _extractor.Setup(e => e.TryExtract(files[2]))
            .Returns(new ExtractionResult.Skipped("Password-protected PDF"));

        var results = await _processor.ProcessAllAsync();

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0], Is.InstanceOf<ProcessingResult.Skipped>()); // .txt
        Assert.That(results[1], Is.InstanceOf<ProcessingResult.Skipped>()); // .docx
        Assert.That(results[2], Is.InstanceOf<ProcessingResult.Skipped>()); // encrypted .pdf
    }
}
