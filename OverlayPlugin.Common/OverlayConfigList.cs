using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// XmlSerializer でシリアライズ可能な IOverlayConfig のコレクション。
    /// </summary>
    [Serializable]
    public class OverlayConfigList<T> : Collection<T>, IXmlSerializable
    {
        public int MissingTypes { get; private set; }

        [NonSerialized]
        private ILogger _logger;

        public OverlayConfigList(ILogger logger)
        {
            _logger = logger;
        }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            MissingTypes = 0;

            if (reader.IsEmptyElement)
            {
                return;
            }

            reader.ReadToDescendant("Overlay");
            do
            {
                var typeName = reader.GetAttribute("Type");

                reader.Read();

                var type = GetType(typeName);

                if (type != null)
                {
                    try
                    {
                        var serializer = new XmlSerializer(type);
                        var config = (T)serializer.Deserialize(reader);
                        this.Add(config);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine(e);
                        _logger.Log(LogLevel.Error, e.ToString());
                    }
                }
                else
                {
                    reader.Skip();
                    MissingTypes++;
                }
            }
            while (reader.ReadToNextSibling("Overlay"));

            reader.ReadEndElement();
        }

        private Type GetType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
                                                          .Select(asm => asm.GetType(fullName, false))
                                                          .FirstOrDefault(type => type != null);

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (var config in this)
            {
                writer.WriteStartElement("Overlay");
                writer.WriteAttributeString("Type", config.GetType().FullName);
                var serializer = new XmlSerializer(config.GetType());
                serializer.Serialize(writer, config);
                writer.WriteEndElement();
            }
        }
    }
}
