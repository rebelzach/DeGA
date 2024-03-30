using Wolder.CommandLine;
using Wolder.CommandLine.Actions;
using Wolder.Core.Workspace;

namespace Wolder.CSharp.ProjectActions;

public record AddProjectToSolutionParameters(
    DotNetProjectReference ProjectReference,
    DotNetSolutionReference SolutionReference);

public class AddProjectToSolution(
    CommandLineActions commandLine,
    AddProjectToSolutionParameters parameters) 
    : IVoidAction<AddProjectToSolutionParameters>
{
    public async Task InvokeAsync()
    {
        await commandLine.ExecuteCommandLineAsync(
            new ExecuteCommandLineParameters(
                $"dotnet sln {parameters.SolutionReference.RelativeFilePath} add {parameters.ProjectReference.RelativeFilePath}"));
    }
}