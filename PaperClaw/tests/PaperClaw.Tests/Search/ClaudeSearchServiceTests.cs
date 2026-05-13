using Anthropic.SDK.Messaging;
using Moq;
using NUnit.Framework;
using PaperClaw.Search;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace PaperClaw.Tests.Search;

[TestFixture]
public class ClaudeSearchServiceTests
{
    private Mock<ISearchMessenger> _messengerMock = null!;
    private OutboxSearchTools _searchTools = null!;
    private ClaudeSearchService _service = null!;
    private string _outbox = null!;

    [SetUp]
    public void SetUp()
    {
        _outbox = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outbox);
        _messengerMock = new Mock<ISearchMessenger>();
        _searchTools = new OutboxSearchTools(_outbox);
        _service = new ClaudeSearchService(_messengerMock.Object, _searchTools, _outbox);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_outbox))
            Directory.Delete(_outbox, recursive: true);
    }

    private static MessageResponse TextResponse(string text) => new()
    {
        Content = [new TextContent { Text = text }]
    };

    private static MessageResponse ToolResponse(string toolId, string toolName, string inputJson) => new()
    {
        Content = [new ToolUseContent { Id = toolId, Name = toolName, Input = System.Text.Json.Nodes.JsonNode.Parse(inputJson) }]
    };

    [Test]
    public async Task SearchAsync_ReturnsTextWhenNoToolCalls()
    {
        _messengerMock
            .Setup(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .ReturnsAsync(TextResponse("Found 2 invoices."));

        var result = await _service.SearchAsync("show me all invoices");

        Assert.That(result, Is.EqualTo("Found 2 invoices."));
    }

    [Test]
    public async Task SearchAsync_PassesQuestionAsUserMessage()
    {
        _messengerMock
            .Setup(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .ReturnsAsync(TextResponse("ok"));

        await _service.SearchAsync("find contracts from 2024");

        _messengerMock.Verify(m => m.SendAsync(
            It.Is<MessageParameters>(p =>
                p.Messages.Any(msg => msg.Role == RoleType.User
                    && msg.Content.OfType<TextContent>().Any(c => c.Text == "find contracts from 2024"))),
            It.IsAny<IList<CommonTool>>()), Times.Once);
    }

    [Test]
    public async Task SearchAsync_SystemMessageContainsOutboxPath()
    {
        _messengerMock
            .Setup(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .ReturnsAsync(TextResponse("ok"));

        await _service.SearchAsync("test");

        _messengerMock.Verify(m => m.SendAsync(
            It.Is<MessageParameters>(p => p.SystemMessage != null && p.SystemMessage.Contains(_outbox)),
            It.IsAny<IList<CommonTool>>()), Times.Once);
    }

    [Test]
    public async Task SearchAsync_ReturnsNoResponseWhenContentIsEmpty()
    {
        _messengerMock
            .Setup(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .ReturnsAsync(new MessageResponse { Content = [] });

        var result = await _service.SearchAsync("test");

        Assert.That(result, Is.EqualTo("(no response)"));
    }

    [Test]
    public async Task SearchAsync_ExecutesToolAndContinuesLoop()
    {
        // First response: tool call to SearchByType
        // Second response: final text answer
        _messengerMock
            .SetupSequence(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .ReturnsAsync(ToolResponse("t1", "SearchByType", "{\"type\":\"Invoice\"}"))
            .ReturnsAsync(TextResponse("No invoices found."));

        var result = await _service.SearchAsync("show me all invoices");

        Assert.That(result, Is.EqualTo("No invoices found."));
        _messengerMock.Verify(m => m.SendAsync(
            It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()), Times.Exactly(2));
    }

    [Test]
    public async Task SearchAsync_FeedsToolResultBackInSecondCall()
    {
        var callCount = 0;
        var secondCallHadToolResult = false;

        _messengerMock
            .Setup(m => m.SendAsync(It.IsAny<MessageParameters>(), It.IsAny<IList<CommonTool>>()))
            .Returns((MessageParameters p, IList<CommonTool> _) =>
            {
                callCount++;
                if (callCount == 2)
                {
                    secondCallHadToolResult = p.Messages
                        .SelectMany(msg => msg.Content.OfType<ToolResultContent>())
                        .Any(r => r.ToolUseId == "t1");
                    return Task.FromResult(TextResponse("Done."));
                }
                return Task.FromResult(ToolResponse("t1", "SearchByType", "{\"type\":\"Invoice\"}"));
            });

        await _service.SearchAsync("find invoices");

        Assert.That(callCount, Is.EqualTo(2));
        Assert.That(secondCallHadToolResult, Is.True);
    }
}
