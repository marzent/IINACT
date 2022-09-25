using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainbowMage.OverlayPlugin {
    internal class EventDispatcher {
        private ILogger _logger;
        private Dictionary<string, Func<JObject, JToken>> handlers;
        private Dictionary<string, List<IEventReceiver>> eventFilter;
        private Dictionary<string, Func<JObject>> stateCallbacks;

        public EventDispatcher(TinyIoCContainer container) {
            _logger = container.Resolve<ILogger>();

            handlers = new Dictionary<string, Func<JObject, JToken>>();
            eventFilter = new Dictionary<string, List<IEventReceiver>>();
            stateCallbacks = new Dictionary<string, Func<JObject>>();
        }

        private void Log(LogLevel level, string message, params object[] args) {
            _logger.Log(level, string.Format(message, args));
        }

        public void RegisterHandler(string name, Func<JObject, JToken> handler) {
            if (handlers.ContainsKey(name)) {
                throw new Exception(string.Format(Resources.DuplicateHandlerError, name));
            }

            handlers[name] = handler;
        }

        public void RegisterEventTypes(List<string> names) {
            foreach (var name in names) {
                eventFilter[name] = new List<IEventReceiver>();
            }
        }

        public void RegisterEventType(string name) {
            RegisterEventType(name, null);
        }

        public void RegisterEventType(string name, Func<JObject> initCallback) {
            eventFilter[name] = new List<IEventReceiver>();

            if (initCallback != null)
                stateCallbacks[name] = initCallback;
        }

        public void Subscribe(string eventName, IEventReceiver receiver) {
            if (!eventFilter.ContainsKey(eventName)) {
                Log(LogLevel.Error, Resources.MissingEventSubError, eventName);
                return;
            }

            if (stateCallbacks.ContainsKey(eventName)) {
                var ev = stateCallbacks[eventName]();
                if (ev != null) receiver.HandleEvent(ev);
            }

            lock (eventFilter[eventName]) {
                if (!eventFilter[eventName].Contains(receiver)) {
                    eventFilter[eventName].Add(receiver);
                }
            }
        }

        public void Unsubscribe(string eventName, IEventReceiver receiver) {
            if (eventFilter.ContainsKey(eventName)) {
                lock (eventFilter[eventName]) {
                    eventFilter[eventName].Remove(receiver);
                }
            }
        }

        public void UnsubscribeAll(IEventReceiver receiver) {
            foreach (var item in eventFilter.Values) {
                lock (item) {
                    if (item.Contains(receiver)) item.Remove(receiver);
                }
            }
        }

        // Can be used to check that an event will be delivered before building
        // an expensive JObject that would otherwise be thrown away.
        public bool HasSubscriber(string eventName) {
            if (!eventFilter.ContainsKey(eventName))
                return false;
            lock (eventFilter[eventName]) {
                return eventFilter[eventName].Count > 0;
            }
        }

        public void DispatchEvent(JObject e) {
            var eventType = e["type"].ToString();
            if (!eventFilter.ContainsKey(eventType)) {
                throw new Exception(string.Format(Resources.MissingEventDispatchError, eventType));
            }

            lock (eventFilter[eventType]) {
                foreach (var receiver in eventFilter[eventType]) {
                    try {
                        receiver.HandleEvent(e);
                    }
                    catch (Exception ex) {
                        Log(LogLevel.Error, Resources.EventHandlerException, eventType, receiver, ex);
                    }
                }
            }
        }

        public JToken CallHandler(JObject e) {
            var handlerName = e["call"].ToString();
            if (!handlers.ContainsKey(handlerName)) {
                throw new Exception(string.Format(Resources.MissingHandlerError, handlerName));
            }

            var result = handlers[handlerName](e);
            if (result != null && result.Type != JTokenType.Object) {
                throw new Exception("Handler response must be an object or null");
            }
            return result;
        }

        public JToken ProcessHandlerMessage(IEventReceiver receiver, string data) {
            try {
                var message = JObject.Parse(data);
                if (!message.ContainsKey("call")) {
                    _logger.Log(LogLevel.Error, Resources.OverlayApiInvalidHandlerCall, receiver.Name + ": " + data);
                    return null;
                }

                var handler = message["call"].ToString();
                if (handler == "subscribe") {
                    if (!message.ContainsKey("events")) {
                        _logger.Log(LogLevel.Error, Resources.OverlayApiMissingEventsField, receiver.Name + ": " + data);
                        return null;
                    }

                    foreach (var name in message["events"].ToList()) {
                        Subscribe(name.ToString(), receiver);
                        _logger.Log(LogLevel.Debug, Resources.OverlayApiSubscribed, receiver.Name, name.ToString());
                    }
                    return null;
                } else if (handler == "unsubscribe") {
                    if (!message.ContainsKey("events")) {
                        _logger.Log(LogLevel.Error, Resources.OverlayApiMissingEventsFieldUnsub, receiver.Name + ": " + data);
                        return null;
                    }

                    foreach (var name in message["events"].ToList()) {
                        Unsubscribe(name.ToString(), receiver);
                    }
                    return null;
                }

                return CallHandler(message);
            }
            catch (Exception e) {
                _logger.Log(LogLevel.Error, Resources.JsHandlerCallException, e);
                return null;
            }
        }
    }
}
