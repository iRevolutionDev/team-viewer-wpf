using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Server.Models;

namespace Client.Services;

public class Client
{
    public delegate void OnReceive(string sender, string eventName, string data);

    public delegate void OnReceiveRaw(string sender, string data);

    private static Client? _instance;

    private readonly string ip;
    private readonly int port;

    public Client(string ip, int port)
    {
        Udp = new UdpClient();
        try
        {
            Udp.Connect(IPAddress.Parse(ip), port);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Erro ao conectar ao servidor: {e.Message}");
        }

        this.ip = ip;
        this.port = port;

        Instance = this;
    }

    public static Client? Instance
    {
        get => _instance;
        private set
        {
            if (_instance != null) throw new Exception("Client already exists");
            _instance = value;
        }
    }

    public UdpClient Udp { get; }

    public string? LocalEndPoint => Udp.Client.LocalEndPoint?.ToString();
    public event OnReceive? OnReceiveEvent;
    public event OnReceiveRaw? OnReceivePacket;

    public void SendEvent(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        Udp.Send(bytes, bytes.Length);
    }

    public void SendChunk(string target, string eventName, string chunk, int chunkSize, int size, int fragmentId, int sequence)
    {
        var jsonObject = new
        {
            EventName = $"{eventName}_chunk",
            Data = chunk,
            ChunkSize = chunkSize,
            Size = size,
            FragmentId = fragmentId,
            Sequence = sequence,
            Target = target
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        Udp.Send(bytes, bytes.Length);
    }

    public void SendEvent<T>(string eventName, T data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        Udp.Send(bytes, bytes.Length);
    }

    public async void SendEventAsync(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        await Udp.SendAsync(bytes, bytes.Length);
    }

    public void Start()
    {
        while (true)
            try
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = Udp.Receive(ref ipEndPoint);
                var content = Encoding.UTF8.GetString(data);

                var json = JsonConvert.DeserializeObject<ReceiveEvent>(content);

                OnReceiveEvent?.Invoke(json?.Sender ?? string.Empty, json?.EventName ?? string.Empty,
                    json?.Data ?? string.Empty);

                OnReceivePacket?.Invoke(ipEndPoint.ToString(), content);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Erro ao receber dados do servidor: {e.Message}");
            }
    }
}