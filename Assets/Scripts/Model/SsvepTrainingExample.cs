using System.Collections.Generic;
using UnityEngine;

public class SsvepTrainingExample : MonoBehaviour
{
    public List<SsvepEpoch> allEpochs = new List<SsvepEpoch>();

    public void TrainForWindow(float windowSec)
    {
        List<SsvepEpoch> cropped = new List<SsvepEpoch>();

        foreach (var ep in allEpochs)
        {
            if (ep.samples >= Mathf.FloorToInt(windowSec * ep.sampleRate))
            {
                cropped.Add(SsvepEpochUtils.CropEpoch(ep, windowSec));
            }
        }

        var freqs = new List<float>() { 8.57f, 10.0f, 12.0f, 15.0f };

        SsvepTemplateClassifier clf = new SsvepTemplateClassifier();
        clf.Train(cropped, freqs);

        int correct = 0;
        for (int i = 0; i < cropped.Count; i++)
        {
            int pred = clf.Predict(cropped[i].eeg, cropped[i].sampleRate);
            if (pred == cropped[i].classId)
                correct++;
        }

        float acc = (float)correct / cropped.Count;
        Debug.Log($"Window={windowSec:F2}s, Accuracy={acc:F3}");
    }
}