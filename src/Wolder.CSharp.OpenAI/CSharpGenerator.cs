using Wolder.Core.Workspace;
using Wolder.CSharp.OpenAI.Actions;

namespace Wolder.CSharp.OpenAI;

[GenerateActionCall<GenerateType>]
[GenerateActionCall<GenerateProject>]
[GenerateActionCall<GenerateClasses>]
[GenerateActionCall<GenerateBlazorComponent>]
[GenerateActionCall<TransformClass>]
[GenerateActionCall<AssistedCompile>]
public partial class CSharpGenerator
{
}