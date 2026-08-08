using ModdableWebServer.Interfaces;
using ServerShared.Controllers;
using ServerShared.Plugins;
using System.Reflection;

namespace Plugin.UplayServer.Rest;

/// <summary>
/// Spoofs the real-Ubisoft UbiServices REST surface (public-ubiservices.ubi.com)
/// </summary>
public class UplayRestPlugin : ServerPlugin
{
    public override uint Priority => 0;

    public override string Name => "UplayServer.Rest";

    public override void Start()
    {
        var server = ServerController.Servers.FirstOrDefault(static x => x.Port == 443);
        if (server == null)
            return;

        if (server.Server is not IHttpServer http)
            return;

        http.DoReturn404IfFail = true;

        http.HTTPAttributeToMethods.Merge(Assembly.GetExecutingAssembly());

        if (server.Server is IWSServer ws)
            ws.WSAttributeToMethods.Merge(Assembly.GetExecutingAssembly());
    }

    public override void Stop()
    {
    }
}
