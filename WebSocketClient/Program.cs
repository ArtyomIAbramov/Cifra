(string host, int port) = ReadConnection();

string serverUri = $"ws://{host}:{port}/";
var client = new WebSocketClient.WebSocketClient.ClientWebSocket(serverUri);

await client.Connect();

Console.WriteLine("Enter messages to send to the server. Type 'exit' to quit.");

while (true)
{
    var message = Console.ReadLine();

    if (message != null)
    {
        if (message.ToLower() == "exit")
            break; 
        else
            await client.SendMessageAsync(message);
    } else
        Console.Write("Message is null");
}

Console.WriteLine("Connected clients count:");
var count = client.GetConnectedClientsCount().Result;
Console.WriteLine(count);

Console.WriteLine("Client stopped.");

static (string host, int port) ReadConnection()
{
    Console.Write("Host: ");
    var host = Console.ReadLine();
    while (host == null)
    {
        Console.Write("Please enter host: ");
        host = Console.ReadLine();
    }

    Console.Write("Port: ");

    int port;

    while (!int.TryParse(Console.ReadLine(),out port))
    {
        Console.Write("Please enter correct number: ");
    }

    return (host, port);
}