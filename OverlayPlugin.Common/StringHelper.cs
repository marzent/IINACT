using System.Globalization;

namespace RainbowMage.OverlayPlugin {
    public static class StringHelper {
        public static string ToProperCase(this string @this) {
            var text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(@this);
            if (text.EndsWith(" i (*)")) {
                text = text[..^6] + " I (*)";
            } else if (text.EndsWith(" Ii")) {
                text = text[..^3] + " II";
            } else if (text.EndsWith(" Ii (*)")) {
                text = text[..^7] + " II (*)";
            } else if (text.EndsWith(" Iii")) {
                text = text[..^4] + " III";
            } else if (text.EndsWith(" Iv")) {
                text = text[..^3] + " IV";
            } else if (text.EndsWith(" V")) {
                text = text[..^2] + " V";
            }
            return text;
        }
    }
}
