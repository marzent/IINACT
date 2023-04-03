using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tts
{
    public class Voice
    {
        public string Value { get; }

        public string DisplayName { get; }

        public Voice(string value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return $"{nameof(Value)}: {Value}, {nameof(DisplayName)}: {DisplayName}";
        }
    }
}
