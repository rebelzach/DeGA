using Wolder.Core.Memory;

namespace Wolder.Core.Files;

public record FileMemoryItem(string RelativePath, string Content) : IMemoryItem
{
    public string Identifier => RelativePath;
}