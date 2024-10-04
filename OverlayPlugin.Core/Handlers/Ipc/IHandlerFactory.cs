using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers.Ipc;

internal interface IHandlerFactory
{
    IHandler Create(string name, TinyIoCContainer container)
    {
        var pluginInterface = container.Resolve<IDalamudPluginInterface>();
        var receiver = pluginInterface.GetIpcProvider<JObject, bool>($"IINACT.IpcProvider.{name}");
        var sender = pluginInterface.GetIpcSubscriber<JObject, bool>(name);
        var logger = container.Resolve<ILogger>();
        var dispatcher = container.Resolve<EventDispatcher>();
        var repository = container.Resolve<FFXIVRepository>();
        return CreateHandler(name, receiver, sender, logger, dispatcher, repository);
    }

    protected IHandler CreateHandler(
        string name, ICallGateProvider<JObject, bool> receiver, ICallGateSubscriber<JObject, bool> sender,
        ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository);
}

internal class HandlerFactory : IHandlerFactory
{
    public IHandler CreateHandler(
        string name, ICallGateProvider<JObject, bool> receiver, ICallGateSubscriber<JObject, bool> sender,
        ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository) =>
        new IpcHandler(name, receiver, sender, logger, eventDispatcher);
}

internal class LegacyHandlerFactory : IHandlerFactory
{
    public IHandler CreateHandler(
        string name, ICallGateProvider<JObject, bool> receiver, ICallGateSubscriber<JObject, bool> sender,
        ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository) =>
        new LegacyIpcHandler(name, receiver, sender, logger, eventDispatcher, repository);
}
