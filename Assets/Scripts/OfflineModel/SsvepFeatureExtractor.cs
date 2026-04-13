using System;
using System.Collections.Generic;
using UnityEngine;

public static class SsvepFeatureExtractor
{
    public static float[] ExtractFrequencyFeatures(
        float[,] eeg,
        float sampleRate,
        List<float> targetFreqs
    )
    {
        int chCount = eeg.GetLength(0);
        int n = eeg.GetLength(1);

        float[] features = new float[targetFreqs.Count];

        for (int fIdx = 0; fIdx < targetFreqs.Count; fIdx++)
        {
            float freq = targetFreqs[fIdx];
            float scoreSum = 0f;

            for (int ch = 0; ch < chCount; ch++)
            {
                float[] signal = GetChannel(eeg, ch);
                float score = ComputeSinCosCorrelationPower(signal, sampleRate, freq);
                scoreSum += score;
            }

            features[fIdx] = scoreSum / chCount;
        }

        return features;
    }

    private static float[] GetChannel(float[,] eeg, int ch)
    {
        int n = eeg.GetLength(1);
        float[] x = new float[n];
        for (int i = 0; i < n; i++)
            x[i] = eeg[ch, i];
        return x;
    }

    private static float ComputeSinCosCorrelationPower(float[] signal, float fs, float freq)
    {
        int n = signal.Length;

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

        float denomSin = Mathf.Sqrt(ss * sinNorm) + 1e-8f;
        float denomCos = Mathf.Sqrt(ss * cosNorm) + 1e-8f;

        float rSin = corrSin / denomSin;
        float rCos = corrCos / denomCos;

        return rSin * rSin + rCos * rCos;
    }
}