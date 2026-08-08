using System.Net;
using System.Net.Sockets;

namespace Plugin.UplayServer.Rest.Storm;

/// <summary>
/// GET /v{ver}/router/{n} 
/// </summary>
internal static class Router
{
    private const int RelayPort = 12000;

    private static readonly Lazy<string> Relay = new(() => $"{ResolveLanIp()}:{RelayPort}");

    [HTTP("GET", "/{ver}/router/{n}")]
    public static bool GetRouter(ServerSender server)
    {
        server.Response.MakeGetResponse($$"""{"router":"{{Relay.Value}}"}""", "application/json");
        server.SendResponse();
        return true;
    }

    private static string ResolveLanIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(new IPEndPoint(IPAddress.Parse("10.255.255.255"), 1));
            if (s.LocalEndPoint is IPEndPoint ep && !IPAddress.IsLoopback(ep.Address))
                return ep.Address.ToString();
        }
        catch
        {
            // fall through
        }
        return "127.0.0.1";
    }
}
