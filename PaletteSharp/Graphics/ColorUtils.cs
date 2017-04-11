using System;
using System.Drawing;
using PaletteSharp.Helpers;

namespace PaletteSharp.Graphics
{
    internal class ColorUtils
    {
        private static int MIN_ALPHA_SEARCH_MAX_ITERATIONS = 10;
        private static int MIN_ALPHA_SEARCH_PRECISION = 10;

        internal static Color CompositeColors(Color foreground, Color background)
        {
            byte bgAlpha = background.A;
            byte fgAlpha = foreground.A;
            int a = CompositeAlpha(fgAlpha, bgAlpha);
            int r = CompositeComponent(foreground.R, fgAlpha, background.R, bgAlpha, a);
            int g = CompositeComponent(foreground.G, fgAlpha, background.G, bgAlpha, a);
            int b = CompositeComponent(foreground.B, fgAlpha, background.B, bgAlpha, a);
            return Color.FromArgb(a, r, g, b);
        }
        private static int CompositeAlpha(byte foregroundAlpha, byte backgroundAlpha)
        {
            return 0xFF - (0xFF - backgroundAlpha) * (0xFF - foregroundAlpha) / 0xFF;
        }
        private static int CompositeComponent(byte fgC, byte fgA, byte bgC, byte bgA, int a)
        {
            if (a == 0) return 0;
            return (0xFF * fgC * fgA + bgC * bgA * (0xFF - fgA)) / (a * 0xFF);
        }

        /**
         * Calculates the minimum alpha value which can be applied to {@code foreground} so that would
         * have a contrast value of at least {@code minContrastRatio} when compared to
         * {@code background}.
         *
         * @param foreground       the foreground color.
         * @param background       the background color. Should be opaque.
         * @param minContrastRatio the minimum contrast ratio.
         * @return the alpha value in the range 0-255, or -1 if no value could be calculated.
         */
        internal static int CalculateMinimumAlpha(Color foreground, Color background, float minContrastRatio)
        {
            if (background.A != 255)
            {
                throw new ArgumentException("background can not be translucent");
            }
            // First lets check that a fully opaque foreground has sufficient contrast
            Color testForeground = SetAlphaComponent(foreground, 255);
            double testRatio = CalculateContrast(testForeground, background);
            if (testRatio < minContrastRatio)
            {
                // Fully opaque foreground does not have sufficient contrast, return error
                return -1;
            }
            // Binary search to find a value with the minimum value which provides sufficient contrast
            int numIterations = 0;
            int minAlpha = 0;
            int maxAlpha = 255;
            while (numIterations <= MIN_ALPHA_SEARCH_MAX_ITERATIONS &&
                   maxAlpha - minAlpha > MIN_ALPHA_SEARCH_PRECISION)
            {
                int testAlpha = (minAlpha + maxAlpha) / 2;
                testForeground = SetAlphaComponent(foreground, testAlpha);
                testRatio = CalculateContrast(testForeground, background);
                if (testRatio < minContrastRatio)
                {
                    minAlpha = testAlpha;
                }
                else
                {
                    maxAlpha = testAlpha;
                }
                numIterations++;
            }
            // Conservatively return the max of the range of possible alphas, which is known to pass.
            return maxAlpha;
        }

        /**
         * Returns the luminance of a color.
         *
         * Formula defined here: http://www.w3.org/TR/2008/REC-WCAG20-20081211/#relativeluminancedef
         */
        internal static double CalculateLuminance(Color color)
        {
            double red = color.R / 255d;
            red = red < 0.03928 ? red / 12.92 : Math.Pow((red + 0.055) / 1.055, 2.4);
            double green = color.G / 255d;
            green = green < 0.03928 ? green / 12.92 : Math.Pow((green + 0.055) / 1.055, 2.4);
            double blue = color.B / 255d;
            blue = blue < 0.03928 ? blue / 12.92 : Math.Pow((blue + 0.055) / 1.055, 2.4);
            return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
        }

        /**
         * Returns the contrast ratio between {@code foreground} and {@code background}.
         * {@code background} must be opaque.
         * <p>
         * Formula defined
         * <a href="http://www.w3.org/TR/2008/REC-WCAG20-20081211/#contrast-ratiodef">here</a>.
         */
        internal static double CalculateContrast(Color foreground, Color background)
        {
            if (background.A != 255)
            {
                throw new ArgumentException("background can not be translucent");
            }
            if (foreground.A < 255)
            {
                // If the foreground is translucent, composite the foreground over the background
                foreground = CompositeColors(foreground, background);
            }
            double luminance1 = CalculateLuminance(foreground) + 0.05;
            double luminance2 = CalculateLuminance(background) + 0.05;
            // Now return the lighter luminance divided by the darker luminance
            return Math.Max(luminance1, luminance2) / Math.Min(luminance1, luminance2);
        }

