using Wolder.Core.Files;

namespace Wolder.CSharp.OpenAI.Actions;

public record GenerateTypeParameters(
    DotNetProjectReference Project, string RelativeNamespace, string TypeName, string BehaviorPrompt, IEnumerable<FileMemoryItem>? ContextMemoryItems = null)
{
    public IEnumerable<FileMemoryItem> ContextMemoryItems { get; init; } = 
        ContextMemoryItems ?? Enumerable.Empty<FileMemoryItem>();
}