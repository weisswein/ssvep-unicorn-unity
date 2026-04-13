using System.Collections.Generic;
using UnityEngine;

public static class SsvepTrainTest
{
    public static void EvaluateHoldout(List<SsvepEpoch> allEpochs, float windowSec)
    {
        List<SsvepEpoch> cropped = new List<SsvepEpoch>();
        foreach (var ep in allEpochs)
        {
            if (ep.samples >= Mathf.FloorToInt(windowSec * ep.sampleRate))
                cropped.Add(SsvepEpochUtils.CropEpoch(ep, windowSec));
        }

        Shuffle(cropped);

        int split = Mathf.FloorToInt(cropped.Count * 0.8f);
        List<SsvepEpoch> train = cropped.GetRange(0, split);
        List<SsvepEpoch> test = cropped.GetRange(split, cropped.Count - split);

        var freqs = new List<float>() { 8.57f, 10.0f, 12.0f, 15.0f };

        SsvepTemplateClassifier clf = new SsvepTemplateClassifier();
        clf.Train(train, freqs);

        int correct = 0;
        foreach (var ep in test)
        {
            int pred = clf.Predict(ep.eeg, ep.sampleRate);
            if (pred == ep.classId)
                correct++;
        }

        float acc = test.Count > 0 ? (float)correct / test.Count : 0f;
        Debug.Log($"[Holdout] Window={windowSec:F2}s, Test Accuracy={acc:F3}");
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}