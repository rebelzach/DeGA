using System.Text;
using System.Text.RegularExpressions;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.CSharp.Compilation;
using Microsoft.Extensions.Logging;
using Wolder.Core.Workspace;

namespace Wolder.CSharp.OpenAI.Actions;

public record TransformClassParameters(
    DotNetProjectReference project, string filePath, string behaviorPrompt);

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
        var (projectRef, filePath, behaviorPrompt) = parameters;
        filePath = Path.Combine(projectRef.RelativeRoot, filePath);
        // Assert file exists
        if (!sourceFiles.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }
        var content = await sourceFiles.ReadFileAsync(filePath);
        var response = await assistant.CompletePromptAsync($"""
            {GenerateType.CSharpPrompt}

            Using the code from this file:
            File: {filePath}
            ```
            {content}
            ```

            Update the code with the following behavior, output the entire modified file:
            {behaviorPrompt}
            
            Begin Output:
            """);

        logger.LogInformation(response);

        var sanitized = Sanitize(response);

        logger.LogInformation(sanitized);

        await sourceFiles.WriteFileAsync(filePath, sanitized);

        var result =  new FileMemoryItem(filePath, sanitized);

        var resultOrFixed = await csharpGenerator.AssistedCompileAsync(
            new(projectRef, result));

        return resultOrFixed;
    }
    
    private static string Sanitize(string input)
    {
        string pattern = @"^\s*```\s*csharp|^\s*```|^\s*```\s*html";
        string result = Regex.Replace(input, pattern, "", RegexOptions.Multiline);

        return result;
    }
}