using System;
using System.Collections.Generic;
using UnityEngine;

public static class SsvepEpochUtils
{
    public static SsvepEpoch CropEpoch(SsvepEpoch src, float windowSec)
    {
        int cropSamples = Mathf.FloorToInt(windowSec * src.sampleRate);
        cropSamples = Mathf.Min(cropSamples, src.samples);

        float[,] cropped = new float[src.channels, cropSamples];

        for (int ch = 0; ch < src.channels; ch++)
        {
            for (int i = 0; i < cropSamples; i++)
            {
                cropped[ch, i] = src.eeg[ch, i];
            }
        }

        return new SsvepEpoch(src.classId, src.frequencyHz, cropped, src.sampleRate);
    }
}