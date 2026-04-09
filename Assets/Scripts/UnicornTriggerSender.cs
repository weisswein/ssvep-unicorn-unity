using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UnicornTriggerSender : MonoBehaviour
{
    [Header("Recorder Trigger Input")]
    public string ipAddress = "127.0.0.1";
    public int port = 1000;

    private UdpClient udpClient;
    private IPEndPoint endPoint;

    private void Awake()
    {
        endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient = new UdpClient();
    }

    public void SendTrigger(int triggerValue)
    {
        string msg = triggerValue.ToString();
        byte[] data = Encoding.ASCII.GetBytes(msg);
        udpClient.Send(data, data.Length, endPoint);
        Debug.Log($"[Trigger] Sent: {triggerValue}");
    }

    private void OnDestroy()
    {
        udpClient?.Close();
    }
}