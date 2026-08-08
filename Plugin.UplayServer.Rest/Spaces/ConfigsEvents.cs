using System.Reflection;

namespace Plugin.UplayServer.Rest.Spaces;

/// <summary>
/// GET /v4/spaces/{spaceId}/configs/events
/// </summary>
internal static class ConfigsEvents
{
    private static readonly string CannedBody = LoadCannedBody();
    //TODO remove hardcoded spaces_id
    [HTTP("GET", "/v4/spaces/6f75883b-3b5b-45ce-a2d4-2a7a8477180c/configs/events")]
    public static bool GetConfigsEvents(ServerSender server)
    {
        server.Response.MakeGetResponse(CannedBody, "application/json");
        server.SendResponse();
        return true;
    }

    private static string LoadCannedBody()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("configs_events.json", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
