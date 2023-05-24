using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Server.Models;
using Server.Utils;

namespace Server.ViewModels;

public class ServerViewModel : ObservableRecipient
{
    private ObservableCollection<Client> _clients = new();
    private string _ip = "127.0.0.1";
    private string _message = string.Empty;
    private int _port = 35565;

    private Services.Server? _server;

    private AsyncRelayCommand? _startServerCommand;

    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public ObservableCollection<Client> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    public AsyncRelayCommand StartServerCommand => _startServerCommand ??= new AsyncRelayCommand(StartServer);

    private Task StartServer()
    {
        return Task.Run(() =>
        {
            _server = new Services.Server(Port);
            _server.OnReceivePacket += OnReceive;
            _server.OnReceiveEvent += OnReceiveEvent;

            _server.Start();
        });
    }

    private async void OnReceive(IPEndPoint sender, string content)
    {
        var chunk = JsonConvert.DeserializeObject<Chunk>(content);
        if (chunk == null) return;

        var target = Clients.FirstOrDefault(x => x.Id == chunk.Target);
        if (target == null) return;

        _server?.SendRaw(IpParser.ToIpEndPoint(target.Ip), content);
    }

    private void OnReceiveEvent(IPEndPoint sender, string eventName, string data)
    {
        switch (eventName)
        {
            case "OnConnect":
                OnConnect(data);
                break;
            case "OnDisconnect":
                OnDisconnect(data);
                break;
            case "OnSentMessage":
                OnMessage(sender, data);
                break;
            case "RequestConnect":
                RequestConnect(sender, data);
                break;
            case "AcceptConnect":
                AcceptConnect(sender, data);
                break;
            case "OnFinishFrame":
                OnFinishFrame(sender, data);
                break;
        }
    }

    private void OnConnect(string data)
    {
        var client = JsonConvert.DeserializeObject<Client>(data);

        if (client != null)
            Application.Current.Dispatcher.Invoke(() =>
            {
                Clients.Add(client);
                _server?.ConnectedClients.Add(client);
            });
    }

    private void OnDisconnect(string data)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var client = Clients.First(x => x.Ip == data);
            _server?.ConnectedClients.Remove(client);
            return Clients.Remove(client);
        });
    }

    private void OnMessage(IPEndPoint sender, string data)
    {
        _server?.SendBroadcastEvent(sender, "OnReceiveMessage", data);
    }

    private void RequestConnect(IPEndPoint senderIp, string id)
    {
        var client = Clients.FirstOrDefault(x => x.Id == id);
        var sender = Clients.FirstOrDefault(x => x.Ip == senderIp.ToString());

        if (client == null || sender == null)
        {
            _server?.SendEvent(senderIp, "OnError", "Client not found");
            return;
        }

        if (sender.Ip == client.Ip)
        {
            _server?.SendEvent(senderIp, "OnError", "You can't connect to yourself");
            return;
        }

        if (client is { IsRequested: false, ConnectedTo: "" })
        {
            client.IsRequested = true;
            _server?.SendEvent(senderIp, client, "OnRequestedConnection", sender.Id);
        }
    }

    private void AcceptConnect(IPEndPoint senderIp, string id)
    {
        var client = Clients.FirstOrDefault(x => x.Id == id);
        var sender = Clients.FirstOrDefault(x => x.Ip == senderIp.ToString());

        if (client == null || sender == null)
        {
            _server?.SendEvent(senderIp, "OnError", "Client not found");
            return;
        }

        if (sender.Ip == client.Ip)
        {
            _server?.SendEvent(senderIp, "OnError", "You can't connect to yourself");
            return;
        }

        if (sender is { IsRequested: false, ConnectedTo: "" })
        {
            _server?.SendEvent(senderIp, "OnError", "You don't have any requests");
            return;
        }

        if (sender is { IsRequested: true, ConnectedTo: "" })
        {
            sender.IsRequested = false;
            sender.ConnectedTo = client.Id;

            _server?.SendEvent(IpParser.ToIpEndPoint(client.Ip), "OnAcceptedConnection", sender.Id);
        }
    }

    private void OnFinishFrame(IPEndPoint senderIp, string data)
    {
        var dataObject = JsonConvert.DeserializeObject<ScreenShare>(data);
        var target = Clients.FirstOrDefault(x => x.Id == dataObject?.Target);

        if (target == null) return;

        _server?.SendEvent(IpParser.ToIpEndPoint(target.Ip), "OnFinishFrame", data);
    }
}