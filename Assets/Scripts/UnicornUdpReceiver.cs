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
    [Header("Recorder UDP Data Output")]
    public int listenPort = 1001;

    [Header("Save")]
    public bool saveRawAsciiLog = true;
    public bool saveParsedCsv = true;
    public string filePrefix = "unity_unicorn_raw";

    [Header("Debug")]
    public bool printFirstPackets = true;
    public int printPacketCount = 5;
    public bool printEvery100Packets = true;

    private Socket socket;
    private Thread receiveThread;
    private volatile bool running;

    private string baseDir;
    private string rawLogPath;
    private string csvPath;

    private StreamWriter rawLogWriter;
    private StreamWriter csvWriter;

    private bool csvHeaderWritten = false;
    private int packetCount = 0;

    private readonly ConcurrentQueue<float[]> sampleQueue = new ConcurrentQueue<float[]>();

    public int LatestSignalCount { get; private set; }
    public string LatestRawText { get; private set; }

    private void Start()
    {
        baseDir = Path.Combine(Application.persistentDataPath, "UnicornData");
        Directory.CreateDirectory(baseDir);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rawLogPath = Path.Combine(baseDir, $"{filePrefix}_{stamp}_raw.txt");
        csvPath = Path.Combine(baseDir, $"{filePrefix}_{stamp}.csv");

        if (saveRawAsciiLog)
        {
            rawLogWriter = new StreamWriter(rawLogPath, false, Encoding.UTF8);
            rawLogWriter.WriteLine("# pc_time_unix\tpayload");
            rawLogWriter.Flush();
        }

        if (saveParsedCsv)
        {
            csvWriter = new StreamWriter(csvPath, false, Encoding.UTF8);
        }

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listenPort);

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endPoint);

        running = true;
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log($"[UnicornUdpReceiver] Listening on UDP port {listenPort}");
        Debug.Log($"[UnicornUdpReceiver] Save dir: {baseDir}");
        Debug.Log($"[UnicornUdpReceiver] Raw log: {rawLogPath}");
        Debug.Log($"[UnicornUdpReceiver] Parsed CSV: {csvPath}");
    }

    private void ReceiveLoop()
    {
        byte[] receiveBufferByte = new byte[1024];

        while (running)
        {
            try
            {
                int numberOfBytesReceived = socket.Receive(receiveBufferByte);
                if (numberOfBytesReceived <= 0) continue;

                // g.tec サンプルに合わせ、受信したバイト列を ASCII 文字列として扱う
                string text = Encoding.ASCII.GetString(receiveBufferByte, 0, numberOfBytesReceived).Trim('\0', '\r', '\n', ' ', '\t');
                double pcTime = GetUnixTimeSeconds();

                LatestRawText = text;
                packetCount++;

                if (printFirstPackets && packetCount <= printPacketCount)
                {
                    Debug.Log($"[UnicornUdpReceiver] Packet {packetCount}: {text}");
                }
                else if (printEvery100Packets && packetCount % 100 == 0)
                {
                    Debug.Log($"[UnicornUdpReceiver] packets={packetCount}, latest='{text}'");
                }

                if (saveRawAsciiLog && rawLogWriter != null)
                {
                    rawLogWriter.WriteLine($"{pcTime.ToString(CultureInfo.InvariantCulture)}\t{text}");
                    rawLogWriter.Flush();
                }

                float[] parsed = TryParseAsciiRecord(text);
                if (parsed == null)
                {
                    Array.Clear(receiveBufferByte, 0, receiveBufferByte.Length);
                    continue;
                }

                LatestSignalCount = parsed.Length;
                sampleQueue.Enqueue(parsed);

                if (saveParsedCsv && csvWriter != null)
                {
                    if (!csvHeaderWritten)
                    {
                        WriteCsvHeader(parsed.Length);
                        csvHeaderWritten = true;
                    }

                    WriteCsvRow(pcTime, parsed);
                }

                Array.Clear(receiveBufferByte, 0, receiveBufferByte.Length);
            }
            catch (SocketException)
            {
                if (!running) break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnicornUdpReceiver] ReceiveLoop error: {ex.Message}");
            }
        }
    }

    private float[] TryParseAsciiRecord(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Recorder の ASCII レコードに合わせて、まず区切り候補を探す
        char separator = '\0';
        if (text.Contains(",")) separator = ',';
        else if (text.Contains(";")) separator = ';';
        else if (text.Contains("\t")) separator = '\t';
        else return null;

        string[] parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

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

    private void WriteCsvHeader(int signalCount)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("pc_time_unix");
        for (int i = 0; i < signalCount; i++)
        {
            sb.Append($",sig_{i}");
        }

        csvWriter.WriteLine(sb.ToString());
        csvWriter.Flush();
    }

    private void WriteCsvRow(double pcTime, float[] values)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(pcTime.ToString(CultureInfo.InvariantCulture));

        for (int i = 0; i < values.Length; i++)
        {
            sb.Append(",");
            sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
        }

        csvWriter.WriteLine(sb.ToString());
        csvWriter.Flush();
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

        try
        {
            socket?.Close();
        }
        catch
        {
            // ignore
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }

        rawLogWriter?.Flush();
        rawLogWriter?.Close();

        csvWriter?.Flush();
        csvWriter?.Close();
    }
}