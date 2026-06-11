using System.Text;

namespace Alca.MonoGame.Kernel.Persistence;

/// <summary>Forward-only binary reader over a <see cref="ReadOnlySpan{T}"/>-backed byte buffer.</summary>
public sealed class SaveDataReader
{
    private readonly byte[] _buffer;
    private int _position;

    /// <summary>Initializes a reader over a copy of <paramref name="data"/>.</summary>
    public SaveDataReader(ReadOnlySpan<byte> data)
    {
        _buffer = data.ToArray();
        _position = 0;
    }

    /// <summary>Gets a value indicating whether the read cursor has reached the end of the data.</summary>
    public bool IsAtEnd => _position >= _buffer.Length;

    #region Read primitives
    /// <summary>Reads a <see cref="bool"/> (1 byte).</summary>
    public bool ReadBool() => ReadByte() != 0;

    /// <summary>Reads a raw <see cref="byte"/>.</summary>
    public byte ReadByte()
    {
        CheckRemaining(1);
        return _buffer[_position++];
    }

    /// <summary>Reads a little-endian <see cref="int"/> (4 bytes).</summary>
    public int ReadInt()
    {
        CheckRemaining(4);
        int v = _buffer[_position]
              | (_buffer[_position + 1] << 8)
              | (_buffer[_position + 2] << 16)
              | (_buffer[_position + 3] << 24);
        _position += 4;
        return v;
    }

    /// <summary>Reads a little-endian <see cref="uint"/> (4 bytes).</summary>
    public uint ReadUInt() => (uint)ReadInt();

    /// <summary>Reads a little-endian IEEE 754 <see cref="float"/> (4 bytes).</summary>
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

    /// <summary>Reads a little-endian IEEE 754 <see cref="double"/> (8 bytes).</summary>
    public double ReadDouble()
    {
        long lo = (uint)ReadInt();
        long hi = (uint)ReadInt();
        return BitConverter.Int64BitsToDouble(lo | (hi << 32));
    }

    /// <summary>Reads a UTF-8 length-prefixed string.</summary>
    public string ReadString()
    {
        int length = ReadInt();
        CheckRemaining(length);
        var value = Encoding.UTF8.GetString(_buffer, _position, length);
        _position += length;
        return value;
    }

    /// <summary>Reads a <see cref="Vector2"/> (8 bytes).</summary>
    public Vector2 ReadVector2() => new(ReadFloat(), ReadFloat());

    /// <summary>Reads a <see cref="Vector3"/> (12 bytes).</summary>
    public Vector3 ReadVector3() => new(ReadFloat(), ReadFloat(), ReadFloat());

    /// <summary>Reads a <see cref="Color"/> (4 bytes: R, G, B, A).</summary>
    public Color ReadColor() => new(ReadByte(), ReadByte(), ReadByte(), ReadByte());
    #endregion

    #region Internal
    private void CheckRemaining(int needed)
    {
        if (_position + needed > _buffer.Length)
            throw new InvalidOperationException(
                $"SaveDataReader: attempted to read {needed} byte(s) beyond end of buffer (position={_position}, length={_buffer.Length}).");
    }
    #endregion
}
