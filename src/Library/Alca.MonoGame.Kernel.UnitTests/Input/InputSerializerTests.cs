using Microsoft.Xna.Framework.Input;
using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UnitTests.Input;

public sealed class InputSerializerTests
{
    private static string TempFile() => Path.GetTempFileName();

    [Fact]
    public async Task Save_ThenLoad_PreservesActionNames()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("Jump", keys: [Keys.Space]));
        map.Register(new InputAction("Fire", mouseButtons: [MouseButton.Left]));

        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);
            InputActionMap loaded = await serializer.Load(path);

            Assert.NotNull(loaded.Get("Jump"));
            Assert.NotNull(loaded.Get("Fire"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Save_ThenLoad_PreservesKeyBindings()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("Move", keys: [Keys.W, Keys.Up]));

        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);
            InputActionMap loaded = await serializer.Load(path);

            InputAction? action = loaded.Get("Move");
            Assert.NotNull(action);

            InputBinding[] bindings = action.GetBindings();
            Assert.Equal(2, bindings.Length);
            Assert.All(bindings, b => Assert.Equal(DeviceType.Keyboard, b.DeviceType));
            Assert.Contains(bindings, b => b.Code == (int)Keys.W);
            Assert.Contains(bindings, b => b.Code == (int)Keys.Up);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Save_ThenLoad_PreservesMixedBindings()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("Action",
            keys: [Keys.Enter],
            padButtons: [Buttons.A],
            mouseButtons: [MouseButton.Left]));

        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);
            InputActionMap loaded = await serializer.Load(path);

            InputAction? action = loaded.Get("Action");
            Assert.NotNull(action);

            InputBinding[] bindings = action.GetBindings();
            Assert.Equal(3, bindings.Length);
            Assert.Single(bindings, b => b.DeviceType == DeviceType.Keyboard);
            Assert.Single(bindings, b => b.DeviceType == DeviceType.Gamepad);
            Assert.Single(bindings, b => b.DeviceType == DeviceType.Mouse);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Save_ThenLoad_EmptyMap_ReturnsEmptyMap()
    {
        var map = new InputActionMap();
        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);
            InputActionMap loaded = await serializer.Load(path);

            Assert.Empty(loaded.GetAllActions());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Save_ProducesValidJsonFile()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("Jump", keys: [Keys.Space]));

        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);

            string json = await File.ReadAllTextAsync(path);
            Assert.Contains("Jump", json);
            Assert.Contains("Keyboard", json);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Load_SaveMultipleActions_PreservesRegistrationOrder()
    {
        var map = new InputActionMap();
        map.Register(new InputAction("A"));
        map.Register(new InputAction("B"));
        map.Register(new InputAction("C"));

        string path = TempFile();
        try
        {
            var serializer = new InputSerializer();
            await serializer.Save(map, path);
            InputActionMap loaded = await serializer.Load(path);

            IReadOnlyList<InputAction> all = loaded.GetAllActions();
            Assert.Equal(3, all.Count);
            Assert.Equal("A", all[0].Name);
            Assert.Equal("B", all[1].Name);
            Assert.Equal("C", all[2].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
