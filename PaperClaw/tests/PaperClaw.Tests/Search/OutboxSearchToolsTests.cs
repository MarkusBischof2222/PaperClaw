using NUnit.Framework;
using PaperClaw.Search;

namespace PaperClaw.Tests.Search;

[TestFixture]
public class OutboxSearchToolsTests
{
    private string _outbox = null!;

    [SetUp]
    public void SetUp()
    {
        _outbox = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outbox);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_outbox))
            Directory.Delete(_outbox, recursive: true);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private string CreateDoc(string year, string month, string type, string id,
        string text = "document text", string transcript = "# Transcript")
    {
        var dir = Path.Combine(_outbox, year, month, type, id);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "text.txt"), text);
        File.WriteAllText(Path.Combine(dir, "transcript.md"), transcript);
        return dir;
    }

    // ── SearchByText ──────────────────────────────────────────────────────────

    [Test]
    public async Task SearchByText_ReturnsMatchingDocuments()
    {
        CreateDoc("2024", "01", "Invoice", "id1", text: "Total amount due: 500 EUR");
        CreateDoc("2024", "01", "Contract", "id2", text: "Employment agreement between parties");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByText("Total amount");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Not.Contain("id2"));
    }

    [Test]
    public async Task SearchByText_IsCaseInsensitive()
    {
        CreateDoc("2024", "01", "Invoice", "id1", text: "Invoice for services rendered");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByText("INVOICE");

        Assert.That(result, Does.Contain("id1"));
    }

    [Test]
    public async Task SearchByText_ReturnsNoDocumentsFoundWhenNoMatch()
    {
        CreateDoc("2024", "01", "Invoice", "id1", text: "Some other content");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByText("nonexistent keyword");

        Assert.That(result, Is.EqualTo("No documents found."));
    }

    [Test]
    public async Task SearchByText_ReturnsNoDocumentsFoundOnEmptyOutbox()
    {
        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByText("anything");

        Assert.That(result, Is.EqualTo("No documents found."));
    }

    [Test]
    public async Task SearchByText_IncludesTranscriptInResult()
    {
        CreateDoc("2024", "01", "Invoice", "id1",
            text: "electric bill",
            transcript: "# Electric Invoice\nDate: 2024-01-15");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByText("electric");

        Assert.That(result, Does.Contain("Electric Invoice"));
    }

    [Test]
    public async Task SearchByText_RejectsPathTraversal()
    {
        var tools = new OutboxSearchTools(_outbox);
        // Path traversal cannot match any file under the root, returns no match
        var result = await tools.SearchByText("test");

        Assert.That(result, Is.EqualTo("No documents found."));
        Assert.That(tools.IsUnderRoot("..\\sensitive"), Is.False);
    }

    // ── SearchByType ──────────────────────────────────────────────────────────

    [Test]
    public async Task SearchByType_ReturnsOnlyMatchingType()
    {
        CreateDoc("2024", "01", "Invoice", "id1");
        CreateDoc("2024", "01", "Contract", "id2");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByType("Invoice");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Not.Contain("id2"));
    }

    [Test]
    public async Task SearchByType_ReturnsNoDocumentsFoundWhenTypeAbsent()
    {
        CreateDoc("2024", "01", "Contract", "id1");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByType("Invoice");

        Assert.That(result, Is.EqualTo("No documents found."));
    }

    [Test]
    public async Task SearchByType_ReturnsNoDocumentsFoundOnEmptyOutbox()
    {
        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByType("Invoice");

        Assert.That(result, Is.EqualTo("No documents found."));
    }

    [Test]
    public async Task SearchByType_IncludesTranscriptInResult()
    {
        CreateDoc("2024", "01", "Invoice", "id1", transcript: "# Invoice\nAmount: €100");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByType("Invoice");

        Assert.That(result, Does.Contain("Amount: €100"));
    }

    [Test]
    public async Task SearchByType_SpansMultipleMonths()
    {
        CreateDoc("2024", "01", "Invoice", "id1");
        CreateDoc("2024", "03", "Invoice", "id2");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByType("Invoice");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Contain("id2"));
    }

    // ── SearchByDateRange ─────────────────────────────────────────────────────

    [Test]
    public async Task SearchByDateRange_ReturnsDocumentsInRange()
    {
        CreateDoc("2024", "01", "Invoice", "id1");
        CreateDoc("2024", "06", "Invoice", "id2");
        CreateDoc("2024", "12", "Invoice", "id3");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("2024-01", "2024-06");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Contain("id2"));
        Assert.That(result, Does.Not.Contain("id3"));
    }

    [Test]
    public async Task SearchByDateRange_ReturnsNoDocumentsFoundOutsideRange()
    {
        CreateDoc("2023", "12", "Invoice", "id1");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("2024-01", "2024-12");

        Assert.That(result, Is.EqualTo("No documents found."));
    }

    [Test]
    public async Task SearchByDateRange_ReturnsErrorOnInvalidFromFormat()
    {
        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("January 2024", "2024-12");

        Assert.That(result, Does.Contain("Invalid date format"));
    }

    [Test]
    public async Task SearchByDateRange_ReturnsErrorOnInvalidToFormat()
    {
        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("2024-01", "bad");

        Assert.That(result, Does.Contain("Invalid date format"));
    }

    [Test]
    public async Task SearchByDateRange_IncludesBoundaryMonths()
    {
        CreateDoc("2024", "01", "Invoice", "id1");
        CreateDoc("2024", "12", "Invoice", "id2");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("2024-01", "2024-12");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Contain("id2"));
    }

    [Test]
    public async Task SearchByDateRange_SpansYears()
    {
        CreateDoc("2023", "11", "Report", "id1");
        CreateDoc("2024", "02", "Report", "id2");
        CreateDoc("2024", "08", "Report", "id3");

        var tools = new OutboxSearchTools(_outbox);
        var result = await tools.SearchByDateRange("2023-11", "2024-02");

        Assert.That(result, Does.Contain("id1"));
        Assert.That(result, Does.Contain("id2"));
        Assert.That(result, Does.Not.Contain("id3"));
    }

    // ── TryParseYearMonth ─────────────────────────────────────────────────────

    [TestCase("2024-01", true)]
    [TestCase("2024-12", true)]
    [TestCase("2024-00", false)]
    [TestCase("2024-13", false)]
    [TestCase("2024", false)]
    [TestCase("", false)]
    [TestCase("abc-01", false)]
    public void TryParseYearMonth_ValidatesFormat(string input, bool expected)
    {
        var result = OutboxSearchTools.TryParseYearMonth(input, out _);
        Assert.That(result, Is.EqualTo(expected));
    }
}
