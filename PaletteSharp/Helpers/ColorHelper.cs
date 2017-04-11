using System.Drawing;
using System.Globalization;
using System.Text;

namespace PaletteSharp.Helpers
{
    public static class ColorHelper
    {
        public static int ToInt(this Color color)
        {
            string stringColor = color.ToRgbHex();
            string r = stringColor.Substring(2, 2);
            string g = stringColor.Substring(4, 2);
            string b = stringColor.Substring(6, 2);
            return int.Parse(b + g + r, NumberStyles.HexNumber);
        }

        public static string ToHexString(this Color color, bool argb = true)
        {
            return $"#{color.ToRgbHex(argb)}";
        }

        private static string ToRgbHex(this Color color, bool argb = true)
        {
            StringBuilder sb = new StringBuilder(argb ? color.A.ToString("X2") : string.Empty);
            sb.Append(color.R.ToString("X2"));
            sb.Append(color.G.ToString("X2"));
            sb.Append(color.B.ToString("X2"));
            return sb.ToString();
        }

        public static Color ToColor(this int color)
        {
            return Color.FromArgb(255,
                byte.Parse(color.ToString("X8").Substring(6, 2), NumberStyles.HexNumber),
                byte.Parse(color.ToString("X8").Substring(4, 2), NumberStyles.HexNumber),
                byte.Parse(color.ToString("X8").Substring(2, 2), NumberStyles.HexNumber));
        }

        public static void GetPixels(this Bitmap bitmap, ref int[] pixels, int offset, int stride, int x, int y,
            int width, int height)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int pixel = bitmap.GetPixel(i + x, j + y).ToInt();
                    pixels[offset + i + stride * j] = pixel;
                }
            }
        }

        public static Bitmap CreateScaledBitmap(this Bitmap src, int dstWidth, int dstHeight)
        {
            return new Bitmap(src, dstWidth, dstHeight);
        }
    }
}
