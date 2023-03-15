using Newtonsoft.Json.Converters;
using System;

namespace RainbowMage.OverlayPlugin
{
    internal class ConfigCreationConverter : CustomCreationConverter<IOverlayConfig>
    {
        private TinyIoCContainer _container;

        public ConfigCreationConverter(TinyIoCContainer container) : base()
        {
            _container = container;
        }

        public override IOverlayConfig Create(Type objectType)
        {
            var construct = objectType.GetConstructor(new Type[] { typeof(TinyIoCContainer), typeof(string) });
            if (construct == null)
            {
                construct = objectType.GetConstructor(new Type[] { typeof(string) });
                if (construct == null)
                {
                    throw new Exception("No valid constructor found for config type " + objectType.ToString() + "!");
                }

                return (IOverlayConfig)construct.Invoke(new object[] { null });
            }
            else
            {
                return (IOverlayConfig)construct.Invoke(new object[] { _container, null });
            }
        }
    }
}
