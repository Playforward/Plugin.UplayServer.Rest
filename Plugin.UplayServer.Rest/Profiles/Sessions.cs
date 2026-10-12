using System.Security.Cryptography;
using System.Text;

namespace Plugin.UplayServer.Rest.Profiles;

/// <summary>
/// POST /v3/profiles/sessions the identity handshake.
/// </summary>
internal static class Sessions
{
    // Far Cry 4 ships UbiServices SDK 2.1.6 (User-Agent
    // "UbiServices_SDK_HTTP_Client_2.1.6.123313_PC"), which posts the identity
    // handshake to /v2/profiles/sessions; later titles use /v3. The request and
    // response shapes are the same, so route both at the one handler.
    // HTTPAttribute is not AllowMultiple, hence a delegating overload rather
    // than a second attribute on CreateSession.
    [HTTP("POST", "/v2/profiles/sessions")]
    public static bool CreateSessionV2(ServerSender server) => CreateSession(server);

    [HTTP("POST", "/v3/profiles/sessions")]
    public static bool CreateSession(ServerSender server)
    {
        Log.Debug("[ubiservices] POST {Path} ({Length} body bytes)",
            server.Request.Url, server.Request.Body?.Length ?? 0);
        var accountId = ParseBasicAuthAccountId(server) ?? Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid().ToString();

        var spaceId = TitleResolver.Resolve(server)?.Matchers.SpaceId ?? "";

        var body = JsonSerializer.Serialize(new
        {
            platformType = "uplay",
            ticket = GenerateOpaqueToken(),
            profileId = accountId,
            userId = accountId,
            nameOnPlatform = "user",
            environment = "Prod",
            expiration = now.AddHours(3).ToString("o"),
            spaceId,
            sessionId,
            sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            clientIp = "127.0.0.1",
            clientIpCountry = "US",
            serverTime = now.ToString("o"),
            twoFactorAuthenticationTicket = (string?)null,
            rememberMeTicket = (string?)null,
        });

        server.Response.MakeGetResponse(body, "application/json");
        // UbiServices carries this value forward out-of-band.  FC4 reads the
        // response session identifier and sends Ubi-SessionId on the next
        // configuration/bootstrap requests; returning it only in JSON leaves
        // the SDK in its generic "service unavailable" state.
        server.Response.SetHeader("Ubi-SessionId", sessionId);
        server.Response.SetCookie("Ubi-SessionId", sessionId, path: "/");
        Log.Debug("[ubiservices] session created for space {SpaceId}", spaceId);
        server.SendResponse();
        return true;
    }

    // DELETE /v2/profiles/sessions — logout/session teardown, sent with an
    // empty "{}" body. We hold no session state server-side to invalidate, so
    // just acknowledge it; the client only needs the 200, not any particular
    // body shape.
    [HTTP("DELETE", "/v2/profiles/sessions")]
    [HTTP("DELETE", "/v3/profiles/sessions")]
    public static bool CloseSession(ServerSender server)
    {
        server.Response.MakeGetResponse("{}", "application/json");
        server.SendResponse();
        return true;
    }

    private static string? ParseBasicAuthAccountId(ServerSender server)
    {
        // NOTE: ModdableWebServer lower-cases header keys (RequestExport.
        // PopulateHeaders) looking up "Authorization" here never matched,
        // so every login silently fell through to a random AccountId instead
        // of the client's own.
        if (!server.Headers.TryGetValue("authorization", out string? auth) || string.IsNullOrEmpty(auth))
            return null;

        const string prefix = "Basic ";
        if (!auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth[prefix.Length..]));
            var sep = decoded.IndexOf(':');
            var username = sep < 0 ? decoded : decoded[..sep];
            // Far Cry 4's local Uplay emulation sends "Basic <base64 of
            // ':my password'>" -- an EMPTY username. decoded[..0] is "", not
            // null, so the caller's `?? Guid.NewGuid()` fallback never fired:
            // every session response went out with profileId/userId = "".
            // Fall through to a random id for that case exactly like a
            // missing header, instead of handing back an empty identity.
            return string.IsNullOrWhiteSpace(username) ? null : username;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateOpaqueToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(96));
}
