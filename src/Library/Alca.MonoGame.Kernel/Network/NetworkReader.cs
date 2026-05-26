using System.Buffers.Binary;
using System.Text;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Stack-allocated binary reader for network messages. Reads values in little-endian order
/// from a <see cref="ReadOnlySpan{T}"/> without heap allocation.
/// String reads allocate a managed string — all other reads are allocation-free.
/// </summary>
public ref struct NetworkReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    /// <summary>Gets the current read position.</summary>
    public int Position => _position;

    /// <summary>Gets the number of bytes remaining to be read.</summary>
    public int Remaining => _buffer.Length - _position;

    /// <summary>Initializes the reader over the given span.</summary>
    public NetworkReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>Reads a <see cref="bool"/> from a single byte (0 = false, any other value = true).</summary>
    public bool ReadBool()
    {
        EnsureAvailable(1);
        return _buffer[_position++] != 0;
    }

    /// <summary>Reads a single byte.</summary>
    public byte ReadByte()
    {
        EnsureAvailable(1);
        return _buffer[_position++];
    }

    /// <summary>Reads a signed 16-bit integer in little-endian order.</summary>
    public short ReadShort()
    {
        EnsureAvailable(2);
        short value = BinaryPrimitives.ReadInt16LittleEndian(_buffer[_position..]);
        _position += 2;
        return value;
    }

    /// <summary>Reads an unsigned 16-bit integer in little-endian order.</summary>
    public ushort ReadUShort()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer[_position..]);
        _position += 2;
        return value;
    }

    /// <summary>Reads a signed 32-bit integer in little-endian order.</summary>
    public int ReadInt()
    {
        EnsureAvailable(4);
        int value = BinaryPrimitives.ReadInt32LittleEndian(_buffer[_position..]);
        _position += 4;
        return value;
    }

    /// <summary>Reads an unsigned 32-bit integer in little-endian order.</summary>
    public uint ReadUInt()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer[_position..]);
        _position += 4;
        return value;
    }

    /// <summary>Reads a 32-bit IEEE 754 floating-point number in little-endian order.</summary>
    public float ReadFloat()
    {
        EnsureAvailable(4);
        float value = BinaryPrimitives.ReadSingleLittleEndian(_buffer[_position..]);
        _position += 4;
        return value;
    }

    /// <summary>Reads a 64-bit IEEE 754 floating-point number in little-endian order.</summary>
    public double ReadDouble()
    {
        EnsureAvailable(8);
        double value = BinaryPrimitives.ReadDoubleLittleEndian(_buffer[_position..]);
        _position += 8;
        return value;
    }

    /// <summary>Reads a <see cref="Vector2"/> from two consecutive little-endian floats (X, Y).</summary>
    public Vector2 ReadVector2() => new(ReadFloat(), ReadFloat());

    /// <summary>Reads a <see cref="Vector3"/> from three consecutive little-endian floats (X, Y, Z).</summary>
    public Vector3 ReadVector3() => new(ReadFloat(), ReadFloat(), ReadFloat());

    /// <summary>
    /// Reads a UTF-8 string prefixed with a ushort byte-count.
    /// Returns <see cref="string.Empty"/> when the length prefix is zero.
    /// This method allocates a managed string.
    /// </summary>
    public string ReadString()
    {
        ushort byteCount = ReadUShort();
        if (byteCount == 0)
            return string.Empty;

        EnsureAvailable(byteCount);
        string value = Encoding.UTF8.GetString(_buffer.Slice(_position, byteCount));
        _position += byteCount;
        return value;
    }

    private void EnsureAvailable(int required)
    {
        if (_position + required > _buffer.Length)
            throw new InvalidOperationException(
                $"NetworkReader underflow: tried to read {required} byte(s) but only {Remaining} remain.");
    }
}
