using System.Text.Json.Nodes;

namespace Plugin.UplayServer.Rest.Spaces;

/// <summary>
/// GET /v1/spaces/{spaceId}/sandboxes/{sandbox}/rendezvous/configuration the
/// RendezVous (Quazal) bootstrap.
/// </summary>
internal static class RendezVousConfiguration
{
    [HTTP("GET", "/v1/spaces/{spaceId}/sandboxes/{sandbox}/rendezvous/configuration")]
    public static bool GetRendezVousConfiguration(ServerSender server)
    {
        var title = TitleResolver.ResolveForSpace(server);
        if (title is null) return false;

        var body = TitleResolver.Body(title, "rendezvous_configuration.json");
        if (string.IsNullOrEmpty(body)) return false;

        if (title.AdvertiseLocalRendezVousIdentity)
        {
            if (!RendezVousIdentity.TryGetEcs1PublicKey(title.RendezVousIdentityFile, out string publicKey, out string error))
            {
                Log.Warning("[rest] {Name}: local RendezVous identity unavailable ({Error}); serving captured configuration", title.Name, error);
            }

            else if (JsonNode.Parse(body) is not JsonObject configuration)
            {
                Log.Error("[rest] {Name}: rendezvous_configuration.json is not an object", title.Name);
                return false;
            }
            else
            {
                configuration["public_key"] = publicKey;
                body = configuration.ToJsonString();
                Log.Information("[rest] {Name}: serving local RendezVous ECS1 identity", title.Name);
            }
        }

        server.Parameters.TryGetValue("sandbox", out var sandbox);
        Log.Information("[rest] {Name}: RendezVous configuration for sandbox {Sandbox}",
            title.Name, sandbox ?? "(none)");

        server.Response.MakeGetResponse(body, "application/json");
        server.SendResponse();
        return true;
    }
}
