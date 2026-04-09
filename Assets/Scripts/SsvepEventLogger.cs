using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class SsvepEventLogger : MonoBehaviour
{
    public string filePrefix = "unity_ssvep_events";

    private StreamWriter writer;
    private string filePath;

    private void Start()
    {
        string dir = Path.Combine(Application.persistentDataPath, "UnicornData");
        Directory.CreateDirectory(dir);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        filePath = Path.Combine(dir, $"{filePrefix}_{stamp}.csv");

        writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("time,trial_id,event,class_id,freq_hz");
        writer.Flush();

        Debug.Log($"[EventLogger] {filePath}");
    }

    public void LogEvent(int trialId, string eventName, int classId, float freqHz)
    {
        double t = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        string line =
            t.ToString(CultureInfo.InvariantCulture) + "," +
            trialId + "," +
            eventName + "," +
            classId + "," +
            freqHz.ToString(CultureInfo.InvariantCulture);

        writer.WriteLine(line);
        writer.Flush();

        Debug.Log($"[Event] {line}");
    }

    private void OnDestroy()
    {
        writer?.Flush();
        writer?.Close();
    }
}