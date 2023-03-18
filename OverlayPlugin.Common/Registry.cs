using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin
{
    public class Registry
    {
        private readonly TinyIoCContainer _container;
        private readonly List<Type> _overlays;
        private readonly List<IEventSource> _eventSources;
        private readonly List<IOverlayPreset> _overlayPresets;

        public IEnumerable<Type> Overlays => _overlays;

        public IEnumerable<IEventSource> EventSources => _eventSources;

        public IReadOnlyList<IOverlayPreset> OverlayPresets => _overlayPresets;

        public event EventHandler<EventSourceRegisteredEventArgs> EventSourceRegistered;
        public event EventHandler EventSourcesStarted;

        public Registry(TinyIoCContainer container)
        {
            _container = container;
            _overlays = new List<Type>();
            _eventSources = new List<IEventSource>();
            _overlayPresets = new List<IOverlayPreset>();
        }

        public void RegisterOverlay<T>()
            where T : class, IOverlay
        {
            _overlays.Add(typeof(T));
            _container.Register<T>();
        }

        public void UnregisterOverlay<T>()
            where T : class, IOverlay
        {
            _overlays.Remove(typeof(T));
        }

        public void StartEventSource<T>(T source)
            where T : class, IEventSource
        {
            _container.BuildUp(source);
            _eventSources.Add(source);
            _container.Register(source);

            source.LoadConfig(_container.Resolve<IPluginConfig>());
            source.Start();

            EventSourceRegistered?.Invoke(null, new EventSourceRegisteredEventArgs(source));
        }

        [Obsolete("Please call StartEventSource() on the Registry object instead.")]
        public static void RegisterEventSource<T>()
            where T : class, IEventSource
        {
            var container = GetContainer();
            var logger = container.Resolve<ILogger>();
            var obj = (T)typeof(T).GetConstructor(new Type[] { typeof(ILogger) })?.Invoke(new object[] { logger });
            container.Resolve<Registry>().StartEventSource(obj);
        }

        public void RegisterOverlayPreset2(IOverlayPreset preset)
        {
            _overlayPresets.Add(preset);
        }

        [Obsolete("Please call RegisterOverlayPreset2() on the Registry object instead.")]
        public static void RegisterOverlayPreset(IOverlayPreset preset)
        {
            GetContainer().Resolve<Registry>().RegisterOverlayPreset2(preset);
        }

        public void StartEventSources()
        {
            if (EventSourcesStarted == null)
                return;
            EventSourcesStarted(null, null);
        }

        // For backwards compat only!!
        public static TinyIoCContainer GetContainer()
        {
            return (TinyIoCContainer)ActGlobals.oFormActMain.OverlayPluginContainer;
        }
    }

    public class EventSourceRegisteredEventArgs : EventArgs
    {
        public IEventSource EventSource { get; private set; }

        public EventSourceRegisteredEventArgs(IEventSource eventSource)
        {
            this.EventSource = eventSource;
        }
    }
}
