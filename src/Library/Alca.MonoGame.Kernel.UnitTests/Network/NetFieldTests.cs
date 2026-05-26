using Alca.MonoGame.Kernel.Network;
using Alca.MonoGame.Kernel.Network.NetFields;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

public sealed class NetFieldTests
{
    // ── NetInt ─────────────────────────────────────────────────────────────────

    [Fact]
    public void NetInt_InitialValue_NotDirty()
    {
        var field = new NetInt(42);
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void NetInt_SetValue_MarksDirty()
    {
        var field = new NetInt(0);
        field.Value = 7;
        Assert.True(field.IsDirty);
    }

    [Fact]
    public void NetInt_SetSameValue_NotDirty()
    {
        var field = new NetInt(5);
        field.Value = 5;
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void NetInt_ClearDirty_IsDirtyFalse()
    {
        var field = new NetInt(0);
        field.Value = 1;
        Assert.True(field.IsDirty);
        field.ClearDirty();
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void NetInt_OnValueChanged_FiresWithOldAndNew()
    {
        var field = new NetInt(10);
        int capturedOld = 0;
        int capturedNew = 0;
        field.OnValueChanged += (o, n) => { capturedOld = o; capturedNew = n; };

        field.Value = 20;

        Assert.Equal(10, capturedOld);
        Assert.Equal(20, capturedNew);
    }

    [Fact]
    public void NetInt_ImplicitConversion_ReturnsValue()
    {
        var field = new NetInt(99);
        int value = field;
        Assert.Equal(99, value);
    }

    // ── NetFloat ───────────────────────────────────────────────────────────────

    [Fact]
    public void NetFloat_SerializeDeserialize_RoundTrip()
    {
        var source = new NetFloat(3.14f);
        var dest = new NetFloat(0f);

        byte[] buf = new byte[64];
        var writer = new NetworkWriter(new Span<byte>(buf));
        source.Serialize(ref writer);

        var reader = new NetworkReader(new ReadOnlySpan<byte>(buf, 0, writer.Position));
        dest.Deserialize(ref reader);

        Assert.Equal(3.14f, dest.Value);
    }

    // ── NetString ──────────────────────────────────────────────────────────────

    [Fact]
    public void NetString_SetValue_MarksDirty()
    {
        var field = new NetString("hello");
        field.Value = "world";
        Assert.True(field.IsDirty);
    }

    [Fact]
    public void NetString_SetSameValue_NotDirty()
    {
        var field = new NetString("hello");
        field.Value = "hello";
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void NetString_NullAssignment_TreatedAsEmpty()
    {
        var field = new NetString("hello");
        field.Value = null!;
        Assert.Equal(string.Empty, field.Value);
    }

    // ── NetVector2 ─────────────────────────────────────────────────────────────

    [Fact]
    public void NetVector2_SetSameValue_NotDirty()
    {
        var v = new Vector2(1f, 2f);
        var field = new NetVector2(v);
        field.Value = v;
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void NetVector2_SetDifferentValue_MarksDirty()
    {
        var field = new NetVector2(Vector2.Zero);
        field.Value = new Vector2(3f, 4f);
        Assert.True(field.IsDirty);
    }

    // ── NetBool ────────────────────────────────────────────────────────────────

    [Fact]
    public void NetBool_Toggle_MarksDirty()
    {
        var field = new NetBool(false);
        field.Value = true;
        Assert.True(field.IsDirty);
        Assert.True(field.Value);
    }

    [Fact]
    public void NetBool_SetSameValue_NotDirty()
    {
        var field = new NetBool(true);
        field.Value = true;
        Assert.False(field.IsDirty);
    }
}