        /**
 * Convert RGB components to HSL (hue-saturation-lightness).
 * <ul>
 * <li>hsl[0] is Hue [0 .. 360)</li>
 * <li>hsl[1] is Saturation [0...1]</li>
 * <li>hsl[2] is Lightness [0...1]</li>
 * </ul>
 *
 * @param r   red component value [0..255]
 * @param g   green component value [0..255]
 * @param b   blue component value [0..255]
 * @param hsl 3 element array which holds the resulting HSL components.
 */
        internal static void RGBToHSL(int r, int g, int b, float[] hsl)
        {
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float deltaMaxMin = max - min;
            float h, s;
            float l = (max + min) / 2f;
            if (max == min)
            {
                // Monochromatic
                h = s = 0f;
            }
            else
            {
                if (max == rf)
                {
                    h = (gf - bf) / deltaMaxMin % 6f;
                }
                else if (max == gf)
                {
                    h = (bf - rf) / deltaMaxMin + 2f;
                }
                else
                {
                    h = (rf - gf) / deltaMaxMin + 4f;
                }
                s = deltaMaxMin / (1f - Math.Abs(2f * l - 1f));
            }
            hsl[0] = h * 60f % 360f;
            hsl[1] = s;
            hsl[2] = l;
        }

        /**
         * Convert the ARGB color to its HSL (hue-saturation-lightness) components.
         * <ul>
         * <li>hsl[0] is Hue [0 .. 360)</li>
         * <li>hsl[1] is Saturation [0...1]</li>
         * <li>hsl[2] is Lightness [0...1]</li>
         * </ul>
         *
         * @param color the ARGB color to convert. The alpha component is ignored.
         * @param hsl 3 element array which holds the resulting HSL components.
         */
        internal static void ColorToHSL(Color color, float[] hsl)
        {
            RGBToHSL(color.R, color.G, color.B, hsl);
        }

        /**
         * Convert HSL (hue-saturation-lightness) components to a RGB color.
         * <ul>
         * <li>hsl[0] is Hue [0 .. 360)</li>
         * <li>hsl[1] is Saturation [0...1]</li>
         * <li>hsl[2] is Lightness [0...1]</li>
         * </ul>
         * If hsv values are out of range, they are pinned.
         *
         * @param hsl 3 element array which holds the input HSL components.
         * @return the resulting RGB color
         */
        internal static Color HSLToColor(float[] hsl)
        {
            float h = hsl[0];
            float s = hsl[1];
            float l = hsl[2];
            float c = (1f - Math.Abs(2 * l - 1f)) * s;
            float m = l - 0.5f * c;
            float x = c * (1f - Math.Abs(h / 60f % 2f - 1f));
            int hueSegment = (int)h / 60;
            int r = 0, g = 0, b = 0;
            switch (hueSegment)
            {
                case 0:
                    r = (int)Math.Round(255 * (c + m), 0);
                    g = (int)Math.Round(255 * (x + m), 0);
                    b = (int)Math.Round(255 * m, 0);
                    break;
                case 1:
                    r = (int)Math.Round(255 * (x + m), 0);
                    g = (int)Math.Round(255 * (c + m), 0);
                    b = (int)Math.Round(255 * m, 0);
                    break;
                case 2:
                    r = (int)Math.Round(255 * m, 0);
                    g = (int)Math.Round(255 * (c + m), 0);
                    b = (int)Math.Round(255 * (x + m), 0);
                    break;
                case 3:
                    r = (int)Math.Round(255 * m, 0);
                    g = (int)Math.Round(255 * (x + m), 0);
                    b = (int)Math.Round(255 * (c + m), 0);
                    break;
                case 4:
                    r = (int)Math.Round(255 * (x + m), 0);
                    g = (int)Math.Round(255 * m, 0);
                    b = (int)Math.Round(255 * (c + m), 0);
                    break;
                case 5:
                case 6:
                    r = (int)Math.Round(255 * (c + m), 0);
                    g = (int)Math.Round(255 * m, 0);
                    b = (int)Math.Round(255 * (x + m), 0);
                    break;
            }
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            return Color.FromArgb(r, g, b);
        }

        /**
         * Set the alpha component of {@code color} to be {@code alpha}.
         */
        internal static Color SetAlphaComponent(Color color, int alpha)
        {
            if (alpha < 0 || alpha > 255)
            {
                throw new ArgumentException("alpha must be between 0 and 255.");
            }
            return ((color.ToInt() & 0x00ffffff) | (alpha << 24)).ToColor();
        }
    }
}
