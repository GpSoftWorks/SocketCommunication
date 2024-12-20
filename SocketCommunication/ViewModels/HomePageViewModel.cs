using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketCommunication.ViewModels
{
    public partial class HomePageViewModel : ObservableObject
    {
        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private const int Port = 12345;

        [ObservableProperty]
        private string localIpAddress;

        [ObservableProperty]
        private string serverIp;

        [ObservableProperty]
        private string messages;

        [ObservableProperty]
        private string handshakeMessage;

        [ObservableProperty]
        private string message;

        [ObservableProperty]
        private bool isServerStarted = true;

        [ObservableProperty]
        private bool isClientConnected = true;

        [RelayCommand]
        public async Task StartServer()
        {
            try
            {
                IsServerStarted = false;
                LocalIpAddress = GetLocalIPAddress();
                _tcpListener = new TcpListener(IPAddress.Parse(LocalIpAddress), Port);
                _tcpListener.Start();
                Messages = LocalIpAddress;

                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var clientSocket = await _tcpListener.AcceptSocketAsync();
                        _ = HandleClientAsync(clientSocket);
                    }
                });
            }
            catch (Exception ex)
            {
                Messages = $"Error starting server: {ex.Message}";
                IsServerStarted = true;
            }
        }

        private async Task HandleClientAsync(Socket clientSocket)
        {
            try
            {
                using var tcpClient = new TcpClient { Client = clientSocket };
                using var networkStream = tcpClient.GetStream();

                var handshakeMessage = "Handshake successful! Connected to server.";
                await networkStream.WriteAsync(Encoding.UTF8.GetBytes(handshakeMessage));

                var buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await networkStream.ReadAsync(buffer)) > 0)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Messages = $"Received from client: {receivedMessage}";
                }
            }
            catch (Exception ex)
            {
                Messages = $"Error handling client: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task Connect(string serverIp)
        {
            try
            {
                IsClientConnected = false;
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ServerIp, Port);
                _networkStream = _tcpClient.GetStream();

                var buffer = new byte[1024];
                int bytesRead = await _networkStream.ReadAsync(buffer);
                HandshakeMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                _ = ReceiveMessagesAsync();
                IsClientConnected = true;
            }
            catch (Exception ex)
            {
                HandshakeMessage = $"Error connecting to server: {ex.Message}";
                IsClientConnected = false;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            try
            {
                var buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await _networkStream.ReadAsync(buffer)) > 0)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Messages = $"Received from server: {receivedMessage}";
                }
            }
            catch (Exception ex)
            {
                Messages = $"Error receiving messages: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SendMessage()
        {
            if (_networkStream != null && !string.IsNullOrEmpty(Message))
            {
                var messageBytes = Encoding.UTF8.GetBytes(Message);
                await _networkStream.WriteAsync(messageBytes);
                Messages = $"Message sent: {Message}";
                Message = string.Empty;
            }
        }

        public static string GetLocalIPAddress()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "127.0.0.1";
        }
    }
}
