using Anthropic.SDK.Messaging;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace PaperClaw.Search;

internal interface ISearchMessenger
{
    Task<MessageResponse> SendAsync(MessageParameters parameters, IList<CommonTool> tools);
}
