using ModdableWebServer.Interfaces;
using ModdableWebServer.EventArguments;   // ReceivedFailedEventArgs
using NetCoreServer;                      // HttpRequest
using ServerShared.Controllers;
using ServerShared.Plugins;
using ServerShared.Titles;
using System.Reflection;

namespace Plugin.UplayServer.Rest;

public class UplayRestPlugin : ServerPlugin<UplayRestSettings>
{
    public override uint Priority => 0;

    public override string Name => "UplayServer.Rest";

    public override void Start()
    {
        WarnAboutTitlesLeftInThisConfig();

        foreach (var t in TitleResolver.All)
            Log.Information("[rest] serving {Title}: appId={AppId} spaceId={SpaceId} onlineConfigID={Legacy} resources={Res}",
                t, t.Matchers.AppId, t.Matchers.SpaceId, t.Matchers.LegacyOnlineConfigId,
                string.IsNullOrWhiteSpace(t.ResourceDir) ? "(built-in, retargeted)" : t.ResourceDir);

        // Not just port 443: pre-UbiServices titles (Far Cry 3-era, see
        // LegacyRoutes/) call plain-HTTP ".svc" endpoints on port 80, with no
        // Ubi-AppId header and no HTTPS at all — every configured HTTP server
        // needs these [HTTP] routes merged in, not just the main HTTPS one.
        foreach (var server in ServerController.Servers)
        {
            if (server.Server is not IHttpServer http)
                continue;

            http.DoReturn404IfFail = true;
            http.HTTPAttributeToMethods.Merge(Assembly.GetExecutingAssembly());

            if (server.Server is IWSServer ws)
                ws.WSAttributeToMethods.Merge(Assembly.GetExecutingAssembly());
        }

        // Log every request that matches no route. Without this the server
        // answers 404 and logs NOTHING about it, which is genuinely dangerous
        // to debug against: a title whose AppId isn't registered looks
        // identical to a client that never connected at all. That cost a long
        // session on Far Cry 4 — client-side TLS bypasses were chased for
        // hours on the assumption that "no request logged" meant "no request
        // arrived", when the handshake was fine the whole time and every
        // request was being silently 404'd purely because the title wasn't in
        // Titles yet.
        //
        // Ubi-AppId is called out explicitly because it is the single field
        // that decides whether a title resolves, so an unrecognised value
        // there is by far the most likely cause of an unexpected 404.
        ServerEvents.ReceivedFailed -= OnRequestUnmatched;
        ServerEvents.ReceivedFailed += OnRequestUnmatched;
    }

    /// <summary>
    /// The title list used to live in this plugin's config. Anything still
    /// there is named, once, with where it goes -- and not served, since a
    /// title this file describes and Titles.json does not is exactly the
    /// disagreement moving the list was meant to end. The stale entries are
    /// cleared so the warning does not repeat on every start.
    /// </summary>
    private void WarnAboutTitlesLeftInThisConfig()
    {
        bool stale = false;
        foreach (var legacy in Config.Titles ?? [])
        {
            bool known = TitleRegistry.ByAppId(legacy.AppId) is not null
                      || TitleRegistry.ByLegacyOnlineConfigId(legacy.LegacyOnlineConfigId) is not null;
            Log.Warning("[rest] '{Name}' is still listed in {Config}, which no longer serves titles; {Advice}",
                legacy.Name, ConfigFileName,
                known ? $"Configs/{TitleRegistry.ConfigFileName} already carries it, so the entry here can go"
                      : $"move it into Configs/{TitleRegistry.ConfigFileName} or its clients get 404s");
            stale = true;
        }
        if (!string.IsNullOrWhiteSpace(Config.AppId))
        {
            Log.Warning("[rest] a single-title AppId {AppId} is still set in {Config}; titles are declared in Configs/{File} now",
                Config.AppId, ConfigFileName, TitleRegistry.ConfigFileName);
            stale = true;
        }
        if (!stale)
            return;
        Config.Titles = null;
        Config.AppId = null;
        Config.SpaceId = null;
        SaveConfig();
    }

    private static void OnRequestUnmatched(object? sender, ReceivedFailedEventArgs e)
    {
        var request = e.Request;
        string url;
        try { url = Uri.UnescapeDataString(request.Url); }
        catch { url = request.Url; }

        string appId = "";
        var headers = new Dictionary<string, string>();
        try
        {
            request.PopulateHeaders(headers);
            headers.TryGetValue("ubi-appid", out appId!);
        }
        catch { /* header parsing is best-effort; never break on a log path */ }

        // Dump the full request on a 404. Knowing a route is missing is only
        // half of it -- to implement the handler you need the exact headers
        // (SDK version, auth scheme, AppId/SpaceId) and the request body, and
        // otherwise the only way to recover those is a packet capture against
        // a client whose TLS you have already had to defeat.
        try
        {
            foreach (var h in headers.OrderBy(h => h.Key))
            {
                // Don't spill credentials into a log file wholesale; the scheme
                // is what matters for implementing the route, not the secret.
                Log.Warning("[rest]   {Key}: {Value}", h.Key, Redact(h.Key, h.Value));
            }

            var body = request.Body;
            if (!string.IsNullOrEmpty(body))
                Log.Warning("[rest]   body ({Len} bytes): {Body}",
                    body.Length, body.Length > 2048 ? body[..2048] + " …(truncated)" : body);
            else
                Log.Warning("[rest]   body: (empty)");
        }
        catch { /* diagnostics only */ }

        if (!string.IsNullOrWhiteSpace(appId))
        {
            var title = TitleRegistry.ByAppId(appId);
            Log.Warning("[rest] 404 {Method} {Url} — Ubi-AppId={AppId} ({Diag})",
                request.Method, url, appId,
                title is null
                    ? $"NOT a configured title — add it to Configs/{TitleRegistry.ConfigFileName}"
                    : $"title '{title.Name}' IS configured, so the route itself is missing or the method/path differs");
        }
        else
        {
            Log.Warning("[rest] 404 {Method} {Url} — no Ubi-AppId header", request.Method, url);
        }
    }

    // Keep the auth scheme (Basic/Ubi_v1/…) because that is what the route has
    // to accept, but drop the secret itself so logs stay shareable.
    private static string Redact(string key, string value)
    {
        if (key is not "authorization" || value.Length <= 16)
            return value;
        var space = value.IndexOf(' ');
        var scheme = space > 0 ? value[..space] : value[..8];
        return scheme + " <redacted>";
    }

    public override void Stop()
    {
        ServerEvents.ReceivedFailed -= OnRequestUnmatched;
    }
}
