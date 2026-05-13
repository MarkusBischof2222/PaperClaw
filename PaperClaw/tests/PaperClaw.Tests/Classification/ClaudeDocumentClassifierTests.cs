using Moq;
using NUnit.Framework;
using PaperClaw.Classification;
using PaperClaw.Models;

namespace PaperClaw.Tests.Classification;

[TestFixture]
public class ClaudeDocumentClassifierTests
{
    private Mock<IAnthropicMessenger> _messengerMock = null!;
    private ClaudeDocumentClassifier _classifier = null!;

    [SetUp]
    public void SetUp()
    {
        _messengerMock = new Mock<IAnthropicMessenger>();
        _classifier = new ClaudeDocumentClassifier(_messengerMock.Object);
    }

    [TestCase("Invoice")]
    [TestCase("Contract")]
    [TestCase("Receipt")]
    [TestCase("Letter")]
    [TestCase("Report")]
    [TestCase("Other")]
    public async Task ParsesAllDocumentTypes(string typeName)
    {
        var json = $$"""
            {
              "type": "{{typeName}}",
              "title": "Test",
              "date": "2024-01-01",
              "sender": "A",
              "recipient": "B",
              "summary": "Summary",
              "key_references": "Ref"
            }
            """;
        _messengerMock.Setup(m => m.ClassifyDocumentAsync(It.IsAny<string>())).ReturnsAsync(json);
        var expectedType = Enum.Parse<DocumentType>(typeName);

        var (type, _) = await _classifier.ClassifyAsync("some text");

        Assert.That(type, Is.EqualTo(expectedType));
    }

    [Test]
    public async Task ParsesMetadataFields()
    {
        var json = """
            {
              "type": "Invoice",
              "title": "Rechnung 2024",
              "date": "2024-02-15",
              "sender": "Stadtwerke GmbH",
              "recipient": "Max Mustermann",
              "summary": "Strom-Jahresrechnung",
              "key_references": "€245.80, Nr. 2024-0042"
            }
            """;
        _messengerMock.Setup(m => m.ClassifyDocumentAsync(It.IsAny<string>())).ReturnsAsync(json);

        var (_, metadata) = await _classifier.ClassifyAsync("text");

        Assert.That(metadata.Title, Is.EqualTo("Rechnung 2024"));
        Assert.That(metadata.Date, Is.EqualTo("2024-02-15"));
        Assert.That(metadata.Sender, Is.EqualTo("Stadtwerke GmbH"));
        Assert.That(metadata.Recipient, Is.EqualTo("Max Mustermann"));
        Assert.That(metadata.Summary, Is.EqualTo("Strom-Jahresrechnung"));
        Assert.That(metadata.KeyReferences, Is.EqualTo("€245.80, Nr. 2024-0042"));
    }

    [Test]
    public async Task FallsBackToOtherForUnknownType()
    {
        var json = """{"type":"Unknown","title":"","date":"","sender":"","recipient":"","summary":"","key_references":""}""";
        _messengerMock.Setup(m => m.ClassifyDocumentAsync(It.IsAny<string>())).ReturnsAsync(json);

        var (type, _) = await _classifier.ClassifyAsync("text");

        Assert.That(type, Is.EqualTo(DocumentType.Other));
    }

    [Test]
    public async Task PassesTextLayerToMessenger()
    {
        const string inputText = "this is the document text layer";
        var json = """{"type":"Other","title":"","date":"","sender":"","recipient":"","summary":"","key_references":""}""";
        _messengerMock.Setup(m => m.ClassifyDocumentAsync(inputText)).ReturnsAsync(json);

        await _classifier.ClassifyAsync(inputText);

        _messengerMock.Verify(m => m.ClassifyDocumentAsync(inputText), Times.Once);
    }

    [Test]
    public void ParseHandlesMissingOptionalFields()
    {
        var json = """{"type":"Report"}""";

        var (type, metadata) = ClaudeDocumentClassifier.Parse(json);

        Assert.That(type, Is.EqualTo(DocumentType.Report));
        Assert.That(metadata.Title, Is.EqualTo(""));
        Assert.That(metadata.Sender, Is.EqualTo(""));
    }
}
