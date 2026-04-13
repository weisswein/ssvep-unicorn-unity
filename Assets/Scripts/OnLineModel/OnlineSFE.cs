using System.Collections.Generic;
using UnityEngine;

public static class OnlineSFE
{
    public static float[] ExtractFeatures(float[,] eeg, float fs, List<float> targetFreqs, int harmonics = 2)
    {
        int chCount = eeg.GetLength(0);
        int n = eeg.GetLength(1);

        float[] feat = new float[targetFreqs.Count];

        for (int fIdx = 0; fIdx < targetFreqs.Count; fIdx++)
        {
            float freq = targetFreqs[fIdx];
            float sum = 0f;

            for (int ch = 0; ch < chCount; ch++)
            {
                float[] signal = new float[n];
                for (int i = 0; i < n; i++)
                    signal[i] = eeg[ch, i];

                sum += ComputeHarmonicPower(signal, fs, freq, harmonics);
            }

            feat[fIdx] = sum / Mathf.Max(1, chCount);
        }

        return Normalize(feat);
    }

    private static float ComputeHarmonicPower(float[] signal, float fs, float freq, int harmonics)
    {
        float total = 0f;
        for (int h = 1; h <= harmonics; h++)
        {
            total += ComputeSinCosCorrelationPower(signal, fs, freq * h);
        }
        return total;
    }

    private static float ComputeSinCosCorrelationPower(float[] signal, float fs, float freq)
    {
        int n = signal.Length;
        if (n <= 1) return 0f;

        float mean = 0f;
        for (int i = 0; i < n; i++) mean += signal[i];
        mean /= n;

        float ss = 0f;
        float corrSin = 0f;
        float corrCos = 0f;
        float sinNorm = 0f;
        float cosNorm = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / fs;
            float x = signal[i] - mean;

            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            float c = Mathf.Cos(2f * Mathf.PI * freq * t);

            corrSin += x * s;
            corrCos += x * c;
            sinNorm += s * s;
            cosNorm += c * c;
            ss += x * x;
        }

        float rSin = corrSin / (Mathf.Sqrt(ss * sinNorm) + 1e-8f);
        float rCos = corrCos / (Mathf.Sqrt(ss * cosNorm) + 1e-8f);

        return rSin * rSin + rCos * rCos;
    }

    private static float[] Normalize(float[] x)
    {
        float sum = 0f;
        for (int i = 0; i < x.Length; i++) sum += x[i];
        if (sum < 1e-8f) return x;

        float[] y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = x[i] / sum;
        return y;
    }
}