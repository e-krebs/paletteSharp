using System;

namespace PaletteSharp.Graphics
{
    internal class Target
    {
        private static float _targetDarkLuma = 0.26f;
        private static float _maxDarkLuma = 0.45f;
        private static float _minLightLuma = 0.55f;
        private static float _targetLightLuma = 0.74f;
        private static float _minNormalLuma = 0.3f;
        private static float _targetNormalLuma = 0.5f;
        private static float _maxNormalLuma = 0.7f;
        private static float _targetMutedSaturation = 0.3f;
        private static float _maxMutedSaturation = 0.4f;
        private static float _targetVibrantSaturation = 1f;
        private static float _minVibrantSaturation = 0.35f;
        private static float _weightSaturation = 0.24f;
        private static float _weightLuma = 0.52f;
        private static float _weightPopulation = 0.24f;
        internal static int IndexMin = 0;
        internal static int IndexTarget = 1;
        internal static int IndexMax = 2;
        internal static int IndexWeightSat = 0;
        internal static int IndexWeightLuma = 1;
        internal static int IndexWeightPop = 2;
        /**
         * A target which has the characteristics of a vibrant color which is light in luminance.
        */
        internal static Target LightVibrant;
        /**
         * A target which has the characteristics of a vibrant color which is neither light or dark.
         */
        internal static Target Vibrant;
        /**
         * A target which has the characteristics of a vibrant color which is dark in luminance.
         */
        internal static Target DarkVibrant;
        /**
         * A target which has the characteristics of a muted color which is light in luminance.
         */
        internal static Target LightMuted;
        /**
         * A target which has the characteristics of a muted color which is neither light or dark.
         */
        internal static Target Muted;
        /**
         * A target which has the characteristics of a muted color which is dark in luminance.
         */
        internal static Target DarkMuted;
        static Target() {
            LightVibrant = new Target();
            SetDefaultLightLightnessValues(LightVibrant);
            SetDefaultVibrantSaturationValues(LightVibrant);
            Vibrant = new Target();
            SetDefaultNormalLightnessValues(Vibrant);
            SetDefaultVibrantSaturationValues(Vibrant);
            DarkVibrant = new Target();
            SetDefaultDarkLightnessValues(DarkVibrant);
            SetDefaultVibrantSaturationValues(DarkVibrant);
            LightMuted = new Target();
            SetDefaultLightLightnessValues(LightMuted);
            SetDefaultMutedSaturationValues(LightMuted);
            Muted = new Target();
            SetDefaultNormalLightnessValues(Muted);
            SetDefaultMutedSaturationValues(Muted);
            DarkMuted = new Target();
            SetDefaultDarkLightnessValues(DarkMuted);
            SetDefaultMutedSaturationValues(DarkMuted);
        }

        internal float[] MSaturationTargets = new float[3];
        internal float[] MLightnessTargets = new float[3];
        internal float[] MWeights = new float[3];
        internal bool MIsExclusive = true; // default to true
        internal Target()
        {
            SetTargetDefaultValues(MSaturationTargets);
            SetTargetDefaultValues(MLightnessTargets);
            SetDefaultWeights();
        }

        internal Target(Target from)
        {
            Array.Copy(from.MSaturationTargets, 0, MSaturationTargets, 0, MSaturationTargets.Length);
            Array.Copy(from.MLightnessTargets, 0, MLightnessTargets, 0, MLightnessTargets.Length);
            Array.Copy(from.MWeights, 0, MWeights, 0, MWeights.Length);
        }
        /**
         * The minimum saturation value for this target.
         */
        internal float GetMinimumSaturation()
        {
            return MSaturationTargets[IndexMin];
        }
        /**
         * The target saturation value for this target.
         */
        internal float GetTargetSaturation()
        {
            return MSaturationTargets[IndexTarget];
        }
        /**
         * The maximum saturation value for this target.
         */
        internal float GetMaximumSaturation()
        {
            return MSaturationTargets[IndexMax];
        }
        /**
         * The minimum lightness value for this target.
         */
        internal float GetMinimumLightness()
        {
            return MLightnessTargets[IndexMin];
        }
        /**
         * The target lightness value for this target.
         */
        internal float GetTargetLightness()
        {
            return MLightnessTargets[IndexTarget];
        }
        /**
         * The maximum lightness value for this target.
         */
        internal float GetMaximumLightness()
        {
            return MLightnessTargets[IndexMax];
        }
        /**
         * Returns the weight of importance that this target places on a color's saturation within
         * the image.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a color
         * being close to the target value has on selection.</p>
         *
         * @see #getTargetSaturation()
         */
        internal float GetSaturationWeight()
        {
            return MWeights[IndexWeightSat];
        }
        /**
         * Returns the weight of importance that this target places on a color's lightness within
         * the image.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a color
         * being close to the target value has on selection.</p>
         *
         * @see #getTargetLightness()
         */
        internal float GetLightnessWeight()
        {
            return MWeights[IndexWeightLuma];
        }
        /**
         * Returns the weight of importance that this target places on a color's population within
         * the image.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a
         * color's population being close to the most populous has on selection.</p>
         */
        internal float GetPopulationWeight()
        {
            return MWeights[IndexWeightPop];
        }
        /**
         * Returns whether any color selected for this target is exclusive for this target only.
         *
         * <p>If false, then the color can be selected for other targets.</p>
         */
        internal bool IsExclusive()
        {
            return MIsExclusive;
        }
        private static void SetTargetDefaultValues(float[] values)
        {
            values[IndexMin] = 0f;
            values[IndexTarget] = 0.5f;
            values[IndexMax] = 1f;
        }
        private void SetDefaultWeights()
        {
            MWeights[IndexWeightSat] = _weightSaturation;
            MWeights[IndexWeightLuma] = _weightLuma;
            MWeights[IndexWeightPop] = _weightPopulation;
        }
        internal void NormalizeWeights()
        {
            float sum = 0;
            for (int i = 0, z = MWeights.Length; i < z; i++)
            {
                float weight = MWeights[i];
                if (weight > 0)
                {
                    sum += weight;
                }
            }
            if (sum != 0)
            {
                for (int i = 0, z = MWeights.Length; i < z; i++)
                {
                    if (MWeights[i] > 0)
                    {
                        MWeights[i] /= sum;
                    }
                }
            }
        }
        private static void SetDefaultDarkLightnessValues(Target target)
        {
            target.MLightnessTargets[IndexTarget] = _targetDarkLuma;
            target.MLightnessTargets[IndexMax] = _maxDarkLuma;
        }
        private static void SetDefaultNormalLightnessValues(Target target)
        {
            target.MLightnessTargets[IndexMin] = _minNormalLuma;
            target.MLightnessTargets[IndexTarget] = _targetNormalLuma;
            target.MLightnessTargets[IndexMax] = _maxNormalLuma;
        }
        private static void SetDefaultLightLightnessValues(Target target)
        {
            target.MLightnessTargets[IndexMin] = _minLightLuma;
            target.MLightnessTargets[IndexTarget] = _targetLightLuma;
        }
        private static void SetDefaultVibrantSaturationValues(Target target)
        {
            target.MSaturationTargets[IndexMin] = _minVibrantSaturation;
            target.MSaturationTargets[IndexTarget] = _targetVibrantSaturation;
        }
        private static void SetDefaultMutedSaturationValues(Target target)
        {
            target.MSaturationTargets[IndexTarget] = _targetMutedSaturation;
            target.MSaturationTargets[IndexMax] = _maxMutedSaturation;
        }
    }
}