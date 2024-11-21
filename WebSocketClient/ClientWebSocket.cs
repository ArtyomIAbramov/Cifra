using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketClient.WebSocketClient
{
    public class ClientWebSocket
    {
        private string serverUri;
        private System.Net.WebSockets.ClientWebSocket webSocket;

        public ClientWebSocket(string serverUri)
        {
            this.serverUri = serverUri;
            webSocket = new System.Net.WebSockets.ClientWebSocket();
        }

        public async Task Connect()
        {
            while (webSocket.State != WebSocketState.Open)
            {
                try
                {
                    Console.WriteLine("Attempting to connect to the server...");
                    await webSocket.ConnectAsync(new Uri(serverUri), CancellationToken.None);
                    Console.WriteLine("Connected to the server.");
                    await ReceiveMessages();
                }
                catch (Exception ex)
                {
                    webSocket = new System.Net.WebSockets.ClientWebSocket();
                    Console.WriteLine($"Connection failed: {ex.Message}. Retrying to reconnect...");
                }
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server closed the connection.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by server", CancellationToken.None);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Message from server: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    await ReconnectAsync();
                    break;
                }
            }
        }

        private static readonly HttpClient client = new HttpClient();

        public async Task<int> GetConnectedClientsCount()
        {
            var response = await client.GetAsync("http://localhost:5000/clients");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            return (int)result.Count;
        }

        public async Task SendMessageAsync(string message)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                Console.WriteLine("Cannot send message. Not connected to server.");
            }
        }

        private async Task ReconnectAsync()
        {
            Console.WriteLine("Reconnecting to server...");
            webSocket.Dispose();
            webSocket = new System.Net.WebSockets.ClientWebSocket();
            await Connect();
        }
    }
}
