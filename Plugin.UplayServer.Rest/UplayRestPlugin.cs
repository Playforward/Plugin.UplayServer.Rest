using ModdableWebServer.Interfaces;
using ServerShared.Controllers;
using ServerShared.Plugins;
using System.Reflection;

namespace Plugin.UplayServer.Rest;

public class UplayRestPlugin : ServerPlugin<UplayRestSettings>
{
    public override uint Priority => 0;

    public override string Name => "UplayServer.Rest";

    public override void Start()
    {
        if (!string.IsNullOrWhiteSpace(Config.AppId)
            && !Config.Titles.Any(t => string.Equals(t.AppId, Config.AppId, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Information("[rest] migrating legacy single-title config (AppId {AppId}) into Titles", Config.AppId);
            Config.Titles.Add(new TitleSettings
            {
                Name = "migrated",
                AppId = Config.AppId!,
                SpaceId = Config.SpaceId ?? "",
            });
            Config.AppId = null;
            Config.SpaceId = null;
            SaveConfig();
        }

        TitleRegistry.Publish(Config.Titles);
        foreach (var t in TitleRegistry.All)
            Log.Information("[rest] serving {Name}: appId={AppId} spaceId={SpaceId} resources={Res}",
                t.Name, t.AppId, t.SpaceId,
                string.IsNullOrWhiteSpace(t.ResourceDir) ? "(built-in, retargeted)" : t.ResourceDir);

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
