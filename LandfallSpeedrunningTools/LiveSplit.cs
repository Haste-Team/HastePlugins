using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace LandfallSpeedrunningTools;

public abstract class LiveSplit : IDisposable
{
    protected static readonly UTF8Encoding Encoding = new(false);
    public static LiveSplit? Instance;

    public abstract Task ConnectAsync();
    protected abstract Task Send(byte[] bytes);

    public void Split() => Send("startorsplit");
    public void PauseGameTime() => Send("pausegametime");
    public void UnpauseGameTime() => Send("unpausegametime");

    private async void Send(string command)
    {
        Debug.Log($"Sending command to LiveSplit: {command}");
        await Send(Encoding.GetBytes(command + Environment.NewLine));
        Debug.Log($"Done sending command to LiveSplit: {command}");
    }

    public abstract void Dispose();
}

public class LiveSplitNamedPipe : LiveSplit
{
    private readonly NamedPipeClientStream _stream;

    public LiveSplitNamedPipe() => _stream = new NamedPipeClientStream(".", "LiveSplit", PipeDirection.InOut, PipeOptions.Asynchronous);

    public override async Task ConnectAsync()
    {
        await _stream.ConnectAsync(1000);
        RecvLoop(_stream);
        Debug.Log("LiveSplit client connected");
    }

    private static async void RecvLoop(Stream stream)
    {
        byte[] buf = new byte[1024];
        while (stream.CanRead)
        {
            var count = await stream.ReadAsync(buf, 0, buf.Length);
            if (count <= 0)
                break;
            Debug.Log($"Got data from LiveSplit: {Encoding.GetString(buf, 0, count)}");
        }

        Debug.Log("LiveSplit stream closed");
    }

    protected override async Task Send(byte[] arr)
    {
        await _stream.WriteAsync(arr, 0, arr.Length);
        await _stream.FlushAsync();
    }

    public override void Dispose()
    {
        _stream.Dispose();
    }
}

public class LiveSplitTCP : LiveSplit
{
    private static readonly int LivesplitDefaultPort = 16834;
    private readonly TcpClient _client;

    public LiveSplitTCP() => _client = new TcpClient();

    public override async Task ConnectAsync()
    {
        await _client.ConnectAsync("localhost", LivesplitDefaultPort);
        RecvLoop(_client.GetStream());
        Debug.Log("LiveSplit client connected");
    }

    private static async void RecvLoop(Stream stream)
    {
        byte[] buf = new byte[1024];
        while (stream.CanRead)
        {
            var count = await stream.ReadAsync(buf, 0, buf.Length);
            if (count <= 0)
                break;
            Debug.Log($"Got data from LiveSplit: {Encoding.GetString(buf, 0, count)}");
        }

        Debug.Log("LiveSplit stream closed");
    }

    protected override async Task Send(byte[] arr)
    {
        await _client.GetStream().WriteAsync(arr, 0, arr.Length);
        await _client.GetStream().FlushAsync();
    }

    public override void Dispose()
    {
        _client.Dispose();
    }
}

/*
public class LiveSplitWebSocketServer : LiveSplit
{
    private readonly List<WebSocket> _websockets = [];

    public LiveSplitWebSocketServer(string prefix)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        RecvLoop(listener);
    }

    private static async void RecvLoop(WebSocket stream)
    {
        byte[] buf = new byte[1024];
        while (stream.State == WebSocketState.Open)
        {
            var result = await stream.ReceiveAsync(new ArraySegment<byte>(buf, 0, buf.Length), CancellationToken.None);
            Debug.Log($"Got data from LiveSplit websocket: {Encoding.GetString(buf, 0, result.Count)}");
        }

        Debug.Log("LiveSplit websocket stream closed");
    }

    private async void RecvLoop(HttpListener listener)
    {
        Debug.Log("Websocket server started");
        while (true)
        {
            var context = await listener.GetContextAsync();
            Debug.Log($"Got http request: is websocket={context.Request.IsWebSocketRequest} ({context.Request.GetType().FullName})");
            if (context.Request.IsWebSocketRequest)
            {
                var websocketContext = await context.AcceptWebSocketAsync(null);
                Debug.Log("Accepted new websocket client");
                var websocket = websocketContext.WebSocket;
                _websockets.Add(websocket);
                RecvLoop(websocket);
            }
        }
    }

    protected override async Task Send(byte[] arr)
    {
        List<WebSocket>? toRemove = null;
        foreach (var socket in _websockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                Debug.Log($"Socket disconnected ({socket.State}). Removing from clients.");
                toRemove ??= [];
                toRemove.Add(socket);
            }
            else
                await socket.SendAsync(new ArraySegment<byte>(arr, 0, arr.Length), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
        if (toRemove != null)
        {
            foreach (var sock in toRemove)
            {
                _websockets.Remove(sock);
            }
        }
    }

    public override void Dispose()
    {
    }
}
*/
