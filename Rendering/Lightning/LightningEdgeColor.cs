using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Core.Display;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.StrawberryJam.Rendering.Lightning
{
    internal static class LightningEdgeColor
    {
        private static readonly Dictionary<string, PackedColor> named = CreateNames();

        private static Dictionary<string, PackedColor> CreateNames()
        {
            var result = new Dictionary<string, PackedColor>();
            foreach (var p in typeof(PackedColor).GetProperties())
                if (p.PropertyType == typeof(PackedColor) && !result.ContainsKey(p.Name))
                    result[p.Name] = (PackedColor)p.GetValue(null);
            return result;
        }

        public static PackedColor Parse(string hex)
        {
            if (named.ContainsKey(hex)) return named[hex];
            string text = hex.Trim('#');
            if (text.StartsWith("0x")) text = text.Substring(2);
            if (text.Length == 6 &&
                int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result))
                return SourceHexColor.HexToColor(result);
            if (text.Length == 8 && text.Substring(0, 2) == "00" && int.TryParse(text.Substring(2),
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) return PackedColor.Transparent;
            if (int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
            {
                var color = default(PackedColor);
                color.A = (byte)(result >> 24);
                color.R = (byte)(result >> 16);
                color.G = (byte)(result >> 8);
                color.B = (byte)result;
                return color;
            }

            return PackedColor.Transparent;
        }
    }
}
