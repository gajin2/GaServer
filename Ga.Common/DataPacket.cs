namespace Ga.Common;

public sealed class DataPacket
{
    public DataPacket(int cmd)
    {
        WriteInt(cmd);
    }

    public DataPacket(uint cmd)
    {
        WriteUInt(cmd);
    }

    public void WriteByte(byte value)
    {
        _data.Add(value);
    }

    public void WriteSbyte(sbyte value)
    {
        _data.Add((byte)value);
    }

    public void WriteShort(short value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteUShort(ushort value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteInt(int value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteInt64(long value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteUInt(uint value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteDouble(double value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    public byte[] ToBytes()
    {
        return _data.ToArray();
    }

    // 4byte: len
    // 4byte: proto
    // body
    public byte[] ToPackBytes()
    {
        if (!_appendLen)
        {
            _appendLen = true;
            int len = _data.Count;
            _data.InsertRange(0, BitConverter.GetBytes(len)); // len
            // _data.InsertRange(0, BitConverter.GetBytes((ushort)Def.TAG_VALUE));
        }

        return ToBytes();
    }

    public void WriteByteArray(byte[] bytes)
    {
        _data.AddRange(bytes);
    }

    private List<byte> _data = new List<byte>();
    private bool _appendLen = false;
}