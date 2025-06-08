using System.Buffers;
using System.IO.Pipelines;
using Serilog;

namespace Ga.Common;

public class PacketPipeReader
{
    PipeReader reader;
    private CancellationToken Token { get; }

    public PacketPipeReader(PipeReader pipeReader, CancellationToken ct = default)
    {
        reader = pipeReader;
        Token = ct;
    }

    public async Task<bool> Read(Action<int, ReadOnlySequence<byte>> action)
    {
        ReadResult result = await reader.ReadAtLeastAsync(4, Token);
        if (result.IsCompleted)
        {
            Log.Information($"PacketPipeReader result.IsCompleted at packLen");
            return false;
        }

        ReadOnlySequence<byte> buffer = result.Buffer;
        Log.Debug($"ReadPipeAsync buff len={buffer.Length}");

        int packLen = BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
        reader.AdvanceTo(buffer.GetPosition(4));

        result = await reader.ReadAtLeastAsync(packLen, Token);

        if (result.IsCompleted)
        {
            Log.Information($"ReadPipeAsync result.IsCompleted at packBody");
            return false;
        }

        buffer = result.Buffer;

        int proto = BitConverter.ToInt32(buffer.Slice(0, 4).FirstSpan);
        try
        {
            action(proto, buffer.Slice(4));
        }
        catch (Exception ex)
        {
            Log.Error($"PacketPipeReader.Read error {ex.Message}");
        }
        finally
        {
            reader.AdvanceTo(buffer.GetPosition(packLen));
        }

        return true;
    }
}