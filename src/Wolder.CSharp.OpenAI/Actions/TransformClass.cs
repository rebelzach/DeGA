using System.Text;
using System.Text.RegularExpressions;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.CSharp.Compilation;
using Microsoft.Extensions.Logging;
using Wolder.Core.Memory;
using Wolder.Core.Workspace;

namespace Wolder.CSharp.OpenAI.Actions;

public record TransformClassParameters(
    DotNetProjectReference project,
    string filePath,
    string behaviorPrompt,
    IEnumerable<IMemoryItem>? ContextMemoryItems = null)
{
    public IEnumerable<IMemoryItem> ContextMemoryItems { get; init; } = 
        ContextMemoryItems ?? Enumerable.Empty<IMemoryItem>();
}

public class TransformClass(
    IAIAssistant assistant,
    ILogger<TransformClass> logger,
    ISourceFiles sourceFiles,
    CSharpGenerator csharpGenerator,
    TransformClassParameters parameters) 
    : IAction<TransformClassParameters, FileMemoryItem>
{
    public async Task<FileMemoryItem> InvokeAsync()
    {
        var (projectRef, filePath, behaviorPrompt, memoryItems) = parameters;
        filePath = Path.Combine(projectRef.RelativeRoot, filePath);
        // Assert file exists
        if (!sourceFiles.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }
        var context = "";
        if (parameters.ContextMemoryItems.Any())
        {
            context = "\nThese items may provide helpful context:\n" + 
                      string.Join("\n", parameters.ContextMemoryItems
                          .Select(i => i.ToPromptText()));
        }
        var content = await sourceFiles.ReadFileAsync(filePath);
        var response = await assistant.CompletePromptAsync($"""
            {GenerateType.CSharpPrompt}
            {context}
            
            Using the code from this file:
            BEGIN: {filePath}
            ```
            {content}
            ```
            END: {filePath}

            Update the code with the following behavior, output the entire modified file:
            {behaviorPrompt}
            """);

        logger.LogInformation(response);

        var sanitized = Sanitize(response);

        logger.LogInformation(sanitized);

        await sourceFiles.WriteFileAsync(filePath, sanitized);

        var result =  new FileMemoryItem(filePath, sanitized);

        var resultOrFixed = await csharpGenerator.AssistedCompileAsync(
            new(projectRef, result, memoryItems));

        return resultOrFixed;
    }
    
    private static string Sanitize(string input)
    {
        var sanitized = ParseUtilities.ExtractCodeBlocks(input);
        return sanitized;
        // string pattern = @"^\s*```\s*csharp|^\s*```|^\s*```\s*html|^\s*begin:|^\s*end:";
        // string result = Regex.Replace(input, pattern, "",
        //     RegexOptions.Multiline | RegexOptions.IgnoreCase);
        //
        // return result;
    }
}