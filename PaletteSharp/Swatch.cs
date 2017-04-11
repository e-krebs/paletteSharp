using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using PaletteSharp.Graphics;
using PaletteSharp.Helpers;

namespace PaletteSharp
{
    /// <summary>
    /// Represents a color swatch generated from an image's palette. The RGB color can be retrieved
    /// by calling getRgb().
    /// </summary>
    public class Swatch
    {
        private int Red => _rgb.R;
        private int Green => _rgb.G;
        private int Blue => _rgb.B;
        private readonly Color _rgb;
        private readonly int _population;
        private bool _generatedTextColors;
        private Color _titleTextColor;
        private Color _bodyTextColor;
        private float[] _hsl;

        public Swatch(Color color, int population)
        {
            _rgb = color;
            _population = population;
        }
        public Swatch(int red, int green, int blue, int population)
        {
            _rgb = Color.FromArgb(red, green, blue);
            _population = population;
        }

        public Swatch(float[] hsl, int population) : this(ColorUtils.HSLToColor(hsl), population)
        {
            _hsl = hsl;
        }

        /// <summary>
        /// return this swatch's ARGB color value
        /// </summary>
        public Color GetArgb() => _rgb;

        ///**
        // * Return this swatch's HSL values.
        // *     hsv[0] is Hue [0 .. 360)
        // *     hsv[1] is Saturation [0...1]
        // *     hsv[2] is Lightness [0...1]
        // */
        public float[] GetHsl()
        {
            if (_hsl == null)
            {
                _hsl = new float[3];
            }
            ColorUtils.RGBToHSL(Red, Green, Blue, _hsl);
            return _hsl;
        }

        /**
         * @return the number of pixels represented by this swatch
         */
        public int GetPopulation() => _population;

        /**
         * Returns an appropriate color to use for any 'title' text which is displayed over this
         * {@link Swatch}'s color. This color is guaranteed to have sufficient contrast.
         */
        public Color GetTitleTextColor()
        {
            EnsureTextColorsGenerated();
            return _titleTextColor;
        }
        /**
         * Returns an appropriate color to use for any 'body' text which is displayed over this
         * {@link Swatch}'s color. This color is guaranteed to have sufficient contrast.
         */
        public Color GetBodyTextColor()
        {
            EnsureTextColorsGenerated();
            return _bodyTextColor;
        }
        private void EnsureTextColorsGenerated()
        {
            if (!_generatedTextColors)
            {
                // First check white, as most colors will be dark
                int lightBodyAlpha = ColorUtils.CalculateMinimumAlpha(Color.White, _rgb, Palette.MinContrastBodyText);
                int lightTitleAlpha = ColorUtils.CalculateMinimumAlpha(Color.White, _rgb, Palette.MinContrastTitleText);
                if (lightBodyAlpha != -1 && lightTitleAlpha != -1)
                {
                    // If we found valid light values, use them and return
                    _bodyTextColor = ColorUtils.SetAlphaComponent(Color.White, lightBodyAlpha);
                    _titleTextColor = ColorUtils.SetAlphaComponent(Color.White, lightTitleAlpha);
                    _generatedTextColors = true;
                    return;
                }
                int darkBodyAlpha = ColorUtils.CalculateMinimumAlpha(Color.Black, _rgb, Palette.MinContrastBodyText);
                int darkTitleAlpha = ColorUtils.CalculateMinimumAlpha(Color.Black, _rgb, Palette.MinContrastTitleText);
                if (darkBodyAlpha != -1 && darkBodyAlpha != -1)
                {
                    // If we found valid dark values, use them and return
                    _bodyTextColor = ColorUtils.SetAlphaComponent(Color.Black, darkBodyAlpha);
                    _titleTextColor = ColorUtils.SetAlphaComponent(Color.Black, darkTitleAlpha);
                    _generatedTextColors = true;
                    return;
                }
                // If we reach here then we can not find title and body values which use the same
                // lightness, we need to use mismatched values
                _bodyTextColor = lightBodyAlpha != -1
                    ? ColorUtils.SetAlphaComponent(Color.White, lightBodyAlpha)
                    : ColorUtils.SetAlphaComponent(Color.Black, darkBodyAlpha);
                _titleTextColor = lightTitleAlpha != -1
                    ? ColorUtils.SetAlphaComponent(Color.White, lightTitleAlpha)
                    : ColorUtils.SetAlphaComponent(Color.Black, darkTitleAlpha);
                _generatedTextColors = true;
            }
        }

        public override string ToString()
        {
            return new StringBuilder(GetType().Name)
                .Append($" [RGB: {GetArgb().ToHexString()}]")
                .Append($" [HSL: {GetHsl().Select(x => x.ToString(CultureInfo.InvariantCulture)).Aggregate((x, y) => $"{x}, {y}")}]")
                .Append($" [Population: {_population}]")
                .Append($" [Title Text: {GetTitleTextColor().ToHexString()}]")
                .Append($" [Body Text: {GetBodyTextColor().ToHexString()}]")
                .ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }
            Swatch swatch = (Swatch)o;
            return _population == swatch._population && _rgb == swatch._rgb;
        }

        public override int GetHashCode()
        {
            return 31 * _rgb.ToInt() + _population;
        }
    }
}
