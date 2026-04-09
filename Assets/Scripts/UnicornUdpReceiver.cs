using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UnicornUdpReceiver : MonoBehaviour
{
    [Header("Recorder Data Output")]
    public string listenIp = "127.0.0.1";
    public int listenPort = 1001;

    [Header("Save")]
    public bool saveCsv = true;
    public string filePrefix = "unity_unicorn_raw";

    [Header("Debug")]
    public bool logEvery100Packets = true;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;

    private string csvPath;
    private string rawPacketLogPath;
    private StreamWriter csvWriter;
    private StreamWriter rawWriter;
    private bool headerWritten = false;
    private int packetCount = 0;

    private readonly ConcurrentQueue<float[]> sampleQueue = new ConcurrentQueue<float[]>();

    public int LatestChannelCount { get; private set; } = 0;

    private void Start()
    {
        string dir = Path.Combine(Application.persistentDataPath, "UnicornData");
        Directory.CreateDirectory(dir);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        csvPath = Path.Combine(dir, $"{filePrefix}_{stamp}.csv");
        rawPacketLogPath = Path.Combine(dir, $"{filePrefix}_{stamp}_packets.txt");

        if (saveCsv)
        {
            csvWriter = new StreamWriter(csvPath, false, Encoding.UTF8);
            rawWriter = new StreamWriter(rawPacketLogPath, false, Encoding.UTF8);
        }

        udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(listenIp), listenPort));

        running = true;
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log($"[UDP Receiver] Listening on {listenIp}:{listenPort}");
        Debug.Log($"[UDP Receiver] CSV: {csvPath}");
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string text = Encoding.UTF8.GetString(data).Trim();
                double pcTime = GetUnixTimeSeconds();

                if (saveCsv)
                {
                    rawWriter.WriteLine($"{pcTime.ToString(CultureInfo.InvariantCulture)}\t{text}");
                    rawWriter.Flush();
                }

                float[] values = TryParsePayload(text);
                if (values == null) continue;

                LatestChannelCount = values.Length;
                sampleQueue.Enqueue(values);

                if (saveCsv)
                {
                    if (!headerWritten)
                    {
                        StringBuilder header = new StringBuilder("pc_time");
                        for (int i = 0; i < values.Length; i++)
                            header.Append($",sig_{i}");
                        csvWriter.WriteLine(header.ToString());
                        csvWriter.Flush();
                        headerWritten = true;
                    }

                    StringBuilder line = new StringBuilder();
                    line.Append(pcTime.ToString(CultureInfo.InvariantCulture));
                    for (int i = 0; i < values.Length; i++)
                    {
                        line.Append(",");
                        line.Append(values[i].ToString(CultureInfo.InvariantCulture));
                    }

                    csvWriter.WriteLine(line.ToString());
                    csvWriter.Flush();
                }

                packetCount++;
                if (logEvery100Packets && packetCount % 100 == 0)
                {
                    Debug.Log($"[UDP Receiver] packets={packetCount}, channels={values.Length}");
                }
            }
            catch (SocketException)
            {
                if (!running) break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UDP Receiver] {ex.Message}");
            }
        }
    }

    private float[] TryParsePayload(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        char sep = text.Contains(",") ? ',' :
                   text.Contains(";") ? ';' : '\0';

        if (sep == '\0') return null;

        string[] parts = text.Split(sep);
        float[] values = new float[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
            {
                return null;
            }
        }

        return values;
    }

    public bool TryDequeueSample(out float[] sample)
    {
        return sampleQueue.TryDequeue(out sample);
    }

    public static double GetUnixTimeSeconds()
    {
        return (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
    }

    private void OnDestroy()
    {
        running = false;

        udpClient?.Close();

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Join(200);

        csvWriter?.Flush();
        csvWriter?.Close();

        rawWriter?.Flush();
        rawWriter?.Close();
    }
}