using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketServer.WebSocketServer
{
    public class WebSocketServer
    {
        private HttpListener listener;
        private ConcurrentBag<WebSocket> clients = new ConcurrentBag<WebSocket>();

        public WebSocketServer(string uri)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine("WebSocket server started...");
            ListenAsync();
        }

        public void Stop()
        {
            listener.Stop();
            Console.WriteLine("WebSocket server stopped.");
        }

        public Task<string> HandleMessageAsync(string message)
        {
            var timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            return Task.FromResult(timestampedMessage);
        }

        private async void ListenAsync()
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in WebSocket server: {ex.Message}");
                }
            }
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            clients.Add(webSocket);

            var buffer = new byte[1024];
            Console.WriteLine("New client connected.");

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Client disconnected.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var processedMessage = await HandleMessageAsync(message);
                    await BroadcastMessageAsync(processedMessage);
                }
            }
            finally
            {
                clients.TryTake(out _);
            }
        }
        private async Task BroadcastMessageAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send message to a client: {ex.Message}");
                    }
                }
            }
        }
    }
}
