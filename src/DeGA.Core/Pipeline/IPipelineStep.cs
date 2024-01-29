﻿namespace DeGA.Core.Pipeline;

public interface IPipelineStep
{
    Type DefinitionType { get; }
    IActionDefinition GetDefinition(IPipelineContext context);
}