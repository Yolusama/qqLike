// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ByteArrayToIntArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 从数字数组转回字节数组
        return JsonSerializer.Deserialize<int[]>(ref reader)
            .Select(x => (byte)x).ToArray();
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        // 将字节数组转为数字数组
        writer.WriteStartArray();
        foreach (byte b in value)
        {
            writer.WriteNumberValue(b);
        }

        writer.WriteEndArray();
    }
}

internal class JSON
{
    public static T Parse<T>(string json)
    {
        JsonDocument document = JsonDocument.Parse(json);
        return document.Deserialize<T>(new JsonSerializerOptions(new JsonSerializerOptions
            { Converters = { new JsonStringEnumConverter() } }));
    }

    public static String Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj,
            new JsonSerializerOptions(new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }));
    }
}

public class ChatMessage
{
    public int Port { get; set; }
    public String Nickname { get; set; }
    public String Type { get; set; }
    public Object Content { get; set; }
    public DateTime Time { get; set; }
}

class Program
{
    private static readonly Dictionary<int, Socket> clients = new();

    static void Main(string[] args)
    {
        Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
        server.Listen(1000);
        object locker = new object();

        ThreadPool.QueueUserWorkItem(arg =>
        {
            while (true)
            {
                lock (locker)
                {
                    Socket client = server.Accept();
                    if (client == null) return;
                    IPEndPoint ip = client.RemoteEndPoint as IPEndPoint;
                    int port = ip.Port;
                    clients.TryAdd(port, client);
                    Console.WriteLine($"Client connected :{ip}");
                }
            }
        });

        ThreadPool.QueueUserWorkItem(arg =>
        {
            while (true)
            {
                for (int i = 0; i < clients.Keys.Count; i++)
                {
                    try
                    {
                        int port = clients.Keys.ElementAt(i);
                        Socket client = clients[port];
                        if (!client.Connected || client.Available < 0) continue;
                        byte[] buffer = new byte[1024 * 1024 * 10];
                        int recvBytes = client.Receive(buffer);
                        if (recvBytes == 0)
                        {
                            Console.WriteLine($"Client disconnected :{client.RemoteEndPoint}");
                            clients.Remove(port, out client);
                            continue;
                        }

                        String json = Encoding.UTF8.GetString(buffer, 0, recvBytes);
                        ChatMessage msg = JSON.Parse<ChatMessage>(json);
                        if (clients.ContainsKey(msg.Port))
                        {
                            int portTo = msg.Port;
                            msg.Port = port;
                            clients[portTo].Send(Encoding.UTF8.GetBytes(JSON.Serialize(msg)));
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        });
        while (true) ;
    }
}