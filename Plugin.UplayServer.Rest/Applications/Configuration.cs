namespace Plugin.UplayServer.Rest.Applications;

/// <summary>
/// GET /v2/applications/{appId}/configuration mostly-static endpoint map +
/// feature switches. The title is resolved per request by
/// <see cref="TitleResolver"/> off the {appId} route param / Ubi-AppId header
/// </summary>
internal static class Configuration
{
    // Far Cry 4's SDK (2.1.6) requests /v1/applications/{appId}/configuration;
    // later titles use /v2. Same shape, so route both at the one handler.
    [HTTP("GET", "/v1/applications/{appId}/configuration")]
    public static bool GetConfigurationV1(ServerSender server) => GetConfiguration(server);

    [HTTP("GET", "/v2/applications/{appId}/configuration")]
    public static bool GetConfiguration(ServerSender server)
    {
        Log.Information("[fc4-capture] GET {Url} headers={Headers}", server.Request.Url,
            string.Join(", ", server.Headers.Select(h => h.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase)
                ? $"{h.Key}=<redacted>" : $"{h.Key}={h.Value}")));
        var title = TitleResolver.ResolveForApp(server);
        if (title is null) return false;

        var body = TitleResolver.Body(title, "configuration.json");
        if (string.IsNullOrEmpty(body)) return false;

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
    }
}
