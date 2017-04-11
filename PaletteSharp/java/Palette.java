/*
 * Copyright 2014 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *       http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package android.support.v7.graphics;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.Rect;
import android.os.AsyncTask;
import android.support.annotation.ColorInt;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.v4.graphics.ColorUtils;
import android.support.v4.os.AsyncTaskCompat;
import android.support.v4.util.ArrayMap;
import android.util.Log;
import android.util.SparseBooleanArray;
import android.util.TimingLogger;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.Map;
/**
 * A helper class to extract prominent colors from an image.
 * <p>
 * A number of colors with different profiles are extracted from the image:
 * <ul>
 *     <li>Vibrant</li>
 *     <li>Vibrant Dark</li>
 *     <li>Vibrant Light</li>
 *     <li>Muted</li>
 *     <li>Muted Dark</li>
 *     <li>Muted Light</li>
 * </ul>
 * These can be retrieved from the appropriate getter method.
 *
 * <p>
 * Instances are created with a {@link Builder} which supports several options to tweak the
 * generated Palette. See that class' documentation for more information.
 * <p>
 * Generation should always be completed on a background thread, ideally the one in
 * which you load your image on. {@link Builder} supports both synchronous and asynchronous
 * generation:
 *
 * <pre>
 * // Synchronous
 * Palette p = Palette.from(bitmap).generate();
 *
 * // Asynchronous
 * Palette.from(bitmap).generate(new PaletteAsyncListener() {
 *     public void onGenerated(Palette p) {
 *         // Use generated instance
 *     }
 * });
 * </pre>
 */
public final class Palette {
    /**
     * Listener to be used with {@link #generateAsync(Bitmap, PaletteAsyncListener)} or
     * {@link #generateAsync(Bitmap, int, PaletteAsyncListener)}
     */
    public interface PaletteAsyncListener {
        /**
         * Called when the {@link Palette} has been generated.
         */
        void onGenerated(Palette palette);
    }
    static final int DEFAULT_RESIZE_BITMAP_AREA = 112 * 112;
    static final int DEFAULT_CALCULATE_NUMBER_COLORS = 16;
    static final float MIN_CONTRAST_TITLE_TEXT = 3.0f;
    static final float MIN_CONTRAST_BODY_TEXT = 4.5f;
    static final String LOG_TAG = "Palette";
    static final boolean LOG_TIMINGS = false;
    /**
     * Start generating a {@link Palette} with the returned {@link Builder} instance.
     */
    public static Builder from(Bitmap bitmap) {
        return new Builder(bitmap);
    }
    /**
     * Generate a {@link Palette} from the pre-generated list of {@link Palette.Swatch} swatches.
     * This is useful for testing, or if you want to resurrect a {@link Palette} instance from a
     * list of swatches. Will return null if the {@code swatches} is null.
     */
    public static Palette from(List<Swatch> swatches) {
        return new Builder(swatches).generate();
    }

