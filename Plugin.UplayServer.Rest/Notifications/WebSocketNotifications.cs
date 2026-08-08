using ModdableWebServer.Interfaces;

namespace Plugin.UplayServer.Rest.Notifications;

/// <summary>
/// GET /v2/websocket 
/// </summary>
internal static class WebSocketNotifications
{
    // TODO remove hardcoded app id
    private const string SourceApplicationId = "5dbe68ea-4d67-489f-9150-ff9f1ef543be";

    [WS("/v2/websocket?{!args}", WebSocketMethodListen.Connected)]
    public static void OnConnected(WebSocketSender sender)
    {
        if (sender.Session is not IWSSession session)
            return;

        string connectionId = Guid.NewGuid().ToString();
        string notification = JsonSerializer.Serialize(new
        {
            content = new
            {
                connectionId,
                contactUrl = $"https://public-im-ubiservices.ubi.com/v2/connections/{connectionId}/notifications",
            },
            notificationType = "US_CONNECTION_ESTABLISHED",
            sourceApplicationId = SourceApplicationId,
            datePosted = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        });

        session.SendText(notification);
    }
}
