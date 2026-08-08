namespace Plugin.UplayServer.Rest.Profiles;

/// <summary>
/// POST /v1/profiles/me/remotelog
/// </summary>
internal static class RemoteLog
{
    [HTTP("POST", "/v1/profiles/me/remotelog")]
    public static bool PostRemoteLog(ServerSender server)
    {
        server.Response.MakeOkResponse(204);
        server.SendResponse();
        return true;
    }
}
