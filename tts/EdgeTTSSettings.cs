using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace tts
{
    public class EdgeTTSSettings
    {

        public int Speed = 100;

        public int Pitch = 100;


        public int Volume = 100;


        public string Voice = "";


        public bool Accept = false;

        public override string ToString()
        {
            return $"{nameof(Speed)}: {Speed}, {nameof(Pitch)}: {Pitch}, {nameof(Volume)}: {Volume}, {nameof(Voice)}: {Voice}";
        }
    }
}
