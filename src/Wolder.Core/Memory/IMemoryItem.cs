namespace Wolder.Core.Memory;

public interface IMemoryItem
{
    string Identifier { get; }
    string Content { get; }
    
    // TODO: More formal serialization of memory items
    public string ToPromptText()
    {
        return $"""
                BEGIN: {Identifier}
                ```
                {Content}
                ```
                END: {Identifier}
                """;
    }
}