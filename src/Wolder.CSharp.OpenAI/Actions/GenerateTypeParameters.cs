using Wolder.Core.Files;
using Wolder.Core.Memory;

namespace Wolder.CSharp.OpenAI.Actions;

public record GenerateTypeParameters(
    DotNetProjectReference Project, string RelativeNamespace, string TypeName, string BehaviorPrompt, IEnumerable<IMemoryItem>? ContextMemoryItems = null)
{
    public IEnumerable<IMemoryItem> ContextMemoryItems { get; init; } = 
        ContextMemoryItems ?? Enumerable.Empty<IMemoryItem>();
}