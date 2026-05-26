using Alca.MonoGame.Kernel.Network;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

public sealed class NetworkWriterReaderTests
{
    [Fact]
    public void Write_ReadBool_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.Write(true);
        w.Write(false);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.True(r.ReadBool());
        Assert.False(r.ReadBool());
    }

    [Fact]
    public void Write_ReadInt_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.Write(42);
        w.Write(-1_000_000);
        w.Write(int.MinValue);
        w.Write(int.MaxValue);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.Equal(42, r.ReadInt());
        Assert.Equal(-1_000_000, r.ReadInt());
        Assert.Equal(int.MinValue, r.ReadInt());
        Assert.Equal(int.MaxValue, r.ReadInt());
    }

    [Fact]
    public void Write_ReadFloat_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.Write(3.14f);
        w.Write(-0.5f);
        w.Write(float.NaN);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.Equal(3.14f, r.ReadFloat());
        Assert.Equal(-0.5f, r.ReadFloat());
        Assert.True(float.IsNaN(r.ReadFloat()));
    }

    [Fact]
    public void Write_ReadVector2_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        var v = new Vector2(1.5f, -2.75f);
        w.Write(v);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Vector2 result = r.ReadVector2();
        Assert.Equal(v.X, result.X);
        Assert.Equal(v.Y, result.Y);
    }

    [Fact]
    public void Write_ReadVector3_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        var v = new Vector3(10f, -20f, 0.001f);
        w.Write(v);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Vector3 result = r.ReadVector3();
        Assert.Equal(v.X, result.X);
        Assert.Equal(v.Y, result.Y);
        Assert.Equal(v.Z, result.Z);
    }

    [Fact]
    public void WriteString_ReadString_RoundTrip()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        const string text = "Hello, World!";
        w.WriteString(text);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.Equal(text, r.ReadString());
    }

    [Fact]
    public void WriteString_NullOrEmpty_ReadsEmpty()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.WriteString(null);
        w.WriteString(string.Empty);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.Equal(string.Empty, r.ReadString());
        Assert.Equal(string.Empty, r.ReadString());
    }

    [Fact]
    public void NetworkWriter_Overflow_Throws()
    {
        byte[] buf = new byte[2];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.Write((byte)1);
        w.Write((byte)2);

        bool threw = false;
        try { w.Write((byte)3); }
        catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void NetworkReader_ReadPastEnd_Throws()
    {
        byte[] buf = [5, 10];
        var r = new NetworkReader(new ReadOnlySpan<byte>(buf));
        r.ReadByte();
        r.ReadByte();

        bool threw = false;
        try { r.ReadByte(); }
        catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void Write_MultipleTypes_CorrectPositions()
    {
        byte[] buf = new byte[256];
        var w = new NetworkWriter(new Span<byte>(buf));
        w.Write(true);      // 1 byte  → pos 1
        w.Write((byte)255); // 1 byte  → pos 2
        w.Write(100);       // 4 bytes → pos 6
        w.Write(2.5f);      // 4 bytes → pos 10

        Assert.Equal(10, w.Position);

        var r = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, w.Position));
        Assert.True(r.ReadBool());
        Assert.Equal(255, r.ReadByte());
        Assert.Equal(100, r.ReadInt());
        Assert.Equal(2.5f, r.ReadFloat());
    }
}
