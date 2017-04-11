using System.Drawing;

namespace PaletteSharp.Filters
{
    /// <summary>
    /// default implementation for IFilter
    /// </summary>
    internal class DefaultFilter : IFilter
    {

        private const float BlackMaxLightness = 0.05f;
        private const float WhiteMinLightness = 0.95f;

        /// <inheritdoc />
        public bool IsAllowed(Color rgb, float[] hsl)
        {
            return !IsWhite(hsl) && !IsBlack(hsl) && !IsNearRedILine(hsl);
        }
        ///**
        // * @return true if the color represents a color which is close to black.
        // */

        private static bool IsBlack(float[] hslColor)
        {
            return hslColor[2] <= BlackMaxLightness;
        }
        /**
         * @return true if the color represents a color which is close to white.
         */
        private static bool IsWhite(float[] hslColor)
        {
            return hslColor[2] >= WhiteMinLightness;
        }
        /**
         * @return true if the color lies close to the red side of the I line.
         */
        private static bool IsNearRedILine(float[] hslColor)
        {
            return hslColor[0] >= 10f && hslColor[0] <= 37f && hslColor[1] <= 0.82f;
        }
    }
}