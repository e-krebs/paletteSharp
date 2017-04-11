using System.Drawing;

namespace PaletteSharp.Filters
{
    /// <summary>
    /// A Filter provides a mechanism for exercising fine-grained control over which colors
    /// are valid within a resulting Palette
    /// </summary>
    internal interface IFilter
    {
        /// <summary>
        /// Hook to allow clients to be able filter colors from resulting palette
        /// </summary>
        /// <param name="rgb">the color in RGB888</param>
        /// <param name="hsl">HSL representation of the color</param>
        /// <returns>true if the color is allowed, false if not</returns>
        bool IsAllowed(Color rgb, float[] hsl);
    }
}