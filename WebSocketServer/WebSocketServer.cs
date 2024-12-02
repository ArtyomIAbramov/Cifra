using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using WebSocketServer.DataBase.Models;
using WebSocketServer.DataBase;

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
                    else
                    {
                        await ProcessHttpRequest(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in WebSocket server: {ex.Message}");
                }
            }
        }

        private async Task ProcessHttpRequest(HttpListenerContext context)
        {
            string responseString = string.Empty;

            if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/send")
            {
                var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                var messageData = JsonConvert.DeserializeObject<MessageData>(body);
                await BroadcastMessageAsync(messageData.Message);
                responseString = JsonConvert.SerializeObject(new { Status = "Message sent" });
            }
            else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/clients")
            {
                responseString = JsonConvert.SerializeObject(new { Count = clients.Count });
            }

            context.Response.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            clients.Add(webSocket);
            await HandleWebSocketConnection(webSocket);
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var array = new ArraySegment<byte>(new byte[1024]);
                var result = await webSocket.ReceiveAsync(array, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var message = Encoding.UTF8.GetString(array.Array, 0, result.Count);
                Console.WriteLine($"Received message: {message}");

                await BroadcastMessageAsync(message);
            }

            clients.TryTake(out webSocket);
        }

        private async Task BroadcastMessageAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                var messageEntity = new Message
                {
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    Client = new Client
                    {
                        SubProtocol = client.SubProtocol,
                        ConnectedAt = DateTime.UtcNow
                    }
                };

                using (var db = DatabaseContext.Instance.GetContext())
                {
                    db.Messages.Add(messageEntity);
                    await db.SaveChangesAsync();
                }


                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        public class MessageData
        {
            public string Message { get; set; }
        }
    }
}
