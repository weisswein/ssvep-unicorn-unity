using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class OnlineSTClassifier
{
    private Dictionary<int, float[]> classMeans = new Dictionary<int, float[]>();
    private Dictionary<int, int> classCounts = new Dictionary<int, int>();

    [System.Serializable]
    private class ModelEntry
    {
        public int classId;
        public float[] mean;
        public int count;
    }

    [System.Serializable]
    private class ModelData
    {
        public List<ModelEntry> entries = new List<ModelEntry>();
    }

    public void UpdateModel(int classId, float[] feature)
    {
        if (!classMeans.ContainsKey(classId))
        {
            classMeans[classId] = (float[])feature.Clone();
            classCounts[classId] = 1;
            return;
        }

        int n = classCounts[classId];
        float[] mean = classMeans[classId];

        for (int i = 0; i < mean.Length; i++)
        {
            mean[i] = (mean[i] * n + feature[i]) / (n + 1);
        }

        classCounts[classId] = n + 1;
    }

    public bool CanPredict()
    {
        return classMeans.Count > 0;
    }

    public int Predict(float[] feature)
    {
        int bestClass = -1;
        float bestScore = float.NegativeInfinity;

        foreach (var kv in classMeans)
        {
            float sim = CosineSimilarity(feature, kv.Value);
            if (sim > bestScore)
            {
                bestScore = sim;
                bestClass = kv.Key;
            }
        }

        return bestClass;
    }

    public float PredictScore(float[] feature, int classId)
    {
        if (!classMeans.ContainsKey(classId)) return float.NegativeInfinity;
        return CosineSimilarity(feature, classMeans[classId]);
    }

    public int GetClassCount(int classId)
    {
        return classCounts.ContainsKey(classId) ? classCounts[classId] : 0;
    }

    public void SaveModel(string filePath)
    {
        ModelData modelData = new ModelData();

        foreach (var kv in classMeans)
        {
            ModelEntry entry = new ModelEntry();
            entry.classId = kv.Key;
            entry.mean = kv.Value;
            entry.count = classCounts.ContainsKey(kv.Key) ? classCounts[kv.Key] : 0;
            modelData.entries.Add(entry);
        }

        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(modelData, true);
        File.WriteAllText(filePath, json);
    }

    public bool LoadModel(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        string json = File.ReadAllText(filePath);
        ModelData modelData = JsonUtility.FromJson<ModelData>(json);

        classMeans.Clear();
        classCounts.Clear();

        if (modelData == null || modelData.entries == null)
            return false;

        foreach (var entry in modelData.entries)
        {
            classMeans[entry.classId] = entry.mean;
            classCounts[entry.classId] = entry.count;
        }

        return true;
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