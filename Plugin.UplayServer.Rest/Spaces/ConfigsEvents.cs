namespace Plugin.UplayServer.Rest.Spaces;

/// <summary>
/// GET /v4/spaces/{spaceId}/configs/events 
/// </summary>
internal static class ConfigsEvents
{
    [HTTP("GET", "/v4/spaces/{spaceId}/configs/events")]
    public static bool GetConfigsEvents(ServerSender server)
    {
        var title = TitleRegistry.ResolveForSpace(server);
        if (title is null) return false;

        var body = TitleRegistry.Body(title, "configs_events.json");
        if (string.IsNullOrEmpty(body)) return false;

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
    }
}
