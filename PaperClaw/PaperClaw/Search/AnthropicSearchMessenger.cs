using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace PaperClaw.Search;

internal class AnthropicSearchMessenger : ISearchMessenger
{
    private readonly AnthropicClient _client;

    public AnthropicSearchMessenger(string apiKey)
    {
        _client = new AnthropicClient(apiKey);
    }

    public Task<MessageResponse> SendAsync(MessageParameters parameters, IList<CommonTool> tools)
        => _client.Messages.GetClaudeMessageAsync(parameters, tools);
}
