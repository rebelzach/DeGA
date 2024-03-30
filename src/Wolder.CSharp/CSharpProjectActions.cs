﻿using Wolder.Core.Workspace;
using Wolder.CSharp.ProjectActions;

namespace Wolder.CSharp;

[GenerateActionCall<SetNullability>]
[GenerateActionCall<CompileProject>]
[GenerateActionCall<CreateSolution>]
[GenerateActionCall<AddProjectToSolution>]
public partial class CSharpProjectActions
{
}