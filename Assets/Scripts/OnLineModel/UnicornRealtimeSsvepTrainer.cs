using System.Collections.Generic;
using UnityEngine;

public class UnicornRealtimeSsvepTrainer : MonoBehaviour
{
    [System.Serializable]
    public class SsvepClass
    {
        public int classId;
        public float frequencyHz;
        public string label;
    }

    [Header("EEG Settings")]
    public int eegChannels = 8;
    public float nominalSampleRate = 250f;
    public float bufferSeconds = 20f;

    [Header("Training Window")]
    public float trainingWindowSec = 2.0f;
    public int harmonics = 2;

    [Header("Classes")]
    public List<SsvepClass> classes = new List<SsvepClass>()
    {
        new SsvepClass(){ classId = 1, frequencyHz = 8.57f, label = "Left" },
        new SsvepClass(){ classId = 2, frequencyHz = 10.0f, label = "Right" },
        new SsvepClass(){ classId = 3, frequencyHz = 12.0f, label = "Up" },
        new SsvepClass(){ classId = 4, frequencyHz = 15.0f, label = "Down" },
    };

    private RealtimeEegBuffer eegBuffer;
    private OnlineSTClassifier classifier = new OnlineSTClassifier();
    private List<float> targetFreqs = new List<float>();

    private bool trialRunning = false;
    private int currentClassId = -1;
    private float currentTrialStartTime = -1f;

    private int totalPredictions = 0;
    private int correctPredictions = 0;

    private void Awake()
    {
        int capacity = Mathf.CeilToInt(bufferSeconds * nominalSampleRate);
        eegBuffer = new RealtimeEegBuffer(eegChannels, capacity);

        targetFreqs.Clear();
        foreach (var c in classes)
            targetFreqs.Add(c.frequencyHz);
    }

    public void AddSample(float[] eegSample, float timeSec)
    {
        eegBuffer.AddSample(eegSample, timeSec);
    }

    public void StartTrial(int classId)
    {
        currentClassId = classId;
        currentTrialStartTime = Time.time;
        trialRunning = true;

        Debug.Log($"[RealtimeTrainer] Trial START class={classId}, t={currentTrialStartTime:F3}");
    }

    public void EndTrial()
    {
        if (!trialRunning) return;

        float trialEndTime = Time.time;
        float segmentStart = currentTrialStartTime;
        float segmentEnd = Mathf.Min(currentTrialStartTime + trainingWindowSec, trialEndTime);

        if (!eegBuffer.TryGetSegment(segmentStart, segmentEnd, out float[,] eegSegment))
        {
            Debug.LogWarning("[RealtimeTrainer] Failed to extract EEG segment.");
            ResetTrialState();
            return;
        }

        float fsEstimated = EstimateFs(eegSegment.GetLength(1), segmentEnd - segmentStart);
        float[] feat = OnlineSFE.ExtractFeatures(eegSegment, fsEstimated, targetFreqs, harmonics);

        int predBeforeUpdate = classifier.CanPredict() ? classifier.Predict(feat) : -1;

        if (predBeforeUpdate != -1)
        {
            totalPredictions++;
            if (predBeforeUpdate == currentClassId) correctPredictions++;

            float acc = (float)correctPredictions / Mathf.Max(1, totalPredictions);
            Debug.Log($"[RealtimeTrainer] Predict BEFORE update: pred={predBeforeUpdate}, true={currentClassId}, runningAcc={acc:F3}");
        }
        else
        {
            Debug.Log("[RealtimeTrainer] Model not ready yet. First samples will be used only for training.");
        }

        classifier.UpdateModel(currentClassId, feat);

        Debug.Log($"[RealtimeTrainer] Model updated: class={currentClassId}, count={classifier.GetClassCount(currentClassId)}");
        DebugFeature(feat);

        ResetTrialState();
    }

    public int PredictCurrentWindow(float windowSec)
    {
        float endTime = Time.time;
        float startTime = endTime - windowSec;

        if (!eegBuffer.TryGetSegment(startTime, endTime, out float[,] eegSegment))
            return -1;

        if (!classifier.CanPredict())
            return -1;

        float fsEstimated = EstimateFs(eegSegment.GetLength(1), windowSec);
        float[] feat = OnlineSFE.ExtractFeatures(eegSegment, fsEstimated, targetFreqs, harmonics);

        return classifier.Predict(feat);
    }

    private float EstimateFs(int samples, float durationSec)
    {
        if (durationSec <= 1e-6f) return nominalSampleRate;
        return samples / durationSec;
    }

    private void ResetTrialState()
    {
        trialRunning = false;
        currentClassId = -1;
        currentTrialStartTime = -1f;
    }

    private void DebugFeature(float[] feat)
    {
        string s = "[RealtimeTrainer] feat = ";
        for (int i = 0; i < feat.Length; i++)
        {
            s += $"{targetFreqs[i]:F2}Hz:{feat[i]:F3} ";
        }
        Debug.Log(s);
    }
}