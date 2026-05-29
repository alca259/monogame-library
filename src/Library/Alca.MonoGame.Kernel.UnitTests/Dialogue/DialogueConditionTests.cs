using Alca.MonoGame.Kernel.Dialogue;

namespace Alca.MonoGame.Kernel.UnitTests.Dialogue;

/// <summary>Unit tests for <see cref="DialogueCondition"/>.</summary>
public sealed class DialogueConditionTests
{
    [Fact]
    public void None_IsEmpty_IsTrue()
    {
        Assert.True(DialogueCondition.None.IsEmpty);
    }

    [Fact]
    public void Default_IsEmpty_IsTrue()
    {
        DialogueCondition sut = default;
        Assert.True(sut.IsEmpty);
    }

    [Fact]
    public void Constructor_WithKeyAndValue_IsEmpty_IsFalse()
    {
        DialogueCondition sut = new DialogueCondition("flag", "true");
        Assert.False(sut.IsEmpty);
    }

    [Fact]
    public void Constructor_StoresKey()
    {
        DialogueCondition sut = new DialogueCondition("myKey", "myValue");
        Assert.Equal("myKey", sut.Key);
    }

    [Fact]
    public void Constructor_StoresValue()
    {
        DialogueCondition sut = new DialogueCondition("myKey", "myValue");
        Assert.Equal("myValue", sut.Value);
    }
}