    private final List<Swatch> mSwatches;
    private final List<Target> mTargets;
    private final Map<Target, Swatch> mSelectedSwatches;
    private final SparseBooleanArray mUsedColors;
    private final Swatch mDominantSwatch;
    Palette(List<Swatch> swatches, List<Target> targets) {
        mSwatches = swatches;
        mTargets = targets;
        mUsedColors = new SparseBooleanArray();
        mSelectedSwatches = new ArrayMap<>();
        mDominantSwatch = findDominantSwatch();
    }
    /**
     * Returns all of the swatches which make up the palette.
     */
    @NonNull
    public List<Swatch> getSwatches() {
        return Collections.unmodifiableList(mSwatches);
    }
    /**
     * Returns the targets used to generate this palette.
     */
    @NonNull
    public List<Target> getTargets() {
        return Collections.unmodifiableList(mTargets);
    }
    /**
     * Returns the most vibrant swatch in the palette. Might be null.
     *
     * @see Target#VIBRANT
     */
    @Nullable
    public Swatch getVibrantSwatch() {
        return getSwatchForTarget(Target.VIBRANT);
    }
    /**
     * Returns a light and vibrant swatch from the palette. Might be null.
     *
     * @see Target#LIGHT_VIBRANT
     */
    @Nullable
    public Swatch getLightVibrantSwatch() {
        return getSwatchForTarget(Target.LIGHT_VIBRANT);
    }
    /**
     * Returns a dark and vibrant swatch from the palette. Might be null.
     *
     * @see Target#DARK_VIBRANT
     */
    @Nullable
    public Swatch getDarkVibrantSwatch() {
        return getSwatchForTarget(Target.DARK_VIBRANT);
    }
    /**
     * Returns a muted swatch from the palette. Might be null.
     *
     * @see Target#MUTED
     */
    @Nullable
    public Swatch getMutedSwatch() {
        return getSwatchForTarget(Target.MUTED);
    }
    /**
     * Returns a muted and light swatch from the palette. Might be null.
     *
     * @see Target#LIGHT_MUTED
     */
    @Nullable
    public Swatch getLightMutedSwatch() {
        return getSwatchForTarget(Target.LIGHT_MUTED);
    }
    /**
     * Returns a muted and dark swatch from the palette. Might be null.
     *
     * @see Target#DARK_MUTED
     */
    @Nullable
    public Swatch getDarkMutedSwatch() {
        return getSwatchForTarget(Target.DARK_MUTED);
    }
    /**
     * Returns the most vibrant color in the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getVibrantSwatch()
     */
    @ColorInt
    public int getVibrantColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.VIBRANT, defaultColor);
    }
    /**
     * Returns a light and vibrant color from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getLightVibrantSwatch()
     */
    @ColorInt
    public int getLightVibrantColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.LIGHT_VIBRANT, defaultColor);
    }
    /**
     * Returns a dark and vibrant color from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getDarkVibrantSwatch()
     */
    @ColorInt
    public int getDarkVibrantColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.DARK_VIBRANT, defaultColor);
    }
    /**
     * Returns a muted color from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getMutedSwatch()
     */
    @ColorInt
    public int getMutedColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.MUTED, defaultColor);
    }
    /**
     * Returns a muted and light color from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getLightMutedSwatch()
     */
    @ColorInt
    public int getLightMutedColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.LIGHT_MUTED, defaultColor);
    }
    /**
     * Returns a muted and dark color from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getDarkMutedSwatch()
     */
    @ColorInt
    public int getDarkMutedColor(@ColorInt final int defaultColor) {
        return getColorForTarget(Target.DARK_MUTED, defaultColor);
    }
    /**
     * Returns the selected swatch for the given target from the palette, or {@code null} if one
     * could not be found.
     */
    @Nullable
    public Swatch getSwatchForTarget(@NonNull final Target target) {
        return mSelectedSwatches.get(target);
    }
    /**
     * Returns the selected color for the given target from the palette as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     */
    @ColorInt
    public int getColorForTarget(@NonNull final Target target, @ColorInt final int defaultColor) {
        Swatch swatch = getSwatchForTarget(target);
        return swatch != null ? swatch.getRgb() : defaultColor;
    }
    /**
     * Returns the dominant swatch from the palette.
     *
     * <p>The dominant swatch is defined as the swatch with the greatest population (frequency)
     * within the palette.</p>
     */
    @Nullable
    public Swatch getDominantSwatch() {
        return mDominantSwatch;
    }
    /**
     * Returns the color of the dominant swatch from the palette, as an RGB packed int.
     *
     * @param defaultColor value to return if the swatch isn't available
     * @see #getDominantSwatch()
     */
    @ColorInt
    public int getDominantColor(@ColorInt int defaultColor) {
        return mDominantSwatch != null ? mDominantSwatch.getRgb() : defaultColor;
    }
    void generate() {
        // We need to make sure that the scored targets are generated first. This is so that
        // inherited targets have something to inherit from
        for (int i = 0, count = mTargets.size(); i < count; i++) {
            final Target target = mTargets.get(i);
            target.normalizeWeights();
            mSelectedSwatches.put(target, generateScoredTarget(target));
        }
        // We now clear out the used colors
        mUsedColors.clear();
    }
    private Swatch generateScoredTarget(final Target target) {
        final Swatch maxScoreSwatch = getMaxScoredSwatchForTarget(target);
        if (maxScoreSwatch != null && target.isExclusive()) {
            // If we have a swatch, and the target is exclusive, add the color to the used list
            mUsedColors.append(maxScoreSwatch.getRgb(), true);
        }
        return maxScoreSwatch;
    }
    private Swatch getMaxScoredSwatchForTarget(final Target target) {
        float maxScore = 0;
        Swatch maxScoreSwatch = null;
        for (int i = 0, count = mSwatches.size(); i < count; i++) {
            final Swatch swatch = mSwatches.get(i);
            if (shouldBeScoredForTarget(swatch, target)) {
                final float score = generateScore(swatch, target);
                if (maxScoreSwatch == null || score > maxScore) {
                    maxScoreSwatch = swatch;
                    maxScore = score;
                }
            }
        }
        return maxScoreSwatch;
    }
    private boolean shouldBeScoredForTarget(final Swatch swatch, final Target target) {
        // Check whether the HSL values are within the correct ranges, and this color hasn't
        // been used yet.
        final float hsl[] = swatch.getHsl();
        return hsl[1] >= target.getMinimumSaturation() && hsl[1] <= target.getMaximumSaturation()
                && hsl[2] >= target.getMinimumLightness() && hsl[2] <= target.getMaximumLightness()
                && !mUsedColors.get(swatch.getRgb());
    }
    private float generateScore(Swatch swatch, Target target) {
        final float[] hsl = swatch.getHsl();
        float saturationScore = 0;
        float luminanceScore = 0;
        float populationScore = 0;
        final int maxPopulation = mDominantSwatch != null ? mDominantSwatch.getPopulation() : 1;
        if (target.getSaturationWeight() > 0) {
            saturationScore = target.getSaturationWeight()
                    * (1f - Math.abs(hsl[1] - target.getTargetSaturation()));
        }
        if (target.getLightnessWeight() > 0) {
            luminanceScore = target.getLightnessWeight()
                    * (1f - Math.abs(hsl[2] - target.getTargetLightness()));
        }
        if (target.getPopulationWeight() > 0) {
            populationScore = target.getPopulationWeight()
                    * (swatch.getPopulation() / (float) maxPopulation);
        }
        return saturationScore + luminanceScore + populationScore;
    }
    private Swatch findDominantSwatch() {
        int maxPop = Integer.MIN_VALUE;
        Swatch maxSwatch = null;
        for (int i = 0, count = mSwatches.size(); i < count; i++) {
            Swatch swatch = mSwatches.get(i);
            if (swatch.getPopulation() > maxPop) {
                maxSwatch = swatch;
                maxPop = swatch.getPopulation();
            }
        }
        return maxSwatch;
    }
    private static float[] copyHslValues(Swatch color) {
        final float[] newHsl = new float[3];
        System.arraycopy(color.getHsl(), 0, newHsl, 0, 3);
        return newHsl;
    }
}