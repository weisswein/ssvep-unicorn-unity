using System;
using System.Collections.Generic;

[Serializable]
public class SsvepEpoch
{
    public int classId;
    public float frequencyHz;
    public float[,] eeg; // [channel, sample]
    public int channels;
    public int samples;
    public float sampleRate;

    public SsvepEpoch(int classId, float frequencyHz, float[,] eeg, float sampleRate)
    {
        this.classId = classId;
        this.frequencyHz = frequencyHz;
        this.eeg = eeg;
        this.sampleRate = sampleRate;
        this.channels = eeg.GetLength(0);
        this.samples = eeg.GetLength(1);
    }
}