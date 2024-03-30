using Wolder.CommandLine;
using Wolder.CommandLine.Actions;
using Wolder.Core.Workspace;

namespace Wolder.CSharp.ProjectActions;

public record CreateSolutionParameters(string Name, string RelativePath = "");

public class CreateSolution(
    CommandLineActions commandLine,
    CreateSolutionParameters parameters) 
    : IAction<CreateSolutionParameters, DotNetSolutionReference>
{
    public async Task<DotNetSolutionReference> InvokeAsync()
    {
        await commandLine.ExecuteCommandLineAsync(
            new ExecuteCommandLineParameters(
                $"dotnet new sln -n {parameters.Name}", 
                parameters.RelativePath));
        return new DotNetSolutionReference(
            Path.Join(
                parameters.RelativePath,
                $"{parameters.Name}.sln"));
    }
}