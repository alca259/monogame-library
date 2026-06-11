using System.Buffers;
using System.Text;

namespace Alca.MonoGame.Kernel.Persistence;

/// <summary>
/// Forward-only binary writer backed by a pooled buffer.
/// Use within a single save operation; call <see cref="ToReadOnlySpan"/> when complete.
/// </summary>
public sealed class SaveDataWriter : IDisposable
{
    private const int InitialCapacity = 4096;

    private byte[] _buffer;
    private int _position;
    private bool _returned;

    /// <summary>Initializes a new writer and leases a buffer from <see cref="ArrayPool{T}.Shared"/>.</summary>
    public SaveDataWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(InitialCapacity);
        _position = 0;
    }

    #region Write primitives
    /// <summary>Writes a <see cref="bool"/> (1 byte).</summary>
    public void Write(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    /// <summary>Writes a <see cref="byte"/> (1 byte).</summary>
    public void Write(byte value) => WriteByte(value);

    /// <summary>Writes a little-endian <see cref="int"/> (4 bytes).</summary>
    public void Write(int value)
    {
        EnsureCapacity(4);
        _buffer[_position++] = (byte)(value & 0xFF);
        _buffer[_position++] = (byte)((value >> 8) & 0xFF);
        _buffer[_position++] = (byte)((value >> 16) & 0xFF);
        _buffer[_position++] = (byte)((value >> 24) & 0xFF);
    }

    /// <summary>Writes a little-endian <see cref="uint"/> (4 bytes).</summary>
    public void Write(uint value) => Write((int)value);

    /// <summary>Writes a little-endian IEEE 754 <see cref="float"/> (4 bytes).</summary>
    public void Write(float value) => Write(BitConverter.SingleToInt32Bits(value));

    /// <summary>Writes a little-endian IEEE 754 <see cref="double"/> (8 bytes).</summary>
    public void Write(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        Write((int)(bits & 0xFFFFFFFF));
        Write((int)((bits >> 32) & 0xFFFFFFFF));
    }

    /// <summary>Writes a UTF-8 length-prefixed string (4-byte length + bytes).</summary>
    public void Write(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        Write(byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position);
        _position += byteCount;
    }

    /// <summary>Writes a <see cref="Vector2"/> (8 bytes).</summary>
    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    /// <summary>Writes a <see cref="Vector3"/> (12 bytes).</summary>
    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    /// <summary>Writes a <see cref="Color"/> (4 bytes: R, G, B, A).</summary>
    public void Write(Color value)
    {
        WriteByte(value.R);
        WriteByte(value.G);
        WriteByte(value.B);
        WriteByte(value.A);
    }

    /// <summary>Returns the written bytes as a read-only span. Valid until <see cref="Dispose"/> is called.</summary>
    public ReadOnlySpan<byte> ToReadOnlySpan() => new(_buffer, 0, _position);

    /// <summary>Returns the underlying buffer to the pool. The writer must not be used after disposal.</summary>
    public void Dispose()
    {
        if (!_returned)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _returned = true;
        }
    }
    #endregion

    #region Internal
    private void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    private void EnsureCapacity(int needed)
    {
        if (_position + needed <= _buffer.Length) return;

        int newSize = Math.Max(_buffer.Length * 2, _position + needed);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
    #endregion
}
