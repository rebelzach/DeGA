using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.Core.Memory;
using Wolder.CSharp.Compilation;
using Wolder.Core.Workspace;
using Wolder.CSharp.OpenAI.Constants;

namespace Wolder.CSharp.OpenAI.Actions;

public record AssistedCompileParameters(
    DotNetProjectReference Project, FileMemoryItem RelevantFiles, 
    IEnumerable<IMemoryItem>? ContextMemoryItems = null)
{
    public IEnumerable<IMemoryItem> ContextMemoryItems { get; init; } = 
        ContextMemoryItems ?? Enumerable.Empty<IMemoryItem>();
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
        var (project, relevantFile, memoryItems) = parameters;
        
        var context = "";
        if (parameters.ContextMemoryItems.Any())
        {
            context = "\nThese items may also provide helpful context:\n" + 
                      string.Join("\n", parameters.ContextMemoryItems
                          .Select(i => i.ToPromptText()));
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
        var (project, lastFile, memoryItems) = parameters;
        var maxAttempts = 4;
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
        var (project, relevantFile, memoryItems) = parameters;
        var sanitized = ParseUtilities.ExtractCodeBlocks(response);

        logger.LogInformation(sanitized);

        var path = relevantFile.RelativePath;
            
        await sourceFiles.WriteFileAsync(path, sanitized);
        
        return new FileMemoryItem(path, sanitized);
    }
}