using Microsoft.Xna.Framework.Input;
using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UnitTests.Input;

public sealed class InputActionMapTests
{
    #region Helpers

    private static KeyboardState KbDown(params Keys[] keys) => new(keys);
    private static KeyboardState KbUp() => new();

    private static GamePadState PadUp() => new(
        new GamePadThumbSticks(), new GamePadTriggers(),
        new GamePadButtons(), new GamePadDPad());

    private static readonly MouseState NoMouse = new();

    #endregion

    [Fact]
    public void Register_Action_CanBeRetrievedByName()
    {
        var map = new InputActionMap();
        var action = new InputAction("Jump", keys: [Keys.Space]);
        map.Register(action);
        Assert.Same(action, map.Get("Jump"));
    }

    [Fact]
    public void Register_SameNameTwice_ReplacesExisting()
    {
        var map = new InputActionMap();
        var first  = new InputAction("Jump", keys: [Keys.Space]);
        var second = new InputAction("Jump", keys: [Keys.Up]);
        map.Register(first);
        map.Register(second);
        Assert.Same(second, map.Get("Jump"));
    }

    [Fact]
    public void Unregister_ExistingAction_RemovesIt()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("Jump", keys: [Keys.Space]));
        map.Unregister("Jump");
        Assert.Null(map.Get("Jump"));
    }

    [Fact]
    public void Unregister_NonExistentName_DoesNotThrow()
    {
        var map = new InputActionMap();
        var ex = Record.Exception(() => map.Unregister("NonExistent"));
        Assert.Null(ex);
    }

    [Fact]
    public void Get_UnknownName_ReturnsNull()
    {
        var map = new InputActionMap();
        Assert.Null(map.Get("Unknown"));
    }

    [Fact]
    public void GetAllActions_ReturnsActionsInRegistrationOrder()
    {
        var map = new InputActionMap();
        var a = new InputAction("A");
        var b = new InputAction("B");
        var c = new InputAction("C");
        map.Register(a);
        map.Register(b);
        map.Register(c);

        IReadOnlyList<InputAction> all = map.GetAllActions();
        Assert.Equal(3, all.Count);
        Assert.Same(a, all[0]);
        Assert.Same(b, all[1]);
        Assert.Same(c, all[2]);
    }

    [Fact]
    public void Update_PropagatesKeyPressToRegisteredAction()
    {
        var map = new InputActionMap();
        var action = new InputAction("Jump", keys: [Keys.Space]);
        map.Register(action);

        map.Update(KbDown(Keys.Space), KbUp(), NoMouse, NoMouse, PadUp(), PadUp());

        Assert.True(action.IsPressed);
        Assert.True(action.IsHeld);
    }

    [Fact]
    public void Update_KeyReleased_ActionIsReleasedTrue()
    {
        var map = new InputActionMap();
        var action = new InputAction("Jump", keys: [Keys.Space]);
        map.Register(action);

        map.Update(KbUp(), KbDown(Keys.Space), NoMouse, NoMouse, PadUp(), PadUp());

        Assert.True(action.IsReleased);
        Assert.False(action.IsHeld);
    }

    [Fact]
    public void Update_AfterUnregister_ActionStateNotUpdated()
    {
        var map = new InputActionMap();
        var action = new InputAction("Jump", keys: [Keys.Space]);
        map.Register(action);
        map.Unregister("Jump");

        map.Update(KbDown(Keys.Space), KbUp(), NoMouse, NoMouse, PadUp(), PadUp());

        Assert.False(action.IsPressed);
    }

    [Fact]
    public void Register_ReplacingAction_DoesNotGrowList()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("A", keys: [Keys.A]));
        map.Register(new InputAction("A", keys: [Keys.B]));

        Assert.Single(map.GetAllActions());
    }
}
