using System.Collections.Concurrent;
using System.Reflection;

namespace Plugin.UplayServer.Rest;

/// <summary>
/// Resolves which title a request belongs to, and serves that title's canned
/// responses. Route handlers are static (ModdableWebServer's dispatch requires
/// it) so they cannot reach the plugin instance's Config this is the bridge.
/// </summary>
internal static class TitleRegistry
{
    private static readonly ConcurrentDictionary<string, string> BodyCache = new();
    private static volatile TitleSettings[] _titles = [];

    /// <summary>Published by <see cref="UplayRestPlugin.Start"/> before routes are merged.</summary>
    public static void Publish(IEnumerable<TitleSettings> titles)
    {
        _titles = titles.Where(t => !string.IsNullOrWhiteSpace(t.AppId)).ToArray();
        BodyCache.Clear();
    }

    public static IReadOnlyList<TitleSettings> All => _titles;

    public static TitleSettings? Resolve(ServerSender server)
    {
        // ModdableWebServer lower-cases header keys (RequestExport.PopulateHeaders).
        if (server.Headers.TryGetValue("ubi-appid", out var hdr) && !string.IsNullOrWhiteSpace(hdr))
        {
            var byHeader = Find(t => Eq(t.AppId, hdr));
            if (byHeader is not null) return byHeader;
            Log.Warning("[rest] request from unconfigured title (Ubi-AppId {AppId}) — add it to "
                      + "Configs/UplayServer.RestConfig.json with its captured responses", hdr);
            return null;
        }
        if (server.Parameters.TryGetValue("appId", out var appId))
            return Find(t => Eq(t.AppId, appId));
        if (server.Parameters.TryGetValue("spaceId", out var spaceId))
            return Find(t => Eq(t.SpaceId, spaceId));
        return null;
    }

    public static TitleSettings? ResolveForSpace(ServerSender server)
    {
        var title = Resolve(server);
        if (title is null) return null;
        if (server.Parameters.TryGetValue("spaceId", out var spaceId) && !Eq(title.SpaceId, spaceId))
        {
            Log.Warning("[rest] {Name} asked for spaceId {Asked} but is configured as {Configured}",
                title.Name, spaceId, title.SpaceId);
            return null;
        }
        return title;
    }

    public static TitleSettings? ResolveForApp(ServerSender server)
    {
        var title = Resolve(server);
        if (title is null) return null;
        if (server.Parameters.TryGetValue("appId", out var appId) && !Eq(title.AppId, appId))
            return null;
        return title;
    }

    public static string? Body(TitleSettings title, string fileName)
        => BodyCache.GetOrAdd($"{title.AppId}/{fileName}", _ =>
        {
            if (!string.IsNullOrWhiteSpace(title.ResourceDir))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), title.ResourceDir, fileName);
                if (File.Exists(path))
                {
                    Log.Information("[rest] {Name}: serving {File} from {Path}", title.Name, fileName, path);
                    return File.ReadAllText(path);
                }
                Log.Warning("[rest] {Name}: {Path} missing — falling back to the built-in FCND "
                          + "capture with identifiers retargeted. Capture the real response "
                          + "(farcry-lan/server/bootstrap.py --no-spoof --dump-bodies) and drop it "
                          + "there; guessed/substituted payloads have caused hard bugs here every time.",
                    title.Name, path);
            }
            var embedded = Embedded(fileName);
            if (embedded is null)
            {
                Log.Warning("[rest] {Name}: no capture for {File} (looked in {Dir}) — returning 404",
                    title.Name, fileName,
                    string.IsNullOrWhiteSpace(title.ResourceDir) ? "(no ResourceDir set)" : title.ResourceDir);
                return "";
            }
            return embedded
                .Replace(FcndSpaceId, title.SpaceId)
                .Replace(FcndAppId, title.AppId);
        }) is { Length: > 0 } body ? body : null;

    private const string FcndSpaceId = "6f75883b-3b5b-45ce-a2d4-2a7a8477180c";
    private const string FcndAppId = "4eb9089b-324e-4e65-a055-5ea05cfeb68f";

    private static string? Embedded(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null) return null;
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static TitleSettings? Find(Func<TitleSettings, bool> pred)
        => Array.Find(_titles, new Predicate<TitleSettings>(pred));

    private static bool Eq(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
