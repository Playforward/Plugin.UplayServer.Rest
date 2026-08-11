namespace Plugin.UplayServer.Rest.Profiles;

internal static class CapturedRoutes
{
    private static bool Serve(ServerSender server, string fileName, bool requireMatchingSpace = false)
    {
        var title = requireMatchingSpace
            ? TitleRegistry.ResolveForSpace(server)
            : TitleRegistry.Resolve(server);
        if (title is null) return false;

        var body = TitleRegistry.Body(title, fileName);
        if (string.IsNullOrEmpty(body)) return false;

        if (body.Contains("{profileId}", StringComparison.Ordinal))
        {
            server.Parameters.TryGetValue("profileId", out var pid);
            body = body.Replace("{profileId}", pid ?? "");
        }

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
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
