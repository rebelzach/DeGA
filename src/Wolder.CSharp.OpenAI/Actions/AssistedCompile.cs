using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.CSharp.Compilation;
using Wolder.Core.Workspace;
using Wolder.CSharp.OpenAI.Constants;

namespace Wolder.CSharp.OpenAI.Actions;

public record AssistedCompileParameters(
    DotNetProjectReference Project, FileMemoryItem RelevantFiles)
{
    public IEnumerable<FileMemoryItem> ContextMemoryItems { get; init; } = 
        Enumerable.Empty<FileMemoryItem>();
}

public class AssistedCompile(
    IAIAssistant assistant,
    ILogger<AssistedCompile> logger,
    CSharpActions csharp,
    ISourceFiles sourceFiles,
    AssistedCompileParameters parameters) 
    : IAction<AssistedCompileParameters, FileMemoryItem>
{
    public async Task<FileMemoryItem> InvokeAsync()
    {
        var (project, relevantFile) = parameters;
        
        var context = "";
        if (parameters.ContextMemoryItems.Any())
        {
            context = "\nThese items may also provide helpful context:\n" + 
                string.Join("\n", parameters.ContextMemoryItems
                    .Select(i => $"File: {i.RelativePath}\n{i.Content}" ));
        }

        var result = await csharp.CompileProjectAsync(new(project));
        if (result is CompilationResult.Failure failure)
        {
            var (resolutionResult, fixedMemoryItem) = await TryResolveFailedCompilationAsync(
                failure, context);
            if (resolutionResult is CompilationResult.Failure)
            {
                throw new("Resolution failed");
            }
            else
            {
                return fixedMemoryItem ?? throw new NullReferenceException(nameof(fixedMemoryItem));
            }
        }

        return relevantFile;
    }

    private async Task<(CompilationResult, FileMemoryItem?)> TryResolveFailedCompilationAsync(
        CompilationResult lastResult, string context)
    {
        var (project, lastFile) = parameters;
        var maxAttempts = 3;
        FileMemoryItem? classMemoryItem = null;
        for (int i = 0; i < maxAttempts; i++)
        {
            var diagnosticMessages = lastResult.Output.Errors;
            var messagesText = string.Join(Environment.NewLine, diagnosticMessages);
            var response = await assistant.CompletePromptAsync($"""
                {Prompts.CSharpPrompt}
                {context}

                Given the following errors:
                {messagesText}
                
                Transform the following file to resolve the errors:
                ```
                {lastFile.Content}
                ```
                
                Begin Output:
                """);
            
            classMemoryItem = await SanitizeAndWriteClassAsync(response);
            
            lastResult = await csharp.CompileProjectAsync(new(project));
            if (lastResult is CompilationResult.Success)
            {
                break;
            }
        }
        return (lastResult, classMemoryItem);
    }

    private async Task<FileMemoryItem> SanitizeAndWriteClassAsync(string response)
    {
        var (project, relevantFile) = parameters;
        var sanitized = ExtractCodeBlocks(response);

        logger.LogInformation(sanitized);

        var path = relevantFile.RelativePath;
            
        await sourceFiles.WriteFileAsync(path, sanitized);
        
        return new FileMemoryItem(path, sanitized);
    }
    
    private static string ExtractCodeBlocks(string input)
    {
        // Pattern to match code blocks, capturing the content inside the backticks
        string pattern = @"```(?:[^`\n]*\n)?(.*?)```";
        var matches = Regex.Matches(input, pattern, RegexOptions.Singleline);

        // Use StringBuilder to concatenate all the code block contents
        var result = new StringBuilder();
        foreach (Match match in matches)
        {
            // Append the content of the code block to the result
            if (match.Groups.Count > 1)
            {
                result.AppendLine(match.Groups[1].Value);
            }
        }

        return result.ToString().TrimEnd(); // TrimEnd to remove the last newline added by AppendLine
    }
}