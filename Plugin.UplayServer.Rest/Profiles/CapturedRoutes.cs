namespace Plugin.UplayServer.Rest.Profiles;

internal static class CapturedRoutes
{
    private static bool Serve(ServerSender server, string fileName, bool requireMatchingSpace = false)
    {
        var title = requireMatchingSpace
            ? TitleResolver.ResolveForSpace(server)
            : TitleResolver.Resolve(server);
        if (title is null) return false;

        var body = TitleResolver.Body(title, fileName);
        if (string.IsNullOrEmpty(body)) return false;

        server.Parameters.TryGetValue("profileId", out var pid);
        string? filled = WithProfileId(body, pid);
        if (filled is null) return false;

        server.Response.MakeGetResponse(filled, "application/json");
        server.SendResponse();
        return true;
    }

    private const string ProfileIdPlaceholder = "{profileId}";

    /// <summary>
    /// Fills a captured body's profile id placeholder from the URL, or null
    /// when the URL's value is not a profile id.
    /// </summary>
    internal static string? WithProfileId(string body, string? profileId)
    {
        if (!body.Contains(ProfileIdPlaceholder, StringComparison.Ordinal))
            return body;
        if (string.IsNullOrEmpty(profileId))
            return body.Replace(ProfileIdPlaceholder, "");
        if (!Guid.TryParse(profileId, out Guid parsed))
            return null;
        return body.Replace(ProfileIdPlaceholder, parsed.ToString("D"));
    }

    [HTTP("GET", "/v1/spaces/{spaceId}/items/all")]
    public static bool ItemsAll(ServerSender s) => Serve(s, "items_all.json", requireMatchingSpace: true);

    [HTTP("GET", "/v1/spaces/{spaceId}/configs/primarystore")]
    public static bool ConfigsPrimaryStore(ServerSender s) => Serve(s, "configs_primarystore.json", requireMatchingSpace: true);

    [HTTP("GET", "/v1/profiles/{profileId}/rewards?{!args}")]
    public static bool Rewards(ServerSender s) => Serve(s, "profile_rewards.json");

    [HTTP("PUT", "/v1/profiles/{profileId}/actions")]
    public static bool Actions(ServerSender s) => Serve(s, "profile_actions.json");

    [HTTP("GET", "/v1/profiles/me/news?{!args}")]
    public static bool News(ServerSender s) => Serve(s, "profile_news.json");

    [HTTP("GET", "/v1/profiles/stats?{!args}")]
    public static bool Stats(ServerSender s) => Serve(s, "profile_stats.json");

    [HTTP("GET", "/v3/users/{profileId}?{!args}")]
    public static bool User(ServerSender s) => Serve(s, "user.json");

    [HTTP("POST", "/v3/profiles/me/events")]
    public static bool Events(ServerSender s) => Serve(s, "events.json");

    [HTTP("POST", "/v1/profiles/me/inventory/primarystore")]
    public static bool InventoryPrimaryStore(ServerSender s) => Serve(s, "inventory_primarystore.json");

    [HTTP("GET", "/v1/spaces/{spaceId}/offers?{!args}")]
    public static bool Offers(ServerSender s) => Serve(s, "offers.json", requireMatchingSpace: true);

    [HTTP("GET", "/v1/profiles/{profileId}/inventory?{!args}")]
    public static bool Inventory(ServerSender s) => Serve(s, "profile_inventory.json");
}
