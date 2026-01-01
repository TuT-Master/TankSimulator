using System;
using System.Linq;
using UnityEngine;

public static class FeatureExtractor
{
    // 16 frequency bands for finer resolution (up to ~8kHz)
    private static readonly float[] bands =
    {
        150, 300, 450, 600, 900, 1200, 1500, 1800,
        2100, 2400, 3000, 3600, 4200, 4800, 6000, 7200
    };

    // Extracts 32 features (16 bands × 2 halves)
    public static float[] ExtractFeature(float[] samples, int sampleRate)
    {
        if (samples == null || samples.Length == 0)
            return new float[bands.Length * 2];

        // Normalize length to fixed 0.5 seconds (8k samples at 16kHz)
        int targetLen = Mathf.RoundToInt(0.5f * sampleRate);
        samples = NormalizeLength(samples, targetLen);

        // Split clip into two halves
        int half = samples.Length / 2;
        float[] firstHalf = samples.Take(half).ToArray();
        float[] secondHalf = samples.Skip(half).ToArray();

        // Extract for both halves
        float[] feat1 = ExtractHalf(firstHalf, sampleRate);
        float[] feat2 = ExtractHalf(secondHalf, sampleRate);

        // Combine into single 32-float vector
        return feat1.Concat(feat2).ToArray();
    }

    private static float[] ExtractHalf(float[] samples, int sampleRate)
    {
        int N = Mathf.RoundToInt(0.032f * sampleRate); // ~32 ms window
        int H = N / 2;
        float[] win = new float[N];
        for (int n = 0; n < N; n++)
            win[n] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * n / (N - 1)));

        float[] acc = new float[bands.Length];
        int frameCount = 0;

        for (int start = 0; start + N <= samples.Length; start += H)
        {
            frameCount++;
            for (int b = 0; b < bands.Length; b++)
                acc[b] += GoertzelMag2(samples, start, N, sampleRate, bands[b], win);
        }

        // Average, log-compress, normalize
        if (frameCount == 0) frameCount = 1;
        for (int b = 0; b < acc.Length; b++)
            acc[b] = Mathf.Log10(1e-6f + acc[b] / frameCount);

        Normalize(acc);
        return acc;
    }

    private static float GoertzelMag2(float[] x, int offset, int N, int fs, float freq, float[] win)
    {
        float k = (int)(0.5f + (N * freq) / fs);
        float w = 2f * Mathf.PI * k / N;
        float cw = Mathf.Cos(w);
        float coeff = 2f * cw;

        float s0 = 0f, s1 = 0f, s2 = 0f;
        for (int n = 0; n < N; n++)
        {
            float xn = x[offset + n] * win[n];
            s0 = coeff * s1 - s2 + xn;
            s2 = s1;
            s1 = s0;
        }
        float real = s1 - s2 * cw;
        float imag = s2 * Mathf.Sin(w);
        return real * real + imag * imag;
    }

    private static void Normalize(float[] v)
    {
        float sumsq = v.Sum(x => x * x);
        float norm = Mathf.Sqrt(Mathf.Max(sumsq, 1e-8f));
        for (int i = 0; i < v.Length; i++)
            v[i] /= norm;
    }

    private static float[] NormalizeLength(float[] samples, int targetLen)
    {
        if (samples.Length == targetLen) return samples;
        if (samples.Length > targetLen)
            return samples.Take(targetLen).ToArray();

        // Pad with zeros if too short
        float[] padded = new float[targetLen];
        Array.Copy(samples, padded, samples.Length);
        return padded;
    }

    public static float Cosine(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return 0f;

        float dot = 0f, aa = 0f, bb = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            aa += a[i] * a[i];
            bb += b[i] * b[i];
        }

        return dot / (Mathf.Sqrt(aa) * Mathf.Sqrt(bb) + 1e-8f);
    }
}
