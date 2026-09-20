namespace Plugin.UplayServer.Rest.LegacyRoutes;

/// <summary>
/// Pre-UbiServices titles (Far Cry 3-era and earlier) call a WCF-style
/// ".svc" REST endpoint over plain HTTP instead of the modern UbiServices
/// API — no Ubi-AppId header, no appId/spaceId, just an "onlineConfigID"
/// GUID baked into the client. Real Ubisoft backends for these are gone, so
/// there is no way to capture a ground-truth response the way FCND/FC5's
/// were captured — the response shape for each title has to come from
/// trial-and-error against the game's own reaction, cross-checked against
/// what static decompilation of its parsing code expects. See
/// TitleMatchers.LegacyOnlineConfigId and Titles/&lt;Game&gt;/online_config.*
/// for where each title's (still evolving) capture lives.
///
/// For Far Cry 3 specifically, Ghidra decompilation of FC3_d3d11.dll
/// confirmed the body is JSON (JobPopulateConfig's document parser checks
/// literal true/false/null and '"'/digit/'['/'{' — textbook JSON value
/// parsing, no XML in sight), structured as an array of entries each with a
/// "Name" string and a "Values" array of strings — the parser walks the
/// root array, looks up "Name" and "Values" on each element, and only
/// records an entry if both are present.
/// </summary>
internal static class OnlineConfigService
{
    // Tried in this order; whichever file exists on disk wins, and its
    // extension picks the Content-Type. Renaming/replacing the file as
    // reverse-engineering narrows down the real shape needs no code change.
    private static readonly (string FileName, string ContentType)[] Candidates =
    [
        ("online_config.json", "application/json"),
        ("online_config.xml", "text/xml"),
        ("online_config.txt", "text/plain"),
    ];

    [HTTP("GET", "/OnlineConfigService.svc/GetOnlineConfig?{!args}")]
    public static bool GetOnlineConfig(ServerSender s)
    {
        var title = TitleResolver.ResolveLegacy(s);
        if (title is null) return false;

        foreach (var (fileName, contentType) in Candidates)
        {
            var body = TitleResolver.Body(title, fileName);
            if (body is null) continue;
            s.Response.MakeGetResponse(body, contentType);
            // Close rather than keep-alive: these pre-UbiServices clients are
            // HTTP/1.0-era, and NetCoreServer otherwise leaves the connection
            // open indefinitely (it sets Content-Length correctly but never
            // disconnects), which left a socket lingering ESTABLISHED long
            // after the exchange finished — see doc 54's note about the FIN
            // arriving 14s late.
            //
            // Correctness only. This does NOT fix FC3's startup freeze, and an
            // earlier revision of this comment wrongly claimed it did: that
            // claim rested on reading gdb's "thread 1", which under Wine is the
            // loader thread parked in __wine_main, not the game's main thread.
            // FC3 completed the whole RendezVous sequence and froze anyway both
            // before and after this change. See doc 55.
            s.Response.SetHeader("Connection", "close");
            s.SendResponse();
            s.Session.Disconnect();
            return true;
        }

        // No capture yet. An empty *string* body is actually wrong here, not
        // just uninformative — traced through the client's parser (Ghidra):
        // FUN_10b33e40("") returns null, so the caller's result stays at its
        // initialized 0xffffffff and gets treated as a parse FAILURE, not
        // "no config, use defaults". A valid empty JSON array parses to zero
        // entries and passes the client's own success check, so it reads as
        // a clean "no entries" response instead of an error. This isn't a
        // guess about the schema's *content* (still unknown, still needs
        // real entries via trial-and-error) — it's just the minimal legal
        // value for a format we've now confirmed. Do NOT go further than
        // this and fabricate plausible-looking entries: guessed payloads
        // have caused hard bugs elsewhere in this codebase every time they
        // were tried (see TitleResolver.Body).
        Log.Warning("[rest] {Name}: no OnlineConfigService capture yet (looked for "
                  + "online_config.{{json,xml,txt}} in {Dir}) — returning empty JSON array",
            title.Name, title.ResourceDir);
        s.Response.MakeGetResponse("[]", "application/json");
        s.Response.SetHeader("Connection", "close");
        s.SendResponse();
        s.Session.Disconnect();
        return true;
    }
}
