using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaletteSharp.Graphics;
using PaletteSharp.Helpers;

namespace PaletteSharp
{

    public class Palette
    {
        /**
         * Listener to be used with {@link #generateAsync(Bitmap, PaletteAsyncListener)} or
         * {@link #generateAsync(Bitmap, int, PaletteAsyncListener)}
         */
        public interface IPaletteAsyncListener
        {
            /**
             * Called when the {@link Palette} has been generated.
             */
            void OnGenerated(Palette palette);
        }

        internal const int DefaultResizeBitmapArea = 112 * 112;
        internal const int DefaultCalculateNumberColors = 16;
        internal const float MinContrastTitleText = 3.0f;
        internal const float MinContrastBodyText = 4.5f;
        internal const string LogTag = "Palette";
        internal const bool LogTimings = false;

        /**
         * Start generating a {@link Palette} with the returned {@link Builder} instance.
         */
        public static PaletteBuilder From(Bitmap bitmap)
        {
            return new PaletteBuilder(bitmap);
        }
        /**
         * Generate a {@link Palette} from the pre-generated list of {@link Palette.Swatch} swatches.
         * This is useful for testing, or if you want to resurrect a {@link Palette} instance from a
         * list of swatches. Will return null if the {@code swatches} is null.
         */
        public static Palette From(List<Swatch> swatches)
        {
            return new PaletteBuilder(swatches).Generate();
        }

        private readonly List<Swatch> _swatches;
        private readonly List<Target> _targets;
        private readonly Dictionary<Target, Swatch> _selectedSwatches;
        private readonly SparseBooleanDictionary<Color> _usedColors;
        private readonly Swatch _dominantSwatch;
        internal Palette(List<Swatch> swatches, List<Target> targets)
        {
            _swatches = swatches;
            _targets = targets;
            _usedColors = new SparseBooleanDictionary<Color>();
            _selectedSwatches = new Dictionary<Target, Swatch>();
            _dominantSwatch = FindDominantSwatch();
        }

        #region get Swatches / Color
        /**
         * Returns all of the swatches which make up the palette.
         */
        public List<Swatch> GetSwatches()
        {
            return _swatches;
        }
        /**
         * Returns the targets used to generate this palette.
         */
        internal List<Target> GetTargets()
        {
            return _targets;
        }
        /**
         * Returns the most vibrant swatch in the palette. Might be null.
         *
         * @see Target#VIBRANT
         */
        public Swatch GetVibrantSwatch()
        {
            return GetSwatchForTarget(Target.Vibrant);
        }
        /**
         * Returns a light and vibrant swatch from the palette. Might be null.
         *
         * @see Target#LIGHT_VIBRANT
         */
        public Swatch GetLightVibrantSwatch()
        {
            return GetSwatchForTarget(Target.LightVibrant);
        }
        /**
         * Returns a dark and vibrant swatch from the palette. Might be null.
         *
         * @see Target#DARK_VIBRANT
         */
        public Swatch GetDarkVibrantSwatch()
        {
            return GetSwatchForTarget(Target.DarkVibrant);
        }
        /**
         * Returns a muted swatch from the palette. Might be null.
         *
         * @see Target#MUTED
         */
        public Swatch GetMutedSwatch()
        {
            return GetSwatchForTarget(Target.Muted);
        }
        /**
         * Returns a muted and light swatch from the palette. Might be null.
         *
         * @see Target#LIGHT_MUTED
         */
        public Swatch GetLightMutedSwatch()
        {
            return GetSwatchForTarget(Target.LightMuted);
        }
        /**
         * Returns a muted and dark swatch from the palette. Might be null.
         *
         * @see Target#DARK_MUTED
         */
        public Swatch GetDarkMutedSwatch()
        {
            return GetSwatchForTarget(Target.DarkMuted);
        }
        /**
         * Returns the most vibrant color in the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getVibrantSwatch()
         */
        public Color GetVibrantColor(Color defaultColor)
        {
            return GetColorForTarget(Target.Vibrant, defaultColor);
        }
        /**
         * Returns a light and vibrant color from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getLightVibrantSwatch()
         */
        public Color GetLightVibrantColor(Color defaultColor)
        {
            return GetColorForTarget(Target.LightVibrant, defaultColor);
        }
        /**
         * Returns a dark and vibrant color from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getDarkVibrantSwatch()
         */
        public Color GetDarkVibrantColor(Color defaultColor)
        {
            return GetColorForTarget(Target.DarkVibrant, defaultColor);
        }
        /**
         * Returns a muted color from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getMutedSwatch()
         */
        public Color GetMutedColor(Color defaultColor)
        {
            return GetColorForTarget(Target.Muted, defaultColor);
        }
        /**
         * Returns a muted and light color from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getLightMutedSwatch()
         */
        public Color GetLightMutedColor(Color defaultColor)
        {
            return GetColorForTarget(Target.LightMuted, defaultColor);
        }
        /**
         * Returns a muted and dark color from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getDarkMutedSwatch()
         */
        public Color GetDarkMutedColor(Color defaultColor)
        {
            return GetColorForTarget(Target.DarkMuted, defaultColor);
        }
        /**
         * Returns the selected swatch for the given target from the palette, or {@code null} if one
         * could not be found.
         */
        private Swatch GetSwatchForTarget(Target key)
        {
            return _selectedSwatches[key];
        }
        /**
         * Returns the selected color for the given target from the palette as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         */
        private Color GetColorForTarget(Target target, Color defaultColor)
        {
            Swatch swatch = GetSwatchForTarget(target);
            return swatch?.GetArgb() ?? defaultColor;
        }
        /**
         * Returns the dominant swatch from the palette.
         *
         * <p>The dominant swatch is defined as the swatch with the greatest population (frequency)
         * within the palette.</p>
         */
        public Swatch GetDominantSwatch()
        {
            return _dominantSwatch;
        }
        /**
         * Returns the color of the dominant swatch from the palette, as an RGB packed int.
         *
         * @param defaultColor value to return if the swatch isn't available
         * @see #getDominantSwatch()
         */
        public Color GetDominantColor(Color defaultColor)
        {
            return _dominantSwatch?.GetArgb() ?? defaultColor;
        }
        #endregion

