namespace Plugin.UplayServer.Rest.Applications;

/// <summary>
/// GET /v2/applications/{appId}/configuration mostly-static endpoint map +
/// feature switches. The title is resolved per request by
/// <see cref="TitleRegistry"/> off the {appId} route param / Ubi-AppId header
/// </summary>
internal static class Configuration
{
    [HTTP("GET", "/v2/applications/{appId}/configuration")]
    public static bool GetConfiguration(ServerSender server)
    {
        var title = TitleRegistry.ResolveForApp(server);
        if (title is null) return false;

        var body = TitleRegistry.Body(title, "configuration.json");
        if (string.IsNullOrEmpty(body)) return false;

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
    }
}
