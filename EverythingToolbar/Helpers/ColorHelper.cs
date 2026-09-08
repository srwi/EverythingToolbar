using System;
using System.Windows.Media;

namespace EverythingToolbar.Helpers
{
    public static class ColorHelper
    {
        public const int DefaultAlpha = 0xDA;
        public const int DefaultBrightness = 0x25;
        public static readonly Color DefaultSearchWindowColor = Color.FromArgb(DefaultAlpha, DefaultBrightness, DefaultBrightness, DefaultBrightness);

        public static Color GetSearchWindowColor(int alpha, int brightness, bool isLight)
        {
            byte clampedAlpha = (byte)Math.Clamp(alpha, 0, 255);
            int clampedBrightness = Math.Clamp(brightness, 0, 100);
            byte gray = (byte)(isLight ? 255 - clampedBrightness : clampedBrightness);
            return Color.FromArgb(clampedAlpha, gray, gray, gray);
        }

        public static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        public static SolidColorBrush ToFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
