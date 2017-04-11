    /**
     * A Filter provides a mechanism for exercising fine-grained control over which colors
     * are valid within a resulting {@link Palette}.
     */
    public interface Filter {
        /**
         * Hook to allow clients to be able filter colors from resulting palette.
         *
         * @param rgb the color in RGB888.
         * @param hsl HSL representation of the color.
         *
         * @return true if the color is allowed, false if not.
         *
         * @see Builder#addFilter(Filter)
         */
        boolean isAllowed(int rgb, float[] hsl);
    }
    /**
     * The default filter.
     */
    static final Filter DEFAULT_FILTER = new Filter() {
        private static final float BLACK_MAX_LIGHTNESS = 0.05f;
        private static final float WHITE_MIN_LIGHTNESS = 0.95f;
        @Override
        public boolean isAllowed(int rgb, float[] hsl) {
            return !isWhite(hsl) && !isBlack(hsl) && !isNearRedILine(hsl);
        }
        /**
         * @return true if the color represents a color which is close to black.
         */
        private boolean isBlack(float[] hslColor) {
            return hslColor[2] <= BLACK_MAX_LIGHTNESS;
        }
        /**
         * @return true if the color represents a color which is close to white.
         */
        private boolean isWhite(float[] hslColor) {
            return hslColor[2] >= WHITE_MIN_LIGHTNESS;
        }
        /**
         * @return true if the color lies close to the red side of the I line.
         */
        private boolean isNearRedILine(float[] hslColor) {
            return hslColor[0] >= 10f && hslColor[0] <= 37f && hslColor[1] <= 0.82f;
        }
    };
