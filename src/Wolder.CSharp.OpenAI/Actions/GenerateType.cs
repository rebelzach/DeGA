using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.Core.Workspace;
using Wolder.CSharp.Compilation;

namespace Wolder.CSharp.OpenAI.Actions;

public class GenerateType(
    IAIAssistant assistant,
    ILogger<GenerateType> logger,
    CSharpActions csharp,
    ISourceFiles sourceFiles,
    GenerateTypeParameters parameters) 
    : IAction<GenerateTypeParameters, FileMemoryItem>
{
    public const string CSharpPrompt =
        "You are a helpful assistant that writes C# code to complete any task specified by me. " +
        "Your output will be directly written to a file where it will be compiled as part of a " +
        "larger C# project. Nullable references are enabled.";
    public async Task<FileMemoryItem> InvokeAsync()
    {
        var (project, typeNamespace, typeName, behaviorPrompt, memoryItems) = parameters;
        // Normalize the namespace to be relative to the project base namespace
        if (typeNamespace.StartsWith(project.BaseNamespace))
        {
            typeNamespace = typeNamespace.Substring(project.BaseNamespace.Length);
        }
        
//         var tree = sourceFiles.GetDirectoryTree();
//         var context = $$"""
//             
//             Directory Tree:
//             {{tree}}
//             """;
        var context = "";
        if (parameters.ContextMemoryItems.Any())
        {
            context = "\nThese items may also provide helpful context:\n" + 
                      string.Join("\n", parameters.ContextMemoryItems
                          .Select(i => i.ToPromptText()));
        }

        var namespaceEnd = string.IsNullOrEmpty(typeNamespace)
            ? ""
            : $".{typeNamespace}";
        var response = await assistant.CompletePromptAsync($"""
            {CSharpPrompt}
            {context}

            Create a type named `{typeName}` with namespace `{project.BaseNamespace}{namespaceEnd}` using the following description:
            {behaviorPrompt}

            Begin Output:
            """);
        
        var typeMemoryItem = await SanitizeAndWriteTypeAsync(response);

        var result = await csharp.CompileProjectAsync(new(project));
        if (result is CompilationResult.Failure failure)
        {
            var (resolutionResult, fixedMemoryItem) = await TryResolveFailedCompilationAsync(project, typeMemoryItem, failure, context);
            if (resolutionResult is CompilationResult.Failure)
            {
                throw new("Resolution failed");
            }
            else
            {
                return fixedMemoryItem ?? throw new NullReferenceException(nameof(fixedMemoryItem));
            }
        }

        return typeMemoryItem;
    }

    private async Task<(CompilationResult, FileMemoryItem?)> TryResolveFailedCompilationAsync(
        DotNetProjectReference project, FileMemoryItem lastFile, CompilationResult lastResult, string context)
    {
        var (projectRef, typeNamespace, typeName, behaviorPrompt, memoryItems) = parameters;
        var maxAttempts = 2;
        FileMemoryItem? typeMemoryItem = null;
        for (int i = 0; i < maxAttempts; i++)
        {
            var diagnosticMessages = lastResult.Output.Errors;
            var messagesText = string.Join(Environment.NewLine, diagnosticMessages);
            var response = await assistant.CompletePromptAsync($"""
                                                                {CSharpPrompt}
                                                                {context}

                                                                Given the following errors:
                                                                {messagesText}

                                                                Transform the following file to resolve the errors:
                                                                ```
                                                                {lastFile.Content}
                                                                ```

                                                                Begin Output:
                                                                """);
            
            typeMemoryItem = await SanitizeAndWriteTypeAsync(response);
            
            lastResult = await csharp.CompileProjectAsync(new(project));
            if (lastResult is CompilationResult.Success)
            {
                break;
            }
        }
        return (lastResult, typeMemoryItem);
    }

    private async Task<FileMemoryItem> SanitizeAndWriteTypeAsync(string response)
    {
        var (project, typeNamespace, typeName, behaviorPrompt, memoryItems) = parameters;
        var sanitized = Sanitize(response);

        logger.LogInformation(sanitized);

        var relativePath = typeNamespace.Replace('.', Path.PathSeparator);
        var path = Path.Combine(project.RelativeRoot, relativePath,  $"{typeName}.cs");
            
        await sourceFiles.WriteFileAsync(path, sanitized);
        
        return new FileMemoryItem(path, sanitized);
    }
    
    private static string Sanitize(string input)
    {
        string pattern = @"^\s*```\s*csharp|^\s*```|^\s*```\s*html|^\s*begin:|^\s*end:";
        string result = Regex.Replace(input, pattern, "",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return result;
    }
}