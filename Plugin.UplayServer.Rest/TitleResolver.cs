using ServerShared.Titles;
using System.Collections.Concurrent;
using System.Reflection;

namespace Plugin.UplayServer.Rest;

/// <summary>
/// Resolves which title an HTTP request belongs to, and serves that title's
/// canned responses. The titles themselves come from the host's shared
/// <see cref="TitleRegistry"/>; this only knows how a UbiServices request
/// names one. Route handlers are static (ModdableWebServer's dispatch
/// requires it), which is why this is too.
/// </summary>
/// <remarks>
/// HTTP is resolved by content -- the <c>Ubi-AppId</c> header, then the
/// <c>spaceId</c> route parameter, then the <c>onlineConfigID</c> a
/// pre-UbiServices title sends -- because every title dials the same
/// bootstrap hostname, so unlike UDP there is no port to key on.
/// </remarks>
internal static class TitleResolver
{
    private static readonly ConcurrentDictionary<string, string> BodyCache = new();

    /// <summary>
    /// How many distinct unconfigured AppIds are reported. Far more than the
    /// handful of titles anybody sets up in one go; past it, the values are
    /// somebody sending noise.
    /// </summary>
    private const int UnconfiguredAppIdsReported = 64;

    private static readonly LogOnce UnconfiguredAppIds = new(UnconfiguredAppIdsReported);

    public static IReadOnlyList<TitleDescriptor> All => TitleRegistry.All;

    public static TitleDescriptor? Resolve(ServerSender server)
    {
        // ModdableWebServer lower-cases header keys (RequestExport.PopulateHeaders).
        if (server.Headers.TryGetValue("ubi-appid", out var hdr) && !string.IsNullOrWhiteSpace(hdr))
        {
            var byHeader = TitleRegistry.ByAppId(hdr);
            if (byHeader is not null) return byHeader;
            if (UnconfiguredAppIds.First(hdr, () => Log.Warning(
                    "[rest] {Count} distinct unconfigured Ubi-AppIds reported; further ones are not logged",
                    UnconfiguredAppIdsReported)))
            {
                Log.Warning("[rest] request from unconfigured title (Ubi-AppId {AppId}) — add it to "
                          + "Configs/{File} with its captured responses (logged once per AppId)",
                    hdr, TitleRegistry.ConfigFileName);
            }
            return null;
        }
        if (server.Parameters.TryGetValue("appId", out var appId))
            return TitleRegistry.ByAppId(appId);
        if (server.Parameters.TryGetValue("spaceId", out var spaceId))
            return TitleRegistry.BySpaceId(spaceId);
        return null;
    }

    public static TitleDescriptor? ResolveForSpace(ServerSender server)
    {
        var title = Resolve(server);
        if (title is null) return null;
        if (server.Parameters.TryGetValue("spaceId", out var spaceId) && !Eq(title.Matchers.SpaceId, spaceId))
        {
            Log.Warning("[rest] {Name} asked for spaceId {Asked} but is configured as {Configured}",
                title.Name, spaceId, title.Matchers.SpaceId);
            return null;
        }
        return title;
    }

    public static TitleDescriptor? ResolveForApp(ServerSender server)
    {
        var title = Resolve(server);
        if (title is null) return null;
        if (server.Parameters.TryGetValue("appId", out var appId) && !Eq(title.Matchers.AppId, appId))
            return null;
        return title;
    }

    /// <summary>
    /// Resolves a pre-UbiServices title (Far Cry 3-era: OnlineConfigService.svc
    /// et al.) by the "onlineConfigID" GUID it sends — the only identifier
    /// these clients ever provide. See LegacyRoutes/.
    /// </summary>
    public static TitleDescriptor? ResolveLegacy(ServerSender server)
    {
        if (!server.Parameters.TryGetValue("onlineConfigID", out var id) || string.IsNullOrWhiteSpace(id))
            return null;
        var title = TitleRegistry.ByLegacyOnlineConfigId(id);
        if (title is null)
            Log.Warning("[rest] legacy request with unconfigured onlineConfigID {Id} — add a title "
                      + "for it in Configs/{File} (Matchers.LegacyOnlineConfigId)", id, TitleRegistry.ConfigFileName);
        return title;
    }

    public static string? Body(TitleDescriptor title, string fileName)
        => BodyCache.GetOrAdd($"{title.TitleId}/{fileName}", _ =>
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
                .Replace(FcndSpaceId, title.Matchers.SpaceId)
                .Replace(FcndAppId, title.Matchers.AppId);
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

    private static bool Eq(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
