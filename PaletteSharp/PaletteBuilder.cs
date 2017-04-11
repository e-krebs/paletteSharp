using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaletteSharp.Filters;
using PaletteSharp.Graphics;
using PaletteSharp.Helpers;

namespace PaletteSharp
{
    public class PaletteBuilder
    {
        private readonly List<Swatch> _swatches;
        private readonly Bitmap _bitmap;
        private readonly List<Target> _targets = new List<Target>();
        private int _maxColors = Palette.DefaultCalculateNumberColors;
        private int _resizeArea = Palette.DefaultResizeBitmapArea;
        private int _resizeMaxDimension = -1;
        private readonly List<IFilter> _filters = new List<IFilter>();
        private Rectangle? _region;

        /**
         * Construct a new {@link Builder} using a source {@link Bitmap}
         */
        public PaletteBuilder(Bitmap bitmap)
        {
            _filters.Add(new DefaultFilter());
            _bitmap = bitmap ?? throw new ArgumentNullException();
            _swatches = null;
            // Add the default targets
            _targets.Add(Target.LightVibrant);
            _targets.Add(Target.Vibrant);
            _targets.Add(Target.DarkVibrant);
            _targets.Add(Target.LightMuted);
            _targets.Add(Target.Muted);
            _targets.Add(Target.DarkMuted);
        }

