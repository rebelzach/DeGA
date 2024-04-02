using Wolder.Core.Memory;

namespace Wolder.Actions.Http;

public record RemoteFileMemoryItem(string Url, string Content)
    : IMemoryItem
{
    public string Identifier => Url;
}