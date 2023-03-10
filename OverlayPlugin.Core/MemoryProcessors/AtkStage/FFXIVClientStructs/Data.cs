using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage.FFXIVClientStructs
{
    public enum DataNamespace
    {
        Global
    }

    public class Data
    {
        private readonly ILogger logger;
        private readonly Dictionary<DataNamespace, ClientStructsData> data = new Dictionary<DataNamespace, ClientStructsData>();

        // @TODO: Is there some way to get this from the module instead?
        private const long DataBaseOffset = 0x140000000;
        
        public Data(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();

            var main = container.Resolve<PluginMain>();
            var pluginDirectory = main.PluginDirectory;
        }

        // @TODO: At some point the runtime checks in this function should be moved to a compile-time test instead
        // and this function can return `long` instead of `long?`
        public long? GetClassInstanceAddress(DataNamespace ns, string targetClass)
        {
            var curObj = GetBaseObject(ns);
            if (curObj == null)
            {
                return null;
            }

            GameClass classObj;

            if (!curObj.classes.TryGetValue(targetClass, out classObj))
            {
                return null;
            }

            var instances = classObj.instances;
            if (instances == null || instances.Length < 1)
            {
                return null;
            }

            return instances[0].ea - DataBaseOffset;
        }

        public ClientStructsData GetBaseObject(DataNamespace ns)
        {
            ClientStructsData baseObj;
            if (!data.TryGetValue(ns, out baseObj))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("data.yml"));
                string dataYaml;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream)) {
                    dataYaml = reader.ReadToEnd();
                }
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                baseObj = deserializer.Deserialize<ClientStructsData>(dataYaml);
                data[ns] = baseObj;
            }
            return baseObj;
        }

        public class ClientStructsData
        {
            public string version;
            public Dictionary<long, string> globals = new Dictionary<long, string>();
            public Dictionary<long, string> functions = new Dictionary<long, string>();
            public Dictionary<string, GameClass> classes = new Dictionary<string, GameClass>();
        }

        public class GameClass
        {
            public ClassVtbl[] vtbls;
            public Dictionary<long, string> funcs = new Dictionary<long, string>();
            public Dictionary<long, string> vfuncs = new Dictionary<long, string>();
            public ClassInstance[] instances;
        }

        public class ClassVtbl
        {
            public long ea;
            public string @base;
        }

        public class ClassInstance
        {
            public long ea;
            public bool? pointer;
            public string name;
        }
    }
}
