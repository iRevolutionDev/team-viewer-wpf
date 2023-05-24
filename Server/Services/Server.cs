using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Server.Models;
using Server.Utils;

namespace Server.Services;

public class Server
{
    public delegate void OnReceive(IPEndPoint sender, string eventName, string data);

    public delegate void OnReceiveRaw(IPEndPoint sender, string data);

    private readonly int _port;

    public Server(int port)
    {
        _port = port;
        UDP = new UdpClient(_port);
    }

    public List<Client> ConnectedClients { get; } = new();

    private UdpClient UDP { get; }
    public event OnReceive? OnReceiveEvent;
    public event OnReceiveRaw? OnReceivePacket;

    public void SendRaw(IPEndPoint sender, string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        UDP.SendAsync(bytes, bytes.Length, sender).Wait();
    }

    public void SendEvent(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        UDP.Send(bytes, bytes.Length);
    }

    public void SendEvent(IPEndPoint sender, string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data,
            Sender = sender.ToString()
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        UDP.Send(bytes, bytes.Length, IpParser.ToIpEndPoint(sender.ToString()));
    }

    public void SendEvent(IPEndPoint sender, Client target, string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data,
            Sender = sender.ToString()
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        UDP.Send(bytes, bytes.Length, IpParser.ToIpEndPoint(target.Ip));
    }

    public void SendBroadcastEvent(IPEndPoint sender, string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data,
            Sender = sender.ToString()
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var client in ConnectedClients) UDP.Send(bytes, bytes.Length, IpParser.ToIpEndPoint(client.Ip));
    }

    public void SendBroadcastEvent(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var client in ConnectedClients) UDP.Send(bytes, bytes.Length, IpParser.ToIpEndPoint(client.Ip));
    }

    public async Task SendEventAsync(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        await UDP.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Any, 0));
    }

    public async Task SendEventAsync(Client client, string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        await UDP.SendAsync(bytes, bytes.Length, IpParser.ToIpEndPoint(client.Ip));
    }

    public async void SendBroadcastEventAsync(string eventName, string data)
    {
        var jsonObject = new
        {
            EventName = eventName,
            Data = data
        };
        var message = JsonConvert.SerializeObject(jsonObject);
        var bytes = Encoding.UTF8.GetBytes(message);
        await UDP.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, _port));
    }

    public void Start()
    {
        while (true)
            try
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = UDP.Receive(ref ipEndPoint);
                var content = Encoding.UTF8.GetString(data);

                var json = JsonConvert.DeserializeObject<ReceiveEvent>(content);

                OnReceiveEvent?.Invoke(ipEndPoint, json?.EventName ?? string.Empty, json?.Data ?? string.Empty);

                OnReceivePacket?.Invoke(ipEndPoint, content);
            }
            catch (SocketException socket)
            {
                MessageBox.Show($"Erro no socket: <{socket.SocketErrorCode}> {socket.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Erro ao receber dados: {e}", "Erro", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
    }
}