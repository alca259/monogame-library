using Alca.MonoGame.Kernel.Persistence;

namespace Alca.MonoGame.Kernel.UnitTests.Persistence;

public sealed class SaveManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SaveManager _sut;

    public SaveManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _sut = new SaveManager(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsAllTypes()
    {
        var obj = new AllTypesObject
        {
            BoolVal = true,
            IntVal = 42,
            FloatVal = 3.14f,
            StringVal = "hello",
            Vec2Val = new Vector2(1f, 2f),
        };

        await _sut.SaveAsync("slot1", [obj]);

        var loaded = new AllTypesObject();
        bool result = await _sut.LoadAsync("slot1", [loaded]);

        Assert.True(result);
        Assert.Equal(obj.BoolVal, loaded.BoolVal);
        Assert.Equal(obj.IntVal, loaded.IntVal);
        Assert.Equal(obj.FloatVal, loaded.FloatVal, 5);
        Assert.Equal(obj.StringVal, loaded.StringVal);
        Assert.Equal(obj.Vec2Val.X, loaded.Vec2Val.X, 5);
        Assert.Equal(obj.Vec2Val.Y, loaded.Vec2Val.Y, 5);
    }

    [Fact]
    public async Task GetSlotsAsync_ReturnsOnlyExistingSlots()
    {
        await _sut.SaveAsync("alpha", []);
        await _sut.SaveAsync("beta", []);

        IReadOnlyList<SaveSlot> slots = await _sut.GetSlotsAsync();

        Assert.Equal(2, slots.Count);
        Assert.Contains(slots, s => s.Name == "alpha");
        Assert.Contains(slots, s => s.Name == "beta");
    }

    [Fact]
    public async Task DeleteSlot_RemovesBothFiles()
    {
        await _sut.SaveAsync("to-delete", []);
        Assert.True(_sut.SlotExists("to-delete"));

        _sut.DeleteSlot("to-delete");

        Assert.False(_sut.SlotExists("to-delete"));
        Assert.False(File.Exists(Path.Combine(_tempDir, "to-delete.meta.json")));
    }

    [Fact]
    public async Task LoadAsync_WhenSlotMissing_ReturnsFalse()
    {
        bool result = await _sut.LoadAsync("nonexistent", []);

        Assert.False(result);
    }

    [Fact]
    public void SaveDataWriter_ReadWriter_RoundTrip_Vector2()
    {
        var expected = new Vector2(12.5f, -7.3f);
        byte[] bytes;

        using (var writer = new SaveDataWriter())
        {
            writer.Write(expected);
            bytes = writer.ToReadOnlySpan().ToArray();
        }

        var reader = new SaveDataReader(bytes);
        Vector2 actual = reader.ReadVector2();

        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
    }

    [Fact]
    public void SaveDataWriter_ReadWriter_RoundTrip_String()
    {
        const string expected = "GameSave_Unicode_Ñ";
        byte[] bytes;

        using (var writer = new SaveDataWriter())
        {
            writer.Write(expected);
            bytes = writer.ToReadOnlySpan().ToArray();
        }

        var reader = new SaveDataReader(bytes);
        string actual = reader.ReadString();

        Assert.Equal(expected, actual);
    }

    // ── Helper types ──────────────────────────────────────────────────────────

    private sealed class AllTypesObject : ISaveable
    {
        public bool BoolVal;
        public int IntVal;
        public float FloatVal;
        public string StringVal = string.Empty;
        public Vector2 Vec2Val;

        public void Save(SaveDataWriter writer)
        {
            writer.Write(BoolVal);
            writer.Write(IntVal);
            writer.Write(FloatVal);
            writer.Write(StringVal);
            writer.Write(Vec2Val);
        }

        public void Load(SaveDataReader reader)
        {
            BoolVal = reader.ReadBool();
            IntVal = reader.ReadInt();
            FloatVal = reader.ReadFloat();
            StringVal = reader.ReadString();
            Vec2Val = reader.ReadVector2();
        }
    }
}
