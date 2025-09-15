using DANCustomTools.Core.Services;

namespace DANCustomTools.Services;

public class ActorCreateService(ILogService logService, IEngineHostService engineHost) : EnginePluginServiceBase(logService, engineHost), IActorCreateService
{
    public override string PluginName => "ActorCreate_Plugin";

    protected override void ProcessMessage(blobWrapper blob)
    {
        var command = "";
        blob.extract(ref command);

        LogService.Info($"SceneExplorer received message: '{command}'");
    }
}
