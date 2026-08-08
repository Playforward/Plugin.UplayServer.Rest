namespace Plugin.UplayServer.Rest.Profiles;

/// <summary>
/// GET /v1/profiles/me/populations 
/// </summary>
internal static class Populations
{
    [HTTP("GET", "/v1/profiles/me/populations?{!args}")]
    public static bool GetPopulations(ServerSender server)
    {
        server.Response.MakeGetResponse("""{"populations":[]}""", "application/json");
        server.SendResponse();
        return true;
    }

    /// <summary>
    /// PUT /v1/profiles/me/populations/data 
    /// </summary>
    [HTTP("PUT", "/v1/profiles/me/populations/data")]
    public static bool PutPopulationsData(ServerSender server)
    {
        server.Response.MakeOkResponse(204);
        server.SendResponse();
        return true;
    }
}
