
namespace Wolder.CSharp;

public record DotNetSolutionReference(string RelativeFilePath)
{
    public string Name => Path.GetFileNameWithoutExtension(RelativeFilePath)!;
}