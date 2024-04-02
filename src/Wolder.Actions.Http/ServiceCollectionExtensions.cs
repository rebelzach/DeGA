using Wolder.Core.Workspace;

namespace Wolder.Actions.Http;

public static class ServiceCollectionExtensions
{
    public static GeneratorWorkspaceBuilder AddHttpActions(
        this GeneratorWorkspaceBuilder builder)
    {
        builder.AddActions<HttpActions>();
        
        return builder;
    }
}