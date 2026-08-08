using System.Security.Cryptography;
using System.Text;

namespace Plugin.UplayServer.Rest.Profiles;

/// <summary>
/// POST /v3/profiles/sessions
/// </summary>
internal static class Sessions
{
    [HTTP("POST", "/v3/profiles/sessions")]
    public static bool CreateSession(ServerSender server)
    {
        var accountId = ParseBasicAuthAccountId(server) ?? Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var body = JsonSerializer.Serialize(new
        {
            platformType = "uplay",
            ticket = GenerateOpaqueToken(),
            profileId = accountId,
            userId = accountId,
            nameOnPlatform = "user",
            environment = "Prod",
            expiration = now.AddHours(3).ToString("o"),
            spaceId = "6f75883b-3b5b-45ce-a2d4-2a7a8477180c", // TODO remove hardcoded spaces_id
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
        if (!server.Headers.TryGetValue("Authorization", out string? auth) || string.IsNullOrEmpty(auth))
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
