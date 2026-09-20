namespace Plugin.UplayServer.Rest.Spaces;

/// <summary>
/// GET /v2/spaces/{spaceId}/entities
/// </summary>
internal static class Entities
{
    [HTTP("GET", "/v2/spaces/{spaceId}/entities?{!args}")]
    public static bool GetEntities(ServerSender server)
    {
        var title = TitleResolver.ResolveForSpace(server);
        if (title is null) return false;

        var body = TitleResolver.Body(title, "entities.json");
        if (string.IsNullOrEmpty(body)) return false;

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
    }
}
