using UnityEngine;

[System.Serializable]
public class RealtimeEegBuffer
{
    private float[,] data;   // [channel, index]
    private float[] times;   // sample timestamp
    private int channels;
    private int capacity;
    private int writeIndex = 0;
    private int count = 0;

    public int Channels => channels;
    public int Capacity => capacity;
    public int Count => count;

    public RealtimeEegBuffer(int channels, int capacity)
    {
        this.channels = channels;
        this.capacity = capacity;
        data = new float[channels, capacity];
        times = new float[capacity];
    }

    public void AddSample(float[] sample, float timeSec)
    {
        if (sample == null || sample.Length != channels) return;

        for (int ch = 0; ch < channels; ch++)
            data[ch, writeIndex] = sample[ch];

        times[writeIndex] = timeSec;

        writeIndex = (writeIndex + 1) % capacity;
        count = Mathf.Min(count + 1, capacity);
    }

    public bool TryGetSegment(float startTime, float endTime, out float[,] segment)
    {
        segment = null;
        if (count <= 0 || endTime <= startTime) return false;

        int[] indices = GetIndicesInRange(startTime, endTime);
        if (indices.Length <= 1) return false;

        segment = new float[channels, indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            for (int ch = 0; ch < channels; ch++)
                segment[ch, i] = data[ch, idx];
        }
        return true;
    }

    private int[] GetIndicesInRange(float startTime, float endTime)
    {
        System.Collections.Generic.List<int> idxs = new System.Collections.Generic.List<int>();

        int oldestIndex = (writeIndex - count + capacity) % capacity;

        for (int k = 0; k < count; k++)
        {
            int idx = (oldestIndex + k) % capacity;
            float t = times[idx];
            if (t >= startTime && t <= endTime)
                idxs.Add(idx);
        }

        return idxs.ToArray();
    }
}