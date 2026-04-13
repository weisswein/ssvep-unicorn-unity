using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SsvepTemplateClassifier
{
    private Dictionary<int, float[]> classTemplates = new Dictionary<int, float[]>();
    private List<float> targetFreqs = new List<float>();

    public void Train(List<SsvepEpoch> epochs, List<float> freqs)
    {
        targetFreqs = new List<float>(freqs);
        classTemplates.Clear();

        Dictionary<int, List<float[]>> grouped = new Dictionary<int, List<float[]>>();

        foreach (var epoch in epochs)
        {
            float[] feat = SsvepFeatureExtractor.ExtractFrequencyFeatures(
                epoch.eeg,
                epoch.sampleRate,
                targetFreqs
            );

            if (!grouped.ContainsKey(epoch.classId))
                grouped[epoch.classId] = new List<float[]>();

            grouped[epoch.classId].Add(feat);
        }

        foreach (var kv in grouped)
        {
            classTemplates[kv.Key] = MeanVector(kv.Value);
        }
    }

    public int Predict(float[,] eeg, float sampleRate)
    {
        float[] feat = SsvepFeatureExtractor.ExtractFrequencyFeatures(eeg, sampleRate, targetFreqs);

        int bestClass = -1;
        float bestScore = float.NegativeInfinity;

        foreach (var kv in classTemplates)
        {
            float sim = CosineSimilarity(feat, kv.Value);
            if (sim > bestScore)
            {
                bestScore = sim;
                bestClass = kv.Key;
            }
        }

        return bestClass;
    }

    public float PredictScore(float[,] eeg, float sampleRate, int classId)
    {
        if (!classTemplates.ContainsKey(classId)) return -999f;

        float[] feat = SsvepFeatureExtractor.ExtractFrequencyFeatures(eeg, sampleRate, targetFreqs);
        return CosineSimilarity(feat, classTemplates[classId]);
    }

    private float[] MeanVector(List<float[]> vectors)
    {
        int dim = vectors[0].Length;
        float[] mean = new float[dim];

        foreach (var v in vectors)
        {
            for (int i = 0; i < dim; i++)
                mean[i] += v[i];
        }

        for (int i = 0; i < dim; i++)
            mean[i] /= vectors.Count;

        return mean;
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0f, na = 0f, nb = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return dot / (Mathf.Sqrt(na) * Mathf.Sqrt(nb) + 1e-8f);
    }
}