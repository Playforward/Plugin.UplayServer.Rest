using System.Reflection;

namespace Plugin.UplayServer.Rest.Spaces;

/// <summary>
/// GET /v2/spaces/{spaceId}/entities 
/// </summary>
internal static class Entities
{
    private static readonly string CannedBody = LoadCannedBody();
    //TODO remove hardcoded spaces_id
    [HTTP("GET", "/v2/spaces/6f75883b-3b5b-45ce-a2d4-2a7a8477180c/entities?{!args}")]
    public static bool GetEntities(ServerSender server)
    {
        server.Response.MakeGetResponse(CannedBody, "application/json");
        server.SendResponse();
        return true;
    }

    private static string LoadCannedBody()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("entities.json", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
