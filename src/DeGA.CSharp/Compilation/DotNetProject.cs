﻿using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace DeGA.CSharp.Compilation;

public class DotNetProject(string path, ILogger<DotNetProject> logger)
{
    private static bool s_isInitialized; // TODO: thread safety, with lazy?

    public string BasePath => Path.GetDirectoryName(path)!;

    public async Task<bool> TryCompileAsync()
    {
        if (!s_isInitialized)
        {
            s_isInitialized = true;
            MSBuildLocator.RegisterDefaults();
        }

        // Create an instance of MSBuildWorkspace
        var workspace = MSBuildWorkspace.Create();

        // Open the project
        var project = await workspace.OpenProjectAsync(path);

        // Compile the project
        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException("No compilation");

        // Handle errors and warnings
        foreach (var diagnostic in compilation.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                logger.LogInformation("Error: {error}", diagnostic.GetMessage());
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                logger.LogInformation("Warning: {warning}", diagnostic.GetMessage());
            }
        }

        // Emit the compilation to a DLL or an executable
        var projectRoot = Path.GetDirectoryName(path)!;
        var binDirectory = Path.Combine(projectRoot, "bin", "generate");
        Directory.CreateDirectory(binDirectory);
        var dllPath = Path.Combine(binDirectory, Path.GetFileNameWithoutExtension(path) + ".dll");
        var result = compilation.Emit(dllPath);

        return result.Success;
    }
}
