using NUnit.Framework;
using PaperClaw.Models;
using PaperClaw.Output;

namespace PaperClaw.Tests.Output;

[TestFixture]
public class FileOutputTargetTests
{
    private string _outbox = null!;
    private string _sourceFile = null!;

    [SetUp]
    public void SetUp()
    {
        _outbox = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outbox);

        _sourceFile = Path.Combine(Path.GetTempPath(), "invoice.pdf");
        File.WriteAllBytes(_sourceFile, [0x25, 0x50, 0x44, 0x46]); // %PDF header bytes
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_outbox, recursive: true);
        if (File.Exists(_sourceFile)) File.Delete(_sourceFile);
    }

    [Test]
    public async Task CreatesStructuredOutputDirectory()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Test Invoice", "2024-01-15", "Sender GmbH", "Recipient AG", "A test invoice", "€100.00");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "extracted text", meta, DocumentType.Invoice);

        var modDate = file.LastWriteTime;
        var yearDir = Path.Combine(_outbox, modDate.Year.ToString());
        Assert.That(Directory.Exists(yearDir), Is.True, "Year directory not created");

        var monthDir = Directory.GetDirectories(yearDir).FirstOrDefault();
        Assert.That(monthDir, Is.Not.Null, "Month directory not created");

        var typeDir = Path.Combine(monthDir!, "Invoice");
        Assert.That(Directory.Exists(typeDir), Is.True, "Type directory not created");
    }

    [Test]
    public async Task WritesAllFourFiles()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("My Contract", "2024-06-01", "Company A", "Company B", "A contract.", "Ref-001");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "contract text layer", meta, DocumentType.Contract);

        var allFiles = Directory.GetFiles(_outbox, "*", SearchOption.AllDirectories);
        Assert.That(allFiles.Any(f => f.EndsWith(".pdf")), Is.True, "PDF copy missing");
        Assert.That(allFiles.Any(f => Path.GetFileName(f) == "text.txt"), Is.True, "text.txt missing");
        Assert.That(allFiles.Any(f => Path.GetFileName(f) == "transcript.md"), Is.True, "transcript.md missing");
        Assert.That(allFiles.Any(f => Path.GetFileName(f) == "log.txt"), Is.True, "log.txt missing");
        Assert.That(allFiles.Any(f => Path.GetFileName(f) == "hash.txt"), Is.True, "hash.txt missing");
    }

    [Test]
    public async Task LogContainsProcessingDetails()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Invoice Title", "2024-05-01", "Sender AG", "Recipient GmbH", "An invoice.", "€500.00, #INV-99");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "invoice text content", meta, DocumentType.Invoice);

        var log = File.ReadAllText(
            Directory.GetFiles(_outbox, "log.txt", SearchOption.AllDirectories).First());

        Assert.That(log, Does.Contain("invoice.pdf"));
        Assert.That(log, Does.Contain("Invoice"));
        Assert.That(log, Does.Contain("Sender AG"));
        Assert.That(log, Does.Contain("Recipient GmbH"));
        Assert.That(log, Does.Contain("€500.00, #INV-99"));
    }

    [Test]
    public async Task TranscriptContainsAllMetadataFields()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Receipt Title", "2024-03-10", "Shop XY", "John Doe", "Purchase receipt.", "€49.99, #REC-42");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "receipt text", meta, DocumentType.Receipt);

        var transcript = File.ReadAllText(
            Directory.GetFiles(_outbox, "transcript.md", SearchOption.AllDirectories).First());

        Assert.That(transcript, Does.Contain("Receipt Title"));
        Assert.That(transcript, Does.Contain("Receipt"));
        Assert.That(transcript, Does.Contain("2024-03-10"));
        Assert.That(transcript, Does.Contain("Shop XY"));
        Assert.That(transcript, Does.Contain("John Doe"));
        Assert.That(transcript, Does.Contain("Purchase receipt."));
        Assert.That(transcript, Does.Contain("€49.99, #REC-42"));
    }

    [Test]
    public async Task ReturnsSavedOnFirstSave()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Doc", "2024-01-01", "A", "B", "Summary.", "Ref");
        var target = new FileOutputTarget(_outbox);

        var result = await target.SaveAsync(file, "unique text content xyz", meta, DocumentType.Other);

        Assert.That(result, Is.EqualTo(SaveResult.Saved));
    }

    [Test]
    public async Task ReturnsDuplicateOnSecondSaveWithSameText()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Doc", "2024-01-01", "A", "B", "Summary.", "Ref");
        var target = new FileOutputTarget(_outbox);
        const string text = "identical document text content";

        await target.SaveAsync(file, text, meta, DocumentType.Other);
        var result = await target.SaveAsync(file, text, meta, DocumentType.Other);

        Assert.That(result, Is.EqualTo(SaveResult.Duplicate));
    }

    [Test]
    public async Task ReturnsSavedForDifferentTextContent()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Doc", "2024-01-01", "A", "B", "Summary.", "Ref");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "first document text", meta, DocumentType.Other);
        var result = await target.SaveAsync(file, "second document text — different", meta, DocumentType.Other);

        Assert.That(result, Is.EqualTo(SaveResult.Saved));
    }

    [Test]
    public async Task TextFileContainsExtractedText()
    {
        var file = new FileInfo(_sourceFile);
        var meta = new DocumentMetadata("Letter", "2024-04-01", "Gov", "Citizen", "Official letter.", "Case #123");
        var target = new FileOutputTarget(_outbox);

        await target.SaveAsync(file, "full letter text layer content", meta, DocumentType.Letter);

        var text = File.ReadAllText(
            Directory.GetFiles(_outbox, "text.txt", SearchOption.AllDirectories).First());

        Assert.That(text, Is.EqualTo("full letter text layer content"));
    }
}
