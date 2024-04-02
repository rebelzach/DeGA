namespace Wolder.Core.Files;

public interface ISourceFiles : IWorkspaceFileSystem
{
    FileMemoryItem CreateFileMemoryItem(string relativePath);
}