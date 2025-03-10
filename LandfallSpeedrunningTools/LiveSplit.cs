using System.IO.Pipes;
using System.Text;
using UnityEngine;

namespace LandfallSpeedrunningTools;

public class LiveSplit : IDisposable
{
    private static readonly UTF8Encoding Encoding = new(false);
    private readonly NamedPipeClientStream _stream;
    public static LiveSplit? Instance;

    public LiveSplit()
    {
        _stream = new NamedPipeClientStream(".", "LiveSplit", PipeDirection.InOut, PipeOptions.Asynchronous);
        _stream.Connect(1000);
        RecvLoop();
    }

    private async void RecvLoop()
    {
        byte[] buf = new byte[1024];
        while (_stream.CanRead)
        {
            var count = await _stream.ReadAsync(buf, 0, buf.Length);
            if (count <= 0)
                break;
            Debug.Log($"Got data from LiveSplit: {Encoding.GetString(buf, 0, count)}");
        }

        Debug.Log("LiveSplit stream closed");
    }

    public void Split()
    {
        Send($"startorsplit{Environment.NewLine}");
    }

    public async void Send(string command)
    {
        Debug.Log("Sending split to LiveSplit");
        var arr = Encoding.GetBytes(command);
        await _stream.WriteAsync(arr, 0, arr.Length);
        await _stream.FlushAsync();
        Debug.Log("Done sending split to LiveSplit");
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}