        public void Generate()
        {
            // We need to make sure that the scored targets are generated first. This is so that
            // inherited targets have something to inherit from
            for (int i = 0, count = _targets.Count; i < count; i++)
            {
                Target target = _targets.ElementAt(i);
                target.NormalizeWeights();
                if (_selectedSwatches.ContainsKey(target))
                {
                    _selectedSwatches[target] = GenerateScoredTarget(target);
                }
                else
                {
                    _selectedSwatches.Add(target, GenerateScoredTarget(target));
                }
            }
            // We now clear out the used colors
            _usedColors.Clear();
        }

        private Swatch GenerateScoredTarget(Target target)
        {
            Swatch maxScoreSwatch = GetMaxScoredSwatchForTarget(target);
            if (maxScoreSwatch != null && target.IsExclusive())
            {
                // If we have a swatch, and the target is exclusive, add the color to the used list
                _usedColors.Append(maxScoreSwatch.GetArgb(), true);
            }
            return maxScoreSwatch;
        }

        private Swatch GetMaxScoredSwatchForTarget(Target target)
        {
            float maxScore = 0;
            Swatch maxScoreSwatch = null;
            for (int i = 0, count = _swatches.Count; i < count; i++)
            {
                Swatch swatch = _swatches.ElementAt(i);
                if (ShouldBeScoredForTarget(swatch, target))
                {
                    float score = GenerateScore(swatch, target);
                    if (maxScoreSwatch == null || score > maxScore)
                    {
                        maxScoreSwatch = swatch;
                        maxScore = score;
                    }
                }
            }
            return maxScoreSwatch;
        }

        private bool ShouldBeScoredForTarget(Swatch swatch, Target target)
        {
            // Check whether the HSL values are within the correct ranges, and this color hasn't
            // been used yet.
            float[] hsl = swatch.GetHsl();
            return hsl[1] >= target.GetMinimumSaturation() && hsl[1] <= target.GetMaximumSaturation()
                   && hsl[2] >= target.GetMinimumLightness() && hsl[2] <= target.GetMaximumLightness()
                   && !_usedColors.Get(swatch.GetArgb());
        }
        private float GenerateScore(Swatch swatch, Target target)
        {
            float[] hsl = swatch.GetHsl();
            float saturationScore = 0;
            float luminanceScore = 0;
            float populationScore = 0;
            int maxPopulation = _dominantSwatch?.GetPopulation() ?? 1;
            if (target.GetSaturationWeight() > 0)
            {
                saturationScore = target.GetSaturationWeight()
                                  * (1f - Math.Abs(hsl[1] - target.GetTargetSaturation()));
            }
            if (target.GetLightnessWeight() > 0)
            {
                luminanceScore = target.GetLightnessWeight()
                                 * (1f - Math.Abs(hsl[2] - target.GetTargetLightness()));
            }
            if (target.GetPopulationWeight() > 0)
            {
                populationScore = target.GetPopulationWeight()
                                  * (swatch.GetPopulation() / (float)maxPopulation);
            }
            return saturationScore + luminanceScore + populationScore;
        }

        private Swatch FindDominantSwatch()
        {
            int maxPop = int.MinValue;
            Swatch maxSwatch = null;
            for (int i = 0, count = _swatches.Count; i < count; i++)
            {
                Swatch swatch = _swatches.ElementAt(i);
                if (swatch.GetPopulation() > maxPop)
                {
                    maxSwatch = swatch;
                    maxPop = swatch.GetPopulation();
                }
            }
            return maxSwatch;
        }
    }
}