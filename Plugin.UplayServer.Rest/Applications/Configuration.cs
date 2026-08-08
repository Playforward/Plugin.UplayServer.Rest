using System.Reflection;

namespace Plugin.UplayServer.Rest.Applications;

/// <summary>
/// GET /v2/applications/{appId}/configuration
/// </summary>
internal static class Configuration
{
    private static readonly string CannedBody = LoadCannedBody();
    //TODO remove hardcoded app id
    [HTTP("GET", "/v2/applications/4eb9089b-324e-4e65-a055-5ea05cfeb68f/configuration")]
    public static bool GetConfiguration(ServerSender server)
    {
        server.Response.MakeGetResponse(CannedBody, "application/json");
        server.SendResponse();
        return true;
    }

    private static string LoadCannedBody()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("configuration.json", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
