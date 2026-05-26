using System.Buffers.Binary;
using System.Text;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Stack-allocated, zero-heap-alloc writer for binary network messages.
/// Wraps a <see cref="Span{T}"/> of bytes and writes values in little-endian order.
/// </summary>
public ref struct NetworkWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    /// <summary>Gets the number of bytes written so far.</summary>
    public int Position => _position;

    /// <summary>Gets the number of bytes remaining in the underlying buffer.</summary>
    public int Remaining => _buffer.Length - _position;

    /// <summary>Gets a read-only view of the bytes written so far.</summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer[.._position];

    /// <summary>Initializes the writer with the given backing buffer.</summary>
    public NetworkWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>Resets the write position to the beginning of the buffer.</summary>
    public void Reset() => _position = 0;

    /// <summary>Writes a <see cref="bool"/> as a single byte (0 = false, 1 = true).</summary>
    public void Write(bool value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value ? (byte)1 : (byte)0;
    }

    /// <summary>Writes a single byte.</summary>
    public void Write(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    /// <summary>Writes a signed 16-bit integer in little-endian order.</summary>
    public void Write(short value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer[_position..], value);
        _position += 2;
    }

    /// <summary>Writes an unsigned 16-bit integer in little-endian order.</summary>
    public void Write(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer[_position..], value);
        _position += 2;
    }

    /// <summary>Writes a signed 32-bit integer in little-endian order.</summary>
    public void Write(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer[_position..], value);
        _position += 4;
    }

    /// <summary>Writes an unsigned 32-bit integer in little-endian order.</summary>
    public void Write(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer[_position..], value);
        _position += 4;
    }

    /// <summary>Writes a 32-bit IEEE 754 floating-point number in little-endian order.</summary>
    public void Write(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer[_position..], value);
        _position += 4;
    }

    /// <summary>Writes a 64-bit IEEE 754 floating-point number in little-endian order.</summary>
    public void Write(double value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer[_position..], value);
        _position += 8;
    }

    /// <summary>Writes a <see cref="Vector2"/> as two consecutive little-endian floats (X, Y).</summary>
    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    /// <summary>Writes a <see cref="Vector3"/> as three consecutive little-endian floats (X, Y, Z).</summary>
    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    /// <summary>
    /// Writes a UTF-8 string with a ushort length prefix (max 65 535 bytes).
    /// Null or empty strings are written as a zero-length prefix with no payload.
    /// </summary>
    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Write((ushort)0);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ushort.MaxValue)
            throw new ArgumentException($"String is too long ({byteCount} bytes). Maximum is {ushort.MaxValue}.", nameof(value));

        Write((ushort)byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer[_position..]);
        _position += byteCount;
    }

    private void EnsureCapacity(int required)
    {
        if (_position + required > _buffer.Length)
            throw new InvalidOperationException(
                $"NetworkWriter overflow: tried to write {required} byte(s) but only {Remaining} remain.");
    }
}
