using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RainbowMage.OverlayPlugin
{
    public abstract class EventSourceBase : IEventSource
    {
        public string Name { get; protected set; }
        protected TinyIoCContainer container;
        private EventDispatcher dispatcher;
        private bool updateRunning = false;

        protected Timer timer;
        protected ILogger logger;
        protected Dictionary<string, JObject> eventCache = new Dictionary<string, JObject>();

        // Backwards compat
        public EventSourceBase(ILogger _)
        {
            Init(Registry.GetContainer());
        }

        public EventSourceBase(TinyIoCContainer c)
        {
            Init(c);
        }

        private void Init(TinyIoCContainer c)
        {
            container = c;
            logger = container.Resolve<ILogger>();
            dispatcher = container.Resolve<EventDispatcher>();

            timer = new Timer(UpdateWrapper, null, Timeout.Infinite, 1000);
        }

        protected void UpdateWrapper(object state)
        {
            if (updateRunning)
            {
                Log(LogLevel.Warning, "Update for {0} took too long, skipping overlapping tick!", this.GetType().Name);
                return;
            }

            try
            {
                updateRunning = true;
                Update();
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Update: {0}", ex);
            } finally
            {
                updateRunning = false;
            }
        }

        public abstract void LoadConfig(IPluginConfig config);

        public abstract void SaveConfig(IPluginConfig config);

        protected void Log(LogLevel level, string message, params object[] args)
        {
            logger.Log(level, message, args);
        }

        public virtual void Dispose()
        {
            timer?.Dispose();
        }

        public virtual void Start()
        {
            timer.Change(0, 1000);
        }

        public virtual void Stop()
        {
            timer.Change(-1, -1);
        }

        abstract protected void Update();

        protected void RegisterEventTypes(List<string> types)
        {
            dispatcher.RegisterEventTypes(types);
        }

        protected void RegisterEventType(string type)
        {
            dispatcher.RegisterEventType(type);
        }

        protected void RegisterEventType(string type, Func<JObject> initCallback)
        {
            dispatcher.RegisterEventType(type, initCallback);
        }

        protected void RegisterCachedEventTypes(List<string> types)
        {
            foreach (var type in types)
            {
                RegisterCachedEventType(type);
            }
        }

        protected void RegisterCachedEventType(string type)
        {
            eventCache[type] = null;
            dispatcher.RegisterEventType(type, () => eventCache[type]);
        }

        protected void RegisterEventHandler(string name, Func<JObject, JToken> handler)
        {
            dispatcher.RegisterHandler(name, handler);
        }

        protected void DispatchEvent(JObject e)
        {
            dispatcher.DispatchEvent(e);
        }

        protected void DispatchAndCacheEvent(JObject e)
        {
            eventCache[e["type"].ToString()] = e;
            dispatcher.DispatchEvent(e);
        }

        protected bool HasSubscriber(string eventName)
        {
            return dispatcher.HasSubscriber(eventName);
        }
    }
}
