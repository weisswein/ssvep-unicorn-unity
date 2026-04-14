using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public int eegChannels = 3;
    public float nominalSampleRate = 250f;
    public float bufferSeconds = 20f;

    [Header("Predict result text")]
    public Text predict_text;

    [Header("Training Window")]
    public float trainingWindowSec = 2.0f;
    public int harmonics = 2;

    [Header("Accuracy Display")]
    public int minSamplesPerClassForAcc = 10;

    [Header("Classes")]
    public List<SsvepClass> classes = new List<SsvepClass>()
    {
        new SsvepClass(){ classId = 1, frequencyHz = 8.57f, label = "Left" },
        new SsvepClass(){ classId = 2, frequencyHz = 10.0f, label = "Right" },
        new SsvepClass(){ classId = 3, frequencyHz = 12.0f, label = "Up" },
        new SsvepClass(){ classId = 4, frequencyHz = 15.0f, label = "Down" },
    };

    private RealtimeEegBuffer eegBuffer;
    private List<float> targetFreqs = new List<float>();

    private bool trialRunning = false;
    private int currentClassId = -1;
    private float currentTrialStartTime = -1f;

    private int totalPredictions = 0;
    private int correctPredictions = 0;

    // キャリブレーション進捗管理用
    private Dictionary<int, int> classTrialCounts = new Dictionary<int, int>();

    private void Awake()
    {
        int capacity = Mathf.CeilToInt(bufferSeconds * nominalSampleRate);
        eegBuffer = new RealtimeEegBuffer(eegChannels, capacity);

        targetFreqs.Clear();
        classTrialCounts.Clear();

        foreach (var c in classes)
        {
            targetFreqs.Add(c.frequencyHz);
            classTrialCounts[c.classId] = 0;
        }

        Debug.Log($"[RealtimeTrainer] Using EEG channels: {eegChannels}");
    }

    public void AddSample(float[] eegSample, float timeSec)
    {
        eegBuffer.AddSample(eegSample, timeSec);
    }

    public void StartTrial(int classId)
    {
        if (predict_text != null)
            predict_text.text = "";

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

            if (predict_text != null)
                predict_text.text = "EEG segment extraction failed";

            ResetTrialState();
            return;
        }

        float fsEstimated = EstimateFs(eegSegment.GetLength(1), segmentEnd - segmentStart);

        // CCA特徴（各周波数との相関）
        float[] feat = OnlineSFE.ExtractFeatures(eegSegment, fsEstimated, targetFreqs, harmonics);

        // 最大相関のクラスを選ぶ
        int predIndex = OnlineSFE.PredictClass(eegSegment, fsEstimated, targetFreqs, harmonics);
        int predBeforeUpdate = (predIndex >= 0 && predIndex < classes.Count)
            ? classes[predIndex].classId
            : -1;

        // 今回trialをキャリブレーション回数に加算
        if (classTrialCounts.ContainsKey(currentClassId))
            classTrialCounts[currentClassId]++;
        else
            classTrialCounts[currentClassId] = 1;

        bool accReady = IsAccReady();

        // ACCは各クラス試行回数が閾値に達してから計算
        if (predBeforeUpdate != -1 && accReady)
        {
            totalPredictions++;

            if (predBeforeUpdate == currentClassId)
                correctPredictions++;
        }

        float acc = (float)correctPredictions / Mathf.Max(1, totalPredictions);

        if (predBeforeUpdate != -1)
        {
            Debug.Log($"[RealtimeTrainer] CCA Predict: pred={predBeforeUpdate}, true={currentClassId}, runningAcc={acc:F3}");
        }
        else
        {
            Debug.Log("[RealtimeTrainer] Prediction failed.");
        }

        if (predict_text != null)
        {
            string accText;

            if (accReady)
            {
                accText = $"{acc:P1}";
            }
            else
            {
                accText = $"Calibration ({GetMinimumClassCount()}/{minSamplesPerClassForAcc})";
            }

            string predLabel = predBeforeUpdate != -1
                ? GetLabelByClassId(predBeforeUpdate)
                : "Unknown";

            predict_text.text =
                $"True : {GetLabelByClassId(currentClassId)}\n" +
                $"Pred : {predLabel}\n" +
                $"Acc  : {accText}";
        }

        DebugFeature(feat);
        ResetTrialState();
    }

    public int PredictCurrentWindow(float windowSec)
    {
        float endTime = Time.time;
        float startTime = endTime - windowSec;

        if (!eegBuffer.TryGetSegment(startTime, endTime, out float[,] eegSegment))
            return -1;

        float fsEstimated = EstimateFs(eegSegment.GetLength(1), windowSec);

        int predIndex = OnlineSFE.PredictClass(eegSegment, fsEstimated, targetFreqs, harmonics);
        if (predIndex < 0 || predIndex >= classes.Count)
            return -1;

        return classes[predIndex].classId;
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
        string s = "[RealtimeTrainer] cca = ";
        for (int i = 0; i < feat.Length; i++)
        {
            s += $"{targetFreqs[i]:F2}Hz:{feat[i]:F3} ";
        }
        Debug.Log(s);
    }

    private string GetLabelByClassId(int classId)
    {
        foreach (var c in classes)
        {
            if (c.classId == classId)
                return c.label;
        }
        return $"Class {classId}";
    }

    private bool IsAccReady()
    {
        foreach (var c in classes)
        {
            if (!classTrialCounts.ContainsKey(c.classId))
                return false;

            if (classTrialCounts[c.classId] < minSamplesPerClassForAcc)
                return false;
        }
        return true;
    }

    private int GetMinimumClassCount()
    {
        int minCount = int.MaxValue;

        foreach (var c in classes)
        {
            int count = classTrialCounts.ContainsKey(c.classId) ? classTrialCounts[c.classId] : 0;
            if (count < minCount)
                minCount = count;
        }

        return minCount == int.MaxValue ? 0 : minCount;
    }
}