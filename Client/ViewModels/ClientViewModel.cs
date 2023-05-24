using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Server.Models;

namespace Client.ViewModels;

public class ClientViewModel : ObservableRecipient
{
    private readonly Services.Client _client;
    private readonly string _ip = "127.0.0.1";
    private readonly int _port = 35565;

    private readonly MemoryStream _stream = new();

    private string _clientId = string.Empty;
    private string _historyMessages = string.Empty;
    private ImageSource? _image;

    private string _message = string.Empty;
    private string _status = "Disconnected";
    private Brush _statusColor = Brushes.Red;
    private string _targetClientId = string.Empty;

    public ClientViewModel()
    {
        HistoryMessages += "Welcome to the chat!\n";
        ClientId = new Random().Next(100, 200).ToString(); // Generate a random client ID

        _client = new Services.Client(_ip, _port); // Create a new client
        StartClient(); // Start the client
    }

    public string ClientId
    {
        get => _clientId;
        set => SetProperty(ref _clientId, value);
    }

    public string TargetClientId
    {
        get => _targetClientId;
        set => SetProperty(ref _targetClientId, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string HistoryMessages
    {
        get => _historyMessages;
        set => SetProperty(ref _historyMessages, value);
    }

    public Brush StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public ImageSource? Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    public RelayCommand SendMessageCommand => new(SendMessage);
    public RelayCommand ConnectCommand => new(Connect);
    public RelayCommand TakeScreenshotCommand => new(TakeScreenshot);

    private void StartClient()
    {
        _client.SendEvent("OnConnect", JsonConvert.SerializeObject(
            new
            {
                Id = ClientId,
                Ip = _client.LocalEndPoint
            }
        ));

        _client.OnReceivePacket += (sender, data) =>
        {
            var jsonObject = JsonConvert.DeserializeObject<Chunk>(data);

            switch (jsonObject?.EventName)
            {
                case "OnFrame_chunk":
                    ReceiveFrame(sender, jsonObject.Data, jsonObject.ChunkSize, jsonObject.Size, jsonObject.FragmentId,
                        jsonObject.Sequence);
                    break;
            }
        };

        _client.OnReceiveEvent += (sender, name, data) =>
        {
            switch (name)
            {
                case "OnReceiveMessage":
                    ReceiveMessage(sender, data);
                    break;
                case "OnRequestedConnection":
                    RequestedConnection(sender, data);
                    break;
                case "OnAcceptedConnection":
                    AcceptConnection(sender, data);
                    break;
                case "OnFinishFrame":
                    FinishFrame(sender, data);
                    break;
                case "OnError":
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusColor = Brushes.Red;
                        Status = "Disconnected";
                        MessageBox.Show(data, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    break;
            }
        };
        Task.Run(() => _client.Start());
    }

    private void ReceiveMessage(string sender, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (sender != _client.LocalEndPoint)
                HistoryMessages += $"Team: {message}\n";
        });
    }

    private void SendMessage()
    {
        HistoryMessages += $"You: {Message}\n";
        _client.SendEventAsync("OnSentMessage", Message);
        Message = string.Empty;
    }

    private void Connect()
    {
        _client.SendEventAsync("RequestConnect", TargetClientId);
        StatusColor = Brushes.Yellow;
        Status = "Waiting for connection...";
    }

    private void RequestedConnection(string sender, string id)
    {
        StatusColor = Brushes.Yellow;
        Status = "Waiting for connection...";

        HistoryMessages += $"O cliente {id} quer se conectar com você!\n";
        var result = MessageBox.Show($"Alguém quer se conectar com você!\n{id}", "Conexão solicitada",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _client.SendEventAsync("AcceptConnect", id);
            StatusColor = Brushes.Green;
            Status = "Connected";
            HistoryMessages += $"Você aceitou a conexão com o cliente {id}!\n";

            Task.Run(() => StreamEncode(id));
        }
        else
        {
            _client.SendEventAsync("RejectConnect", id);
            StatusColor = Brushes.Red;
            Status = "Disconnected";
        }
    }

    private void TakeScreenshot()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var bytes = Screen.TakeScreenshot();
            Image = Screen.FromBytes(bytes);
        });
    }

    private void AcceptConnection(string sender, string data)
    {
        StatusColor = Brushes.Green;
        Status = "Connected";

        HistoryMessages += $"O cliente {data} aceitou sua conexão!\n";
    }

    private void StreamEncode(string id)
    {
        var screen = new Screen();
        var frameId = 0;
        screen.OnFrame += frame =>
        {
            var chunk = new byte[1024];
            int bytesCount;
            var sequence = 0;


            using var memoryStream = new MemoryStream(frame);

            while ((bytesCount = memoryStream.Read(chunk, 0, 1024)) > 0)
                _client.SendChunk(id, "OnFrame", Convert.ToBase64String(chunk, 0, bytesCount), bytesCount,
                    frame.Length, frameId, sequence++);

            _client.SendEventAsync("OnFinishFrame",
                JsonConvert.SerializeObject(new { Target = id, Data = "" }));
            frameId++;
        };
        screen.Start();
    }

    private void ReceiveFrame(string sender, string frame, int length, int size, int frameId, int sequence)
    {
        var bytes = Convert.FromBase64String(frame);
        
        _stream.Position = sequence * 1024;
        _stream.Write(bytes, 0, bytes.Length);

        Status = $"Receiving chunks of the frame({length}) from {sender} {sequence}...";
        StatusColor = Brushes.Yellow;
    }

    private void FinishFrame(string sender, string data)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_stream.Length == 0) return;

            Image = Screen.FromBytes(_stream.GetBuffer());
            _stream.SetLength(0);
            
            Status = $"Frame received from {sender}!";
            StatusColor = Brushes.Green;
        });
    }
}