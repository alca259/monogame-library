using Alca.MonoGame.Kernel.UI.Focus;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Focus;

public sealed class UIFocusManagerTests
{
    #region Helpers

    private sealed class StubFocusable : IFocusable
    {
        public int TabIndex { get; init; }
        public int? FocusNeighborUp    { get; set; }
        public int? FocusNeighborDown  { get; set; }
        public int? FocusNeighborLeft  { get; set; }
        public int? FocusNeighborRight { get; set; }
        public bool IsFocused { get; private set; }
        public int FocusGainedCount  { get; private set; }
        public int FocusLostCount    { get; private set; }

        public void OnFocusGained() { IsFocused = true;  FocusGainedCount++; }
        public void OnFocusLost()   { IsFocused = false; FocusLostCount++;   }
    }

    private static UIFocusManager CreateManager() => new UIFocusManager();

    #endregion

    #region Register / Unregister

    [Fact]
    public void Register_AddElement_IsAvailableForFocusNext()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };

        mgr.Register(el);
        mgr.FocusNext();

        Assert.Same(el, mgr.FocusedElement);
    }

    [Fact]
    public void Register_DuplicateTabIndex_IgnoresSecond()
    {
        var mgr = CreateManager();
        var first  = new StubFocusable { TabIndex = 0 };
        var second = new StubFocusable { TabIndex = 0 };

        mgr.Register(first);
        mgr.Register(second);
        mgr.FocusNext();

        Assert.Same(first, mgr.FocusedElement);
    }

    [Fact]
    public void Unregister_RemovesElementFromTabOrder()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };

        mgr.Register(el);
        mgr.Unregister(el);
        mgr.FocusNext();

        Assert.Null(mgr.FocusedElement);
    }

    [Fact]
    public void Unregister_FocusedElement_ClearsFocusWithoutFiringLost()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };

        mgr.Register(el);
        mgr.SetFocus(el);
        mgr.Unregister(el);

        Assert.Null(mgr.FocusedElement);
        // OnFocusLost was NOT fired during Unregister (element already being removed)
        Assert.Equal(0, el.FocusLostCount);
    }

    #endregion

    #region SetFocus

    [Fact]
    public void SetFocus_NewElement_FiresOnFocusGained()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);

        mgr.SetFocus(el);

        Assert.Equal(1, el.FocusGainedCount);
        Assert.True(el.IsFocused);
    }

    [Fact]
    public void SetFocus_PreviousElement_FiresOnFocusLost()
    {
        var mgr = CreateManager();
        var first  = new StubFocusable { TabIndex = 0 };
        var second = new StubFocusable { TabIndex = 1 };
        mgr.Register(first);
        mgr.Register(second);

        mgr.SetFocus(first);
        mgr.SetFocus(second);

        Assert.Equal(1, first.FocusLostCount);
        Assert.False(first.IsFocused);
    }

    [Fact]
    public void SetFocus_SameElement_DoesNotFireEvents()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.SetFocus(el);

        Assert.Equal(1, el.FocusGainedCount);
        Assert.Equal(0, el.FocusLostCount);
    }

    [Fact]
    public void SetFocus_Null_ClearsCurrentFocus()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.SetFocus(null);

        Assert.Null(mgr.FocusedElement);
        Assert.Equal(1, el.FocusLostCount);
    }

    #endregion

    #region FocusNext / FocusPrevious

    [Fact]
    public void FocusNext_NoElements_DoesNotThrow()
    {
        var mgr = CreateManager();
        mgr.FocusNext(); // Must not throw
        Assert.Null(mgr.FocusedElement);
    }

    [Fact]
    public void FocusNext_NothingFocused_FocusesFirstByTabIndex()
    {
        var mgr = CreateManager();
        var a = new StubFocusable { TabIndex = 5 };
        var b = new StubFocusable { TabIndex = 1 };
        mgr.Register(a);
        mgr.Register(b);

        mgr.FocusNext();

        Assert.Same(b, mgr.FocusedElement);
    }

    [Fact]
    public void FocusNext_WrapsAroundAtEnd()
    {
        var mgr = CreateManager();
        var a = new StubFocusable { TabIndex = 0 };
        var b = new StubFocusable { TabIndex = 1 };
        mgr.Register(a);
        mgr.Register(b);
        mgr.SetFocus(b);

        mgr.FocusNext();

        Assert.Same(a, mgr.FocusedElement);
    }

    [Fact]
    public void FocusPrevious_WrapsAroundAtStart()
    {
        var mgr = CreateManager();
        var a = new StubFocusable { TabIndex = 0 };
        var b = new StubFocusable { TabIndex = 1 };
        mgr.Register(a);
        mgr.Register(b);
        mgr.SetFocus(a);

        mgr.FocusPrevious();

        Assert.Same(b, mgr.FocusedElement);
    }

    [Fact]
    public void FocusNext_SingleElement_StaysFocused()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.FocusNext();

        Assert.Same(el, mgr.FocusedElement);
    }

    [Fact]
    public void FocusNext_TabOrderRespected()
    {
        var mgr = CreateManager();
        var a = new StubFocusable { TabIndex = 10 };
        var b = new StubFocusable { TabIndex = 2 };
        var c = new StubFocusable { TabIndex = 7 };
        mgr.Register(a);
        mgr.Register(b);
        mgr.Register(c);

        mgr.FocusNext(); // → b (TabIndex 2)
        Assert.Same(b, mgr.FocusedElement);

        mgr.FocusNext(); // → c (TabIndex 7)
        Assert.Same(c, mgr.FocusedElement);

        mgr.FocusNext(); // → a (TabIndex 10)
        Assert.Same(a, mgr.FocusedElement);
    }

    #endregion

    #region D-Pad Navigation

    [Fact]
    public void FocusUp_WithValidNeighbor_MoveseFocus()
    {
        var mgr = CreateManager();
        var top    = new StubFocusable { TabIndex = 0 };
        var bottom = new StubFocusable { TabIndex = 1, FocusNeighborUp = 0 };
        mgr.Register(top);
        mgr.Register(bottom);
        mgr.SetFocus(bottom);

        mgr.FocusUp();

        Assert.Same(top, mgr.FocusedElement);
    }

    [Fact]
    public void FocusDown_WithValidNeighbor_MovesFocus()
    {
        var mgr = CreateManager();
        var top    = new StubFocusable { TabIndex = 0, FocusNeighborDown = 1 };
        var bottom = new StubFocusable { TabIndex = 1 };
        mgr.Register(top);
        mgr.Register(bottom);
        mgr.SetFocus(top);

        mgr.FocusDown();

        Assert.Same(bottom, mgr.FocusedElement);
    }

    [Fact]
    public void FocusLeft_WithValidNeighbor_MovesFocus()
    {
        var mgr = CreateManager();
        var left  = new StubFocusable { TabIndex = 0 };
        var right = new StubFocusable { TabIndex = 1, FocusNeighborLeft = 0 };
        mgr.Register(left);
        mgr.Register(right);
        mgr.SetFocus(right);

        mgr.FocusLeft();

        Assert.Same(left, mgr.FocusedElement);
    }

    [Fact]
    public void FocusRight_WithValidNeighbor_MovesFocus()
    {
        var mgr = CreateManager();
        var left  = new StubFocusable { TabIndex = 0, FocusNeighborRight = 1 };
        var right = new StubFocusable { TabIndex = 1 };
        mgr.Register(left);
        mgr.Register(right);
        mgr.SetFocus(left);

        mgr.FocusRight();

        Assert.Same(right, mgr.FocusedElement);
    }

    [Fact]
    public void FocusUp_NoNeighbor_DoesNotChangeFocus()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.FocusUp();

        Assert.Same(el, mgr.FocusedElement);
    }

    [Fact]
    public void FocusUp_NoFocusedElement_DoesNotThrow()
    {
        var mgr = CreateManager();
        mgr.FocusUp(); // Must not throw
        Assert.Null(mgr.FocusedElement);
    }

    [Fact]
    public void FocusDirection_UnregisteredNeighborId_DoesNotChangeFocus()
    {
        var mgr = CreateManager();
        // NeighborDown = 99, which is not registered
        var el = new StubFocusable { TabIndex = 0, FocusNeighborDown = 99 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.FocusDown();

        Assert.Same(el, mgr.FocusedElement);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllElements()
    {
        var mgr = CreateManager();
        mgr.Register(new StubFocusable { TabIndex = 0 });
        mgr.Register(new StubFocusable { TabIndex = 1 });

        mgr.Clear();
        mgr.FocusNext();

        Assert.Null(mgr.FocusedElement);
    }

    [Fact]
    public void Clear_FiresOnFocusLostOnFocusedElement()
    {
        var mgr = CreateManager();
        var el = new StubFocusable { TabIndex = 0 };
        mgr.Register(el);
        mgr.SetFocus(el);

        mgr.Clear();

        Assert.Equal(1, el.FocusLostCount);
        Assert.Null(mgr.FocusedElement);
    }

    #endregion
}
