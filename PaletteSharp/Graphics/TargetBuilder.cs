namespace PaletteSharp.Graphics
{
    /**
     * Builder class for generating custom {@link Target} instances.
     */
    internal class TargetBuilder
    {
        private readonly Target _target;
        /**
         * Create a new {@link Target} builder from scratch.
         */
        internal TargetBuilder()
        {
            _target = new Target();
        }
        /**
         * Create a new builder based on an existing {@link Target}.
         */
        internal TargetBuilder(Target target)
        {
            _target = new Target(target);
        }
        /**
         * Set the minimum saturation value for this target.
         */
        internal TargetBuilder SetMinimumSaturation(float value)
        {
            _target.MSaturationTargets[Target.IndexMin] = value;
            return this;
        }
        /**
         * Set the target/ideal saturation value for this target.
         */
        internal TargetBuilder SetTargetSaturation(float value)
        {
            _target.MSaturationTargets[Target.IndexTarget] = value;
            return this;
        }
        /**
         * Set the maximum saturation value for this target.
         */
        internal TargetBuilder SetMaximumSaturation(float value)
        {
            _target.MSaturationTargets[Target.IndexMax] = value;
            return this;
        }
        /**
         * Set the minimum lightness value for this target.
         */
        internal TargetBuilder SetMinimumLightness(float value)
        {
            _target.MLightnessTargets[Target.IndexMin] = value;
            return this;
        }
        /**
         * Set the target/ideal lightness value for this target.
         */
        internal TargetBuilder SetTargetLightness(float value)
        {
            _target.MLightnessTargets[Target.IndexTarget] = value;
            return this;
        }
        /**
         * Set the maximum lightness value for this target.
         */
        internal TargetBuilder SetMaximumLightness(float value)
        {
            _target.MLightnessTargets[Target.IndexMax] = value;
            return this;
        }
        /**
         * Set the weight of importance that this target will place on saturation values.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a color
         * being close to the target value has on selection.</p>
         *
         * <p>A weight of 0 means that it has no weight, and thus has no
         * bearing on the selection.</p>
         *
         * @see #setTargetSaturation(float)
         */
        internal TargetBuilder SetSaturationWeight(float weight)
        {
            _target.MWeights[Target.IndexWeightSat] = weight;
            return this;
        }
        /**
         * Set the weight of importance that this target will place on lightness values.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a color
         * being close to the target value has on selection.</p>
         *
         * <p>A weight of 0 means that it has no weight, and thus has no
         * bearing on the selection.</p>
         *
         * @see #setTargetLightness(float)
         */
        internal TargetBuilder SetLightnessWeight(float weight)
        {
            _target.MWeights[Target.IndexWeightLuma] = weight;
            return this;
        }
        /**
         * Set the weight of importance that this target will place on a color's population within
         * the image.
         *
         * <p>The larger the weight, relative to the other weights, the more important that a
         * color's population being close to the most populous has on selection.</p>
         *
         * <p>A weight of 0 means that it has no weight, and thus has no
         * bearing on the selection.</p>
         */
        internal TargetBuilder SetPopulationWeight(float weight)
        {
            _target.MWeights[Target.IndexWeightPop] = weight;
            return this;
        }
        /**
         * Set whether any color selected for this target is exclusive to this target only.
         * Defaults to true.
         *
         * @param exclusive true if any the color is exclusive to this target, or false is the
         *                  color can be selected for other targets.
         */
        internal TargetBuilder SetExclusive(bool exclusive)
        {
            _target.MIsExclusive = exclusive;
            return this;
        }
        /**
         * Builds and returns the resulting {@link Target}.
         */
        internal Target Build()
        {
            return _target;
        }
    }
}
