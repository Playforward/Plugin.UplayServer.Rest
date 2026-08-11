using System.Security.Cryptography;
using System.Text;

namespace Plugin.UplayServer.Rest.Profiles;

/// <summary>
/// POST /v3/profiles/sessions the identity handshake.
/// </summary>
internal static class Sessions
{
    [HTTP("POST", "/v3/profiles/sessions")]
    public static bool CreateSession(ServerSender server)
    {
        var accountId = ParseBasicAuthAccountId(server) ?? Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var spaceId = TitleRegistry.Resolve(server)?.SpaceId ?? "";

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
            sessionId = Guid.NewGuid().ToString(),
            sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            clientIp = "127.0.0.1",
            clientIpCountry = "US",
            serverTime = now.ToString("o"),
            twoFactorAuthenticationTicket = (string?)null,
            rememberMeTicket = (string?)null,
        });

        server.Response.MakeGetResponse(body, "application/json");
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
            return sep < 0 ? decoded : decoded[..sep];
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateOpaqueToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(96));
}
