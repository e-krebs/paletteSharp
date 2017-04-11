using System;
using System.Collections.Generic;
using System.Drawing;
using PaletteSharp.Filters;
using PaletteSharp.Helpers;
using PriorityQueue.Collections;

namespace PaletteSharp.Graphics
{
    internal class ColorCutQuantizer
    {
        const int ComponentRed = -3;
        const int ComponentGreen = -2;
        const int ComponentBlue = -1;
        private const int QuantizeWordWidth = 5;
        private const int QuantizeWordMask = (1 << QuantizeWordWidth) - 1;
        static int[] _colors;
        static int[] _histogram;
        readonly List<Swatch> _quantizedColors;
        readonly IFilter[] _filters;
        private readonly float[] _mTempHsl = new float[3];
        /**
         * Constructor.
         *
         * @param pixels histogram representing an image's pixel data
         * @param maxColors The maximum number of colors that should be in the result palette.
         * @param filters Set of filters to use in the quantization stage
         */
        internal ColorCutQuantizer(int[] pixels, int maxColors, IFilter[] filters)
        {
            _filters = filters;
            int[] hist = _histogram = new int[1 << (QuantizeWordWidth * 3)];
            for (int i = 0; i < pixels.Length; i++)
            {
                int quantizedColor = QuantizeFromRgb888(pixels[i]);
                // Now update the pixel value to the quantized value
                pixels[i] = quantizedColor;
                // And update the histogram
                hist[quantizedColor]++;
            }
            // Now let's count the number of distinct colors
            int distinctColorCount = 0;
            for (int color = 0; color < hist.Length; color++)
            {
                if (hist[color] > 0 && ShouldIgnoreColor(color))
                {
                    // If we should ignore the color, set the population to 0
                    hist[color] = 0;
                }
                if (hist[color] > 0)
                {
                    // If the color has population, increase the distinct color count
                    distinctColorCount++;
                }
            }
            // Now lets go through create an array consisting of only distinct colors
            int[] colors = _colors = new int[distinctColorCount];
            int distinctColorIndex = 0;
            for (int color = 0; color < hist.Length; color++)
            {
                if (hist[color] > 0)
                {
                    colors[distinctColorIndex++] = color;
                }
            }
            if (distinctColorCount <= maxColors)
            {
                // The image has fewer colors than the maximum requested, so just return the colors
                _quantizedColors = new List<Swatch>();
                foreach (int color in colors)
                {
                    _quantizedColors.Add(new Swatch(ApproximateToRgb888(color), hist[color]));
                }
            }
            else
            {
                // We need use quantization to reduce the number of colors
                _quantizedColors = QuantizePixels(maxColors);
            }
        }
        /**
         * @return the list of quantized colors
         */
        internal List<Swatch> GetQuantizedColors()
        {
            return _quantizedColors;
        }
        private List<Swatch> QuantizePixels(int maxColors)
        {
            // Create the priority queue which is sorted by volume descending. This means we always
            // split the largest box in the queue
            var vboxComparerVolume = new VboxComparerVolume();
            PriorityQueue<Vbox> pq = new PriorityQueue<Vbox>(vboxComparerVolume);
            // To start, offer a box which contains all of the colors
            pq.Offer(new Vbox(0, _colors.Length - 1));
            // Now go through the boxes, splitting them until we have reached maxColors or there are no
            // more boxes to split
            SplitBoxes(pq, maxColors);
            // Finally, return the average colors of the color boxes
            return GenerateAverageColors(pq);
        }
        /**
         * Iterate through the {@link java.util.Queue}, popping
         * {@link ColorCutQuantizer.Vbox} objects from the queue
         * and splitting them. Once split, the new box and the remaining box are offered back to the
         * queue.
         *
         * @param queue {@link java.util.PriorityQueue} to poll for boxes
         * @param maxSize Maximum amount of boxes to split
         */
        private void SplitBoxes(PriorityQueue<Vbox> queue, int maxSize)
        {
            while (queue.Count < maxSize)
            {
                Vbox vbox = queue.Poll();
                if (vbox != null && vbox.CanSplit())
                {
                    // First split the box, and offer the result
                    queue.Offer(vbox.SplitBox());
                    // Then offer the box back
                    queue.Offer(vbox);
                }
                else
                {
                    // If we get here then there are no more boxes to split, so return
                    return;
                }
            }
        }
        private List<Swatch> GenerateAverageColors(PriorityQueue<Vbox> vboxes)
        {
            List<Swatch> colors = new List<Swatch>();
            while (vboxes.Count > 0)
            {
                Vbox vbox = vboxes.Poll();
                Swatch swatch = vbox.GetAverageColor();
                if (!ShouldIgnoreColor(swatch))
                {
                    // As we're averaging a color box, we can still get colors which we do not want, so
                    // we check again here
                    colors.Add(swatch);
                }
            }
            return colors;
        }
        /**
         * Modify the significant octet in a packed color int. Allows sorting based on the value of a
         * single color component. This relies on all components being the same word size.
         *
         * @see Vbox#findSplitPoint()
         */
        internal static void ModifySignificantOctet(int[] a, int dimension,
            int lower, int upper)
        {
            switch (dimension)
            {
                case ComponentRed:
                    // Already in RGB, no need to do anything
                    break;
                case ComponentGreen:
                    // We need to do a RGB to GRB swap, or vice-versa
                    for (int i = lower; i <= upper; i++)
                    {
                        int color = a[i];
                        a[i] = QuantizedGreen(color) << (QuantizeWordWidth + QuantizeWordWidth)
                               | QuantizedRed(color) << QuantizeWordWidth
                               | QuantizedBlue(color);
                    }
                    break;
                case ComponentBlue:
                    // We need to do a RGB to BGR swap, or vice-versa
                    for (int i = lower; i <= upper; i++)
                    {
                        int color = a[i];
                        a[i] = QuantizedBlue(color) << (QuantizeWordWidth + QuantizeWordWidth)
                               | QuantizedGreen(color) << QuantizeWordWidth
                               | QuantizedRed(color);
                    }
                    break;
            }
        }
        private bool ShouldIgnoreColor(int color565)
        {
            Color rgb = ApproximateToRgb888(color565);
            ColorUtils.ColorToHSL(rgb, _mTempHsl);
            return ShouldIgnoreColor(rgb, _mTempHsl);
        }
        private bool ShouldIgnoreColor(Swatch color)
        {
            return ShouldIgnoreColor(color.GetArgb(), color.GetHsl());
        }
        private bool ShouldIgnoreColor(Color rgb, float[] hsl)
        {
            if (_filters != null && _filters.Length > 0)
            {
                for (int i = 0, count = _filters.Length; i < count; i++)
                {
                    if (!_filters[i].IsAllowed(rgb, hsl))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /**
         * Comparator which sorts {@link Vbox} instances based on their volume, in descending order
         */
        private class VboxComparerVolume : IComparer<Vbox>
        {
            public int Compare(Vbox lhs, Vbox rhs)
            {
                if (rhs != null && lhs != null) return rhs.GetVolume() - lhs.GetVolume();
                if (rhs == null && lhs == null) return 0;
                if (rhs == null) return 1;
                return -1; // lhs == null
            }
        }

    /**
     * Quantized a RGB888 value to have a word width of {@value #QUANTIZE_WORD_WIDTH}.
     */
    private static int QuantizeFromRgb888(int color)
        {
            int r = ModifyWordWidth(color.ToColor().R, 8, QuantizeWordWidth);
            int g = ModifyWordWidth(color.ToColor().G, 8, QuantizeWordWidth);
            int b = ModifyWordWidth(color.ToColor().B, 8, QuantizeWordWidth);
            return r << (QuantizeWordWidth + QuantizeWordWidth) | g << QuantizeWordWidth | b;
        }
        /**
         * Quantized RGB888 values to have a word width of {@value #QUANTIZE_WORD_WIDTH}.
         */
        internal static Color ApproximateToRgb888(int r, int g, int b)
        {
            return Color.FromArgb(ModifyWordWidth(r, QuantizeWordWidth, 8),
                ModifyWordWidth(g, QuantizeWordWidth, 8),
                ModifyWordWidth(b, QuantizeWordWidth, 8));
        }
        private static Color ApproximateToRgb888(int color)
        {
            return ApproximateToRgb888(QuantizedRed(color), QuantizedGreen(color), QuantizedBlue(color));
        }
        /**
         * @return red component of the quantized color
         */
        internal static int QuantizedRed(int color)
        {
            return (color >> (QuantizeWordWidth + QuantizeWordWidth)) & QuantizeWordMask;
        }
        /**
         * @return green component of a quantized color
         */
        internal static int QuantizedGreen(int color)
        {
            return (color >> QuantizeWordWidth) & QuantizeWordMask;
        }
        /**
         * @return blue component of a quantized color
         */
        internal static int QuantizedBlue(int color)
        {
            return color & QuantizeWordMask;
        }
        private static int ModifyWordWidth(int value, int currentWidth, int targetWidth)
        {
            int newValue;
            if (targetWidth > currentWidth)
            {
                // If we're approximating up in word width, we'll shift up
                newValue = value << (targetWidth - currentWidth);
            }
            else
            {
                // Else, we will just shift and keep the MSB
                newValue = value >> (currentWidth - targetWidth);
            }
            return newValue & ((1 << targetWidth) - 1);
        }

        #region Vbox
        /// <summary>
        /// Represents a tightly fitting box around a color space
        /// </summary>
        private class Vbox
        {
            // lower and upper index are inclusive
            private readonly int _lowerIndex;
            private int _upperIndex;
            // Population of colors within this box
            private int _population;
            private int _minRed, _maxRed;
            private int _minGreen, _maxGreen;
            private int _minBlue, _maxBlue;
            internal Vbox(int lowerIndex, int upperIndex)
            {
                _lowerIndex = lowerIndex;
                _upperIndex = upperIndex;
                FitBox();
            }

            internal int GetVolume()
            {
                return (_maxRed - _minRed + 1) * (_maxGreen - _minGreen + 1) *
                       (_maxBlue - _minBlue + 1);
            }
            internal bool CanSplit()
            {
                return GetColorCount() > 1;
            }

            private int GetColorCount()
            {
                return 1 + _upperIndex - _lowerIndex;
            }
            /**
             * Recomputes the boundaries of this box to tightly fit the colors within the box.
             */
            private void FitBox()
            {
                int[] colors = _colors;
                int[] hist = _histogram;
                // Reset the min and max to opposite values
                int minGreen, minBlue;
                int minRed = minGreen = minBlue = int.MaxValue;
                int maxGreen, maxBlue;
                int maxRed = maxGreen = maxBlue = int.MinValue;
                int count = 0;
                for (int i = _lowerIndex; i <= _upperIndex; i++)
                {
                    int color = colors[i];
                    count += hist[color];
                    int r = QuantizedRed(color);
                    int g = QuantizedGreen(color);
                    int b = QuantizedBlue(color);
                    if (r > maxRed)
                    {
                        maxRed = r;
                    }
                    if (r < minRed)
                    {
                        minRed = r;
                    }
                    if (g > maxGreen)
                    {
                        maxGreen = g;
                    }
                    if (g < minGreen)
                    {
                        minGreen = g;
                    }
                    if (b > maxBlue)
                    {
                        maxBlue = b;
                    }
                    if (b < minBlue)
                    {
                        minBlue = b;
                    }
                }
                _minRed = minRed;
                _maxRed = maxRed;
                _minGreen = minGreen;
                _maxGreen = maxGreen;
                _minBlue = minBlue;
                _maxBlue = maxBlue;
                _population = count;
            }
            /**
             * Split this color box at the mid-point along its longest dimension
             *
             * @return the new ColorBox
             */
            internal Vbox SplitBox()
            {
                if (!CanSplit())
                {
                    throw new Exception("Can not split a box with only 1 color");
                }
                // find median along the longest dimension
                int splitPoint = FindSplitPoint();
                Vbox newBox = new Vbox(splitPoint + 1, _upperIndex);
                // Now change this box's upperIndex and recompute the color boundaries
                _upperIndex = splitPoint;
                FitBox();
                return newBox;
            }
            /**
             * @return the dimension which this box is largest in
             */
            private int GetLongestColorDimension()
            {
                int redLength = _maxRed - _minRed;
                int greenLength = _maxGreen - _minGreen;
                int blueLength = _maxBlue - _minBlue;
                if (redLength >= greenLength && redLength >= blueLength)
                {
                    return ComponentRed;
                }
                else if (greenLength >= redLength && greenLength >= blueLength)
                {
                    return ComponentGreen;
                }
                else
                {
                    return ComponentBlue;
                }
            }
            /**
             * Finds the point within this box's lowerIndex and upperIndex index of where to split.
             *
             * This is calculated by finding the longest color dimension, and then sorting the
             * sub-array based on that dimension value in each color. The colors are then iterated over
             * until a color is found with at least the midpoint of the whole box's dimension midpoint.
             *
             * @return the index of the colors array to split from
             */
            private int FindSplitPoint()
            {
                int longestDimension = GetLongestColorDimension();
                int[] colors = _colors;
                int[] hist = _histogram;
                // We need to sort the colors in this box based on the longest color dimension.
                // As we can't use a Comparator to define the sort logic, we modify each color so that
                // its most significant is the desired dimension
                ModifySignificantOctet(colors, longestDimension, _lowerIndex, _upperIndex);
                Array.Sort(colors, _lowerIndex, _upperIndex - _lowerIndex +1);
                // Now revert all of the colors so that they are packed as RGB again
                ModifySignificantOctet(colors, longestDimension, _lowerIndex, _upperIndex);
                int midPoint = _population / 2;
                for (int i = _lowerIndex, count = 0; i <= _upperIndex; i++)
                {
                    count += hist[colors[i]];
                    if (count >= midPoint)
                    {
                        return i;
                    }
                }
                return _lowerIndex;
            }
            /**
             * @return the average color of this box.
             */
            internal Swatch GetAverageColor()
            {
                int[] colors = _colors;
                int[] hist = _histogram;
                int redSum = 0;
                int greenSum = 0;
                int blueSum = 0;
                int totalPopulation = 0;
                for (int i = _lowerIndex; i <= _upperIndex; i++)
                {
                    int color = colors[i];
                    int colorPopulation = hist[color];
                    totalPopulation += colorPopulation;
                    redSum += colorPopulation * QuantizedRed(color);
                    greenSum += colorPopulation * QuantizedGreen(color);
                    blueSum += colorPopulation * QuantizedBlue(color);
                }
                int redMean = (int)Math.Round(redSum / (float)totalPopulation, 0);
                int greenMean = (int)Math.Round(greenSum / (float)totalPopulation, 0);
                int blueMean = (int)Math.Round(blueSum / (float)totalPopulation, 0);
                return new Swatch(ApproximateToRgb888(redMean, greenMean, blueMean), totalPopulation);
            }
        }

        #endregion
    }
}
