﻿namespace DeGA.Core.Pipeline;

internal record PipelineStep<TDefinition>(
    Func<IPipelineContext, TDefinition> DefinitionFactory) : IPipelineStep
    where TDefinition : IActionDefinition
{
    public Type DefinitionType => typeof(TDefinition);
    public IActionDefinition GetDefinition(IPipelineContext context) => DefinitionFactory(context);
}