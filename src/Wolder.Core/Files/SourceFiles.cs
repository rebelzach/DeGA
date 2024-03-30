namespace Wolder.Core.Files;

public class SourceFiles(WorkspaceRootPath rootPath)
    : WorkspaceFileSystem(rootPath, "src"), ISourceFiles
{
    public override void CleanDirectory()
    {
        var rootPath = RootDirectoryPath;

        if (!Path.Exists(rootPath))
        {
            base.CleanDirectory();
            return;
        }
        
        // Archive the past run into a "runs" directory.
        // Get the parent directory of the root path
        var parentDir = Directory.GetParent(rootPath)?.FullName
            ?? throw new NullReferenceException("rootPath FullName");

        // Ensure the 'runs' directory exists in the parent directory
        var runsDir = Path.Combine(parentDir, "runs");
        Directory.CreateDirectory(runsDir);

        // Create a timestamped directory name
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var archiveDir = Path.Combine(runsDir, timestamp);

        // Move the root directory to the new archive directory
        Directory.Move(rootPath, archiveDir);

        base.CleanDirectory();
    }
}
