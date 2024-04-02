using Wolder.Core.Workspace;

namespace Wolder.Actions.Http;

public record DownloadContentParameters(
    string Url);

public class DownloadContent(DownloadContentParameters parameters)
    : IAction<DownloadContentParameters, RemoteFileMemoryItem>
{
    private static readonly HttpClient Client = new HttpClient();
    
    public async Task<RemoteFileMemoryItem> InvokeAsync()
    {
        string content = await Client.GetStringAsync(parameters.Url);
        return new RemoteFileMemoryItem(parameters.Url, content);
    }
}
