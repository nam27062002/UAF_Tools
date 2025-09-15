using System;
using System.Windows.Input;
using System.Xml.Linq;
using System.Linq;
using DANCustomTools.Core.ViewModels;
using DANCustomTools.MVVM;
using DANCustomTools.Models.PropertiesEditor;
using DANCustomTools.Models.SceneExplorer;
using DANCustomTools.Services;
using System.Threading;
using System.Threading.Tasks;
namespace DANCustomTools.ViewModels;

public class ActorCreateViewModel : SubToolViewModelBase
{
    public override string SubToolName => "Actor Create";
    private readonly IActorCreateService _actorCreateService;

    public ActorCreateViewModel(ILogService logService, IActorCreateService actorCreateService) : base(logService)
    {
        _actorCreateService = actorCreateService;
        _ = StartServicesAsync(["--port", "12345"]);
    }

    private async Task StartServicesAsync(string[] arguments)
    {
        try
        {
            await Task.WhenAll(
                _actorCreateService.StartAsync(arguments)
            );
            LogService.Info("All services started successfully");
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to start one or more services", ex);
        }
    }

    protected override void SubscribeToConnectionEvents()
    {
        throw new NotImplementedException();
    }

    protected override void UnsubscribeFromConnectionEvents()
    {
        throw new NotImplementedException();
    }
}
