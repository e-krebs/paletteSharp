    /**
     * Builder class for generating {@link Palette} instances.
     */
    public static final class Builder {
        private final List<Swatch> mSwatches;
        private final Bitmap mBitmap;
        private final List<Target> mTargets = new ArrayList<>();
        private int mMaxColors = DEFAULT_CALCULATE_NUMBER_COLORS;
        private int mResizeArea = DEFAULT_RESIZE_BITMAP_AREA;
        private int mResizeMaxDimension = -1;
        private final List<Filter> mFilters = new ArrayList<>();
        private Rect mRegion;
        /**
         * Construct a new {@link Builder} using a source {@link Bitmap}
         */
        public Builder(Bitmap bitmap) {
            if (bitmap == null || bitmap.isRecycled()) {
                throw new IllegalArgumentException("Bitmap is not valid");
            }
            mFilters.add(DEFAULT_FILTER);
            mBitmap = bitmap;
            mSwatches = null;
            // Add the default targets
            mTargets.add(Target.LIGHT_VIBRANT);
            mTargets.add(Target.VIBRANT);
            mTargets.add(Target.DARK_VIBRANT);
            mTargets.add(Target.LIGHT_MUTED);
            mTargets.add(Target.MUTED);
            mTargets.add(Target.DARK_MUTED);
        }
        /**
         * Construct a new {@link Builder} using a list of {@link Swatch} instances.
         * Typically only used for testing.
         */
        public Builder(List<Swatch> swatches) {
            if (swatches == null || swatches.isEmpty()) {
                throw new IllegalArgumentException("List of Swatches is not valid");
            }
            mFilters.add(DEFAULT_FILTER);
            mSwatches = swatches;
            mBitmap = null;
        }
        /**
         * Set the maximum number of colors to use in the quantization step when using a
         * {@link android.graphics.Bitmap} as the source.
         * <p>
         * Good values for depend on the source image type. For landscapes, good values are in
         * the range 10-16. For images which are largely made up of people's faces then this
         * value should be increased to ~24.
         */
        @NonNull
        public Builder maximumColorCount(int colors) {
            mMaxColors = colors;
            return this;
        }
        /**
         * Set the resize value when using a {@link android.graphics.Bitmap} as the source.
         * If the bitmap's area is greater than the value specified, then the bitmap
         * will be resized so that its area matches {@code area}. If the
         * bitmap is smaller or equal, the original is used as-is.
         * <p>
         * This value has a large effect on the processing time. The larger the resized image is,
         * the greater time it will take to generate the palette. The smaller the image is, the
         * more detail is lost in the resulting image and thus less precision for color selection.
         *
         * @param area the number of pixels that the intermediary scaled down Bitmap should cover,
         *             or any value <= 0 to disable resizing.
         */
        @NonNull
        public Builder resizeBitmapArea(final int area) {
            mResizeArea = area;
            mResizeMaxDimension = -1;
            return this;
        }
        /**
         * Clear all added filters. This includes any default filters added automatically by
         * {@link Palette}.
         */
        @NonNull
        public Builder clearFilters() {
            mFilters.clear();
            return this;
        }
        /**
         * Add a filter to be able to have fine grained control over which colors are
         * allowed in the resulting palette.
         *
         * @param filter filter to add.
         */
        @NonNull
        public Builder addFilter(Filter filter) {
            if (filter != null) {
                mFilters.add(filter);
            }
            return this;
        }
        /**
         * Set a region of the bitmap to be used exclusively when calculating the palette.
         * <p>This only works when the original input is a {@link Bitmap}.</p>
         *
         * @param left The left side of the rectangle used for the region.
         * @param top The top of the rectangle used for the region.
         * @param right The right side of the rectangle used for the region.
         * @param bottom The bottom of the rectangle used for the region.
         */
        @NonNull
        public Builder setRegion(int left, int top, int right, int bottom) {
            if (mBitmap != null) {
                if (mRegion == null) mRegion = new Rect();
                // Set the Rect to be initially the whole Bitmap
                mRegion.set(0, 0, mBitmap.getWidth(), mBitmap.getHeight());
                // Now just get the intersection with the region
                if (!mRegion.intersect(left, top, right, bottom)) {
                    throw new IllegalArgumentException("The given region must intersect with "
                            + "the Bitmap's dimensions.");
                }
            }
            return this;
        }
        /**
         * Clear any previously region set via {@link #setRegion(int, int, int, int)}.
         */
        @NonNull
        public Builder clearRegion() {
            mRegion = null;
            return this;
        }
        /**
         * Add a target profile to be generated in the palette.
         *
         * <p>You can retrieve the result via {@link Palette#getSwatchForTarget(Target)}.</p>
         */
        @NonNull
        public Builder addTarget(@NonNull final Target target) {
            if (!mTargets.contains(target)) {
                mTargets.add(target);
            }
            return this;
        }
        /**
         * Clear all added targets. This includes any default targets added automatically by
         * {@link Palette}.
         */
        @NonNull
        public Builder clearTargets() {
            if (mTargets != null) {
                mTargets.clear();
            }
            return this;
        }
        /**
         * Generate and return the {@link Palette} synchronously.
         */
        @NonNull
        public Palette generate() {
            final TimingLogger logger = LOG_TIMINGS
                    ? new TimingLogger(LOG_TAG, "Generation")
                    : null;
            List<Swatch> swatches;
            if (mBitmap != null) {
                // We have a Bitmap so we need to use quantization to reduce the number of colors
                // First we'll scale down the bitmap if needed
                final Bitmap bitmap = scaleBitmapDown(mBitmap);
                if (logger != null) {
                    logger.addSplit("Processed Bitmap");
                }
                final Rect region = mRegion;
                if (bitmap != mBitmap && region != null) {
                    // If we have a scaled bitmap and a selected region, we need to scale down the
                    // region to match the new scale
                    final double scale = bitmap.getWidth() / (double) mBitmap.getWidth();
                    region.left = (int) Math.floor(region.left * scale);
                    region.top = (int) Math.floor(region.top * scale);
                    region.right = Math.min((int) Math.ceil(region.right * scale),
                            bitmap.getWidth());
                    region.bottom = Math.min((int) Math.ceil(region.bottom * scale),
                            bitmap.getHeight());
                }
                // Now generate a quantizer from the Bitmap
                final ColorCutQuantizer quantizer = new ColorCutQuantizer(
                        getPixelsFromBitmap(bitmap),
                        mMaxColors,
                        mFilters.isEmpty() ? null : mFilters.toArray(new Filter[mFilters.size()]));
                // If created a new bitmap, recycle it
                if (bitmap != mBitmap) {
                    bitmap.recycle();
                }
                swatches = quantizer.getQuantizedColors();
                if (logger != null) {
                    logger.addSplit("Color quantization completed");
                }
            } else {
                // Else we're using the provided swatches
                swatches = mSwatches;
            }
            // Now create a Palette instance
            final Palette p = new Palette(swatches, mTargets);
            // And make it generate itself
            p.generate();
            if (logger != null) {
                logger.addSplit("Created Palette");
                logger.dumpToLog();
            }
            return p;
        }
        /**
         * Generate the {@link Palette} asynchronously. The provided listener's
         * {@link PaletteAsyncListener#onGenerated} method will be called with the palette when
         * generated.
         */
        @NonNull
        public AsyncTask<Bitmap, Void, Palette> generate(final PaletteAsyncListener listener) {
            if (listener == null) {
                throw new IllegalArgumentException("listener can not be null");
            }
            return AsyncTaskCompat.executeParallel(
                    new AsyncTask<Bitmap, Void, Palette>() {
                        @Override
                        protected Palette doInBackground(Bitmap... params) {
                            try {
                                return generate();
                            } catch (Exception e) {
                                Log.e(LOG_TAG, "Exception thrown during async generate", e);
                                return null;
                            }
                        }
                        @Override
                        protected void onPostExecute(Palette colorExtractor) {
                            listener.onGenerated(colorExtractor);
                        }
                    }, mBitmap);
        }
        private int[] getPixelsFromBitmap(Bitmap bitmap) {
            final int bitmapWidth = bitmap.getWidth();
            final int bitmapHeight = bitmap.getHeight();
            final int[] pixels = new int[bitmapWidth * bitmapHeight];
            bitmap.getPixels(pixels, 0, bitmapWidth, 0, 0, bitmapWidth, bitmapHeight);
            if (mRegion == null) {
                // If we don't have a region, return all of the pixels
                return pixels;
            } else {
                // If we do have a region, lets create a subset array containing only the region's
                // pixels
                final int regionWidth = mRegion.width();
                final int regionHeight = mRegion.height();
                // pixels contains all of the pixels, so we need to iterate through each row and
                // copy the regions pixels into a new smaller array
                final int[] subsetPixels = new int[regionWidth * regionHeight];
                for (int row = 0; row < regionHeight; row++) {
                    System.arraycopy(pixels, ((row + mRegion.top) * bitmapWidth) + mRegion.left,
                            subsetPixels, row * regionWidth, regionWidth);
                }
                return subsetPixels;
            }
        }
        /**
         * Scale the bitmap down as needed.
         */
        private Bitmap scaleBitmapDown(final Bitmap bitmap) {
            double scaleRatio = -1;
            if (mResizeArea > 0) {
                final int bitmapArea = bitmap.getWidth() * bitmap.getHeight();
                if (bitmapArea > mResizeArea) {
                    scaleRatio = Math.sqrt(mResizeArea / (double) bitmapArea);
                }
            } else if (mResizeMaxDimension > 0) {
                final int maxDimension = Math.max(bitmap.getWidth(), bitmap.getHeight());
                if (maxDimension > mResizeMaxDimension) {
                    scaleRatio = mResizeMaxDimension / (double) maxDimension;
                }
            }
            if (scaleRatio <= 0) {
                // Scaling has been disabled or not needed so just return the Bitmap
                return bitmap;
            }
            return Bitmap.createScaledBitmap(bitmap,
                    (int) Math.ceil(bitmap.getWidth() * scaleRatio),
                    (int) Math.ceil(bitmap.getHeight() * scaleRatio),
                    false);
        }
    }
