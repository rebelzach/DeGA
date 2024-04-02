namespace Wolder.CSharp.OpenAI.Constants;

public static class Prompts
{
    public const string CSharpPrompt =
        "You are a helpful assistant that writes C# code to complete any task specified by me. " +
        "Your output will be directly written to a file where it will be compiled as part of a " +
        "larger C# project. Nullable references are enabled.";
}