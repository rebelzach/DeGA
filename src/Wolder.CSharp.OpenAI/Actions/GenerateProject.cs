using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Wolder.CommandLine;
using Wolder.CommandLine.Actions;
using Wolder.Core.Assistants;
using Wolder.Core.Files;
using Wolder.CSharp.Compilation;
using Wolder.Core.Workspace;

namespace Wolder.CSharp.OpenAI.Actions;

public enum ProjectType
{
    Blazor
}

public record GenerateProjectParameters(string Name, string Prompt, ProjectType ProjectType)
{
    public IEnumerable<FileMemoryItem> ContextMemoryItems { get; init; } = 
        Enumerable.Empty<FileMemoryItem>();
}

public class GenerateProject(
    IAIAssistant assistant,
    ILogger<GenerateProject> logger,
    CommandLineActions commandLineActions,
    ISourceFiles sourceFiles,
    GenerateProjectParameters parameters) 
    : IAction<GenerateProjectParameters, DotNetProjectReference>
{
    public async Task<DotNetProjectReference> InvokeAsync()
    {
        var helpContextCommand = parameters.ProjectType switch
        {
            ProjectType.Blazor => "dotnet new blazor -h",
            _ => throw new InvalidOperationException("Unknown project type.")
        };
        var helpOutput = await commandLineActions.ExecuteCommandLineAsync(
            new ExecuteCommandLineParameters(helpContextCommand));
        var commandResult = await assistant.CompletePromptAsync(
            $"You are a dotnet net8 project generator. Your only output should be a `dotnet new` " +
            $"CLI command that will create a dotnet project. Create a command that will create a project with name '{parameters.Name}' Project requirements: {parameters.Prompt} \n" +
            $"Here is the output of `{helpContextCommand}` for reference:\n" +
            $"---Begin Output---" +
            $"{helpOutput.Output}" +
            $"---End Output---" +
            $"\n> ");
        await commandLineActions.ExecuteCommandLineAsync(
            new ExecuteCommandLineParameters(commandResult));
        return new DotNetProjectReference(
            Path.Join(parameters.Name, $"{parameters.Name}.csproj"), parameters.Name);
    }
}