        /**
         * Construct a new {@link Builder} using a list of {@link Swatch} instances.
         * Typically only used for testing.
         */
        public PaletteBuilder(List<Swatch> swatches)
        {
            if (swatches == null || !swatches.Any())
            {
                throw new ArgumentException("List of Swatches is not valid");
            }
            _filters.Add(new DefaultFilter());
            _swatches = swatches;
            _bitmap = null;
        }
        /**
         * Set the maximum number of colors to use in the quantization step when using a
         * {@link android.graphics.Bitmap} as the source.
         * <p>
         * Good values for depend on the source image type. For landscapes, good values are in
         * the range 10-16. For images which are largely made up of people's faces then this
         * value should be increased to ~24.
         */
        public PaletteBuilder MaximumColorCount(int colors)
        {
            _maxColors = colors;
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
        public PaletteBuilder ResizeBitmapArea(int area)
        {
            _resizeArea = area;
            _resizeMaxDimension = -1;
            return this;
        }
        /**
         * Clear all added filters. This includes any default filters added automatically by
         * {@link Palette}.
         */
        public PaletteBuilder ClearFilters()
        {
            _filters.Clear();
            return this;
        }
        /**
         * Add a filter to be able to have fine grained control over which colors are
         * allowed in the resulting palette.
         *
         * @param filter filter to add.
         */
        internal PaletteBuilder AddFilter(IFilter filter)
        {
            if (filter != null)
            {
                _filters.Add(filter);
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
        public PaletteBuilder SetRegion(int left, int top, int right, int bottom)
        {
            if (_bitmap != null)
            {
                if (!_region.HasValue) _region = new Rectangle();
                // Set the Rect to be initially the whole Bitmap
                _region = Rectangle.FromLTRB(0, 0, _bitmap.Width, _bitmap.Height);
                // Now just get the intersection with the region
                if (!_region.Value.IntersectsWith(Rectangle.FromLTRB(left, top, right, bottom)))
                {
                    throw new ArgumentException("The given region must intersect with "
                                                       + "the Bitmap's dimensions.");
                }
            }
            return this;
        }
        /**
         * Clear any previously region set via {@link #setRegion(int, int, int, int)}.
         */
        public PaletteBuilder ClearRegion()
        {
            _region = null;
            return this;
        }
        /**
         * Add a target profile to be generated in the palette.
         *
         * <p>You can retrieve the result via {@link Palette#getSwatchForTarget(Target)}.</p>
         */
        internal PaletteBuilder AddTarget(Target target)
        {
            if (!_targets.Contains(target))
            {
                _targets.Add(target);
            }
            return this;
        }
        /**
         * Clear all added targets. This includes any default targets added automatically by
         * {@link Palette}.
         */
        public PaletteBuilder ClearTargets()
        {
            _targets?.Clear();
            return this;
        }
        /**
         * Generate and return the {@link Palette} synchronously.
         */
        public Palette Generate()
        {
            //TimingLogger logger = LOG_TIMINGS
            //    ? new TimingLogger(LOG_TAG, "Generation")
            //    : null;
            List<Swatch> swatches;
            if (_bitmap != null)
            {
                // We have a Bitmap so we need to use quantization to reduce the number of colors
                // First we'll scale down the bitmap if needed
                Bitmap bitmap = ScaleBitmapDown(_bitmap);
                //if (logger != null)
                //{
                //    logger.addSplit("Processed Bitmap");
                //}
                Rectangle? nullableRegion = _region;
                if (bitmap != _bitmap && nullableRegion.HasValue)
                {
                    Rectangle region = nullableRegion.Value;
                    // If we have a scaled bitmap and a selected region, we need to scale down the
                    // region to match the new scale
                    double scale = bitmap.Width / (double)_bitmap.Width;
                    region = Rectangle.FromLTRB((int)Math.Floor(region.Left * scale), region.Top, region.Right, region.Bottom);
                    region = Rectangle.FromLTRB(region.Left, (int)Math.Floor(region.Top * scale), region.Right, region.Bottom);
                    region = Rectangle.FromLTRB(region.Left, region.Top, Math.Min((int)Math.Ceiling(region.Right * scale), bitmap.Width), region.Bottom);
                    region = Rectangle.FromLTRB(region.Left, region.Top, region.Right, Math.Min((int)Math.Ceiling(region.Bottom * scale), bitmap.Width));
                }
                // Now generate a quantizer from the Bitmap
                ColorCutQuantizer quantizer = new ColorCutQuantizer(GetPixelsFromBitmap(bitmap), _maxColors, _filters.Any() ? null : _filters.ToArray());
                // If created a new bitmap, recycle it
                //if (bitmap != _bitmap)
                //{
                //    bitmap.recycle();
                //}
                swatches = quantizer.GetQuantizedColors();
                //if (logger != null)
                //{
                //    logger.addSplit("Color quantization completed");
                //}
            }
            else
            {
                // Else we're using the provided swatches
                swatches = _swatches;
            }
            // Now create a Palette instance
            Palette p = new Palette(swatches, _targets);
            // And make it generate itself
            p.Generate();
            //if (logger != null)
            //{
            //    logger.addSplit("Created Palette");
            //    logger.dumpToLog();
            //}
            return p;
        }
        /**
         * Generate the {@link Palette} asynchronously. The provided listener's
         * {@link PaletteAsyncListener#onGenerated} method will be called with the palette when
         * generated.
         */
        //public AsyncTask<Bitmap, Void, Palette> generate(PaletteAsyncListener listener)
        //{
        //    if (listener == null)
        //    {
        //        throw new IllegalArgumentException("listener can not be null");
        //    }
        //    return AsyncTaskCompat.executeParallel(
        //        new AsyncTask<Bitmap, Void, Palette>() {
        //                @Override
        //                protected Palette doInBackground(Bitmap... params)
        //                {
        //                try
        //                {
        //                return generate();
        //            }
        //            catch (Exception e)
        //    {
        //        Log.e(LOG_TAG, "Exception thrown during async generate", e);
        //        return null;
        //    }
        //    }
        //    @Override
        //    protected void onPostExecute(Palette colorExtractor)
        //    {
        //        listener.onGenerated(colorExtractor);
        //    }
        //    }, mBitmap);
        //}
        private int[] GetPixelsFromBitmap(Bitmap bitmap)
        {
            int bitmapWidth = bitmap.Width;
            int bitmapHeight = bitmap.Height;
            int[] pixels = new int[bitmapWidth * bitmapHeight];
            bitmap.GetPixels(ref pixels, 0, bitmapWidth, 0, 0, bitmapWidth, bitmapHeight);
            if (!_region.HasValue)
            {
                // If we don't have a region, return all of the pixels
                return pixels;
            }
            // If we do have a region, lets create a subset array containing only the region's
            // pixels
            int regionWidth = _region.Value.Width;
            int regionHeight = _region.Value.Height;
            // pixels contains all of the pixels, so we need to iterate through each row and
            // copy the regions pixels into a new smaller array
            int[] subsetPixels = new int[regionWidth * regionHeight];
            for (int row = 0; row < regionHeight; row++)
            {
                Array.Copy(pixels, (row + _region.Value.Top) * bitmapWidth + _region.Value.Left, subsetPixels, row * regionWidth, regionWidth);
            }
            return subsetPixels;
        }
        /**
         * Scale the bitmap down as needed.
         */
        private Bitmap ScaleBitmapDown(Bitmap bitmap)
        {
            double scaleRatio = -1;
            if (_resizeArea > 0)
            {
                int bitmapArea = bitmap.Width * bitmap.Height;
                if (bitmapArea > _resizeArea)
                {
                    scaleRatio = Math.Sqrt(_resizeArea / (double)bitmapArea);
                }
            }
            else if (_resizeMaxDimension > 0)
            {
                int maxDimension = Math.Max(bitmap.Width, bitmap.Height);
                if (maxDimension > _resizeMaxDimension)
                {
                    scaleRatio = _resizeMaxDimension / (double)maxDimension;
                }
            }
            if (scaleRatio <= 0)
            {
                // Scaling has been disabled or not needed so just return the Bitmap
                return bitmap;
            }
            return bitmap.CreateScaledBitmap((int)Math.Ceiling(bitmap.Width * scaleRatio), (int)Math.Ceiling(bitmap.Height * scaleRatio));
        }
    }
}
