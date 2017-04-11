    /**
     * Builder class for generating custom {@link Target} instances.
     */
    public final static class Builder {
        private final Target mTarget;
        /**
         * Create a new {@link Target} builder from scratch.
         */
        public Builder() {
            mTarget = new Target();
        }
        /**
         * Create a new builder based on an existing {@link Target}.
         */
        public Builder(Target target) {
            mTarget = new Target(target);
        }
        /**
         * Set the minimum saturation value for this target.
         */
        public Builder setMinimumSaturation(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mSaturationTargets[INDEX_MIN] = value;
            return this;
        }
        /**
         * Set the target/ideal saturation value for this target.
         */
        public Builder setTargetSaturation(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mSaturationTargets[INDEX_TARGET] = value;
            return this;
        }
        /**
         * Set the maximum saturation value for this target.
         */
        public Builder setMaximumSaturation(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mSaturationTargets[INDEX_MAX] = value;
            return this;
        }
        /**
         * Set the minimum lightness value for this target.
         */
        public Builder setMinimumLightness(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mLightnessTargets[INDEX_MIN] = value;
            return this;
        }
        /**
         * Set the target/ideal lightness value for this target.
         */
        public Builder setTargetLightness(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mLightnessTargets[INDEX_TARGET] = value;
            return this;
        }
        /**
         * Set the maximum lightness value for this target.
         */
        public Builder setMaximumLightness(@FloatRange(from = 0, to = 1) float value) {
            mTarget.mLightnessTargets[INDEX_MAX] = value;
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
        public Builder setSaturationWeight(@FloatRange(from = 0) float weight) {
            mTarget.mWeights[INDEX_WEIGHT_SAT] = weight;
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
        public Builder setLightnessWeight(@FloatRange(from = 0) float weight) {
            mTarget.mWeights[INDEX_WEIGHT_LUMA] = weight;
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
        public Builder setPopulationWeight(@FloatRange(from = 0) float weight) {
            mTarget.mWeights[INDEX_WEIGHT_POP] = weight;
            return this;
        }
        /**
         * Set whether any color selected for this target is exclusive to this target only.
         * Defaults to true.
         *
         * @param exclusive true if any the color is exclusive to this target, or false is the
         *                  color can be selected for other targets.
         */
        public Builder setExclusive(boolean exclusive) {
            mTarget.mIsExclusive = exclusive;
            return this;
        }
        /**
         * Builds and returns the resulting {@link Target}.
         */
        public Target build() {
            return mTarget;
        }
    }
