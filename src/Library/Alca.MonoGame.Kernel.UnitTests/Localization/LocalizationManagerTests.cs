using System.Text.Json;
using Alca.MonoGame.Kernel.Localization;
using Microsoft.Extensions.Localization;

namespace Alca.MonoGame.Kernel.UnitTests.Localization;

public sealed class LocalizationManagerTests : IDisposable
{
    private static readonly string LocalizationDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Localization");

    private readonly LocalizationManager _sut = new();

    public LocalizationManagerTests()
    {
        Directory.CreateDirectory(LocalizationDir);
    }

    public void Dispose()
    {
        foreach (string file in Directory.GetFiles(LocalizationDir, "test_*.json"))
        {
            File.Delete(file);
        }
    }

    private static void WriteLanguageFile(string culture, Dictionary<string, string> strings)
    {
        string json = JsonSerializer.Serialize(strings);
        File.WriteAllText(Path.Combine(LocalizationDir, $"{culture}.json"), json);
    }

    // ── LoadLanguage ──────────────────────────────────────────────────────────────

    [Fact]
    public void LoadLanguage_ValidCulture_SetsCurrentCulture()
    {
        WriteLanguageFile("test_lc_a", []);

        _sut.LoadLanguage("test_lc_a");

        Assert.Equal("test_lc_a", _sut.CurrentCulture);
    }

    [Fact]
    public void LoadLanguage_ValidCulture_LoadsStrings()
    {
        WriteLanguageFile("test_lc_b", new() { ["greeting"] = "Hello", ["farewell"] = "Goodbye" });

        _sut.LoadLanguage("test_lc_b");

        Assert.Equal("Hello", _sut["greeting"].Value);
        Assert.Equal("Goodbye", _sut["farewell"].Value);
    }

    [Fact]
    public void LoadLanguage_SameCulture_DoesNotReloadStrings()
    {
        WriteLanguageFile("test_lc_c", new() { ["key"] = "First" });
        _sut.LoadLanguage("test_lc_c");

        WriteLanguageFile("test_lc_c", new() { ["key"] = "Second" });
        _sut.LoadLanguage("test_lc_c");

        Assert.Equal("First", _sut["key"].Value);
    }

    [Fact]
    public void LoadLanguage_NewCulture_ClearsPreviousStrings()
    {
        WriteLanguageFile("test_lc_d1", new() { ["old_key"] = "OldValue" });
        WriteLanguageFile("test_lc_d2", new() { ["new_key"] = "NewValue" });

        _sut.LoadLanguage("test_lc_d1");
        _sut.LoadLanguage("test_lc_d2");

        Assert.True(_sut["old_key"].ResourceNotFound);
    }

    [Fact]
    public void LoadLanguage_MissingFile_DoesNotThrow()
    {
        var ex = Record.Exception(() => _sut.LoadLanguage("test_lc_nonexistent_xyz"));
        Assert.Null(ex);
    }

    [Fact]
    public void LoadLanguage_NewCulture_RaisesCultureChanged()
    {
        WriteLanguageFile("test_lc_e", []);
        bool raised = false;
        _sut.CultureChanged += () => raised = true;

        _sut.LoadLanguage("test_lc_e");

        Assert.True(raised);
    }

    [Fact]
    public void LoadLanguage_SameCulture_DoesNotRaiseCultureChanged()
    {
        WriteLanguageFile("test_lc_f", []);
        _sut.LoadLanguage("test_lc_f");
        int count = 0;
        _sut.CultureChanged += () => count++;

        _sut.LoadLanguage("test_lc_f");

        Assert.Equal(0, count);
    }

    // ── IStringLocalizer indexer ──────────────────────────────────────────────────

    [Fact]
    public void Indexer_ExistingKey_ReturnsLocalizedValue()
    {
        WriteLanguageFile("test_idx_a", new() { ["menu.play"] = "Jugar" });
        _sut.LoadLanguage("test_idx_a");

        LocalizedString result = _sut["menu.play"];

        Assert.Equal("Jugar", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_MissingKey_SetsResourceNotFound_AndReturnsKey()
    {
        WriteLanguageFile("test_idx_b", []);
        _sut.LoadLanguage("test_idx_b");

        LocalizedString result = _sut["missing.key"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("missing.key", result.Value);
    }

    [Fact]
    public void IndexerWithArgs_ExistingKey_FormatsValue()
    {
        WriteLanguageFile("test_idx_c", new() { ["score"] = "Pts: {0}" });
        _sut.LoadLanguage("test_idx_c");

        LocalizedString result = _sut["score", 42];

        Assert.Equal("Pts: 42", result.Value);
    }

    // ── GetAllStrings ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllStrings_ReturnsAllLoadedPairs()
    {
        WriteLanguageFile("test_all_a", new() { ["a"] = "Alpha", ["b"] = "Beta" });
        _sut.LoadLanguage("test_all_a");

        List<LocalizedString> all = _sut.GetAllStrings(false).ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, s => s.Name == "a" && s.Value == "Alpha");
        Assert.Contains(all, s => s.Name == "b" && s.Value == "Beta");
    }

    // ── StringLocalizerExtensions.Get ─────────────────────────────────────────────

    [Fact]
    public void Get_ExistingKeyWithArgs_FormatsString()
    {
        WriteLanguageFile("test_get_a", new() { ["hud.score"] = "Score: {0}" });
        _sut.LoadLanguage("test_get_a");

        string result = _sut.Get("hud.score", 1500);

        Assert.Equal("Score: 1500", result);
    }

    [Fact]
    public void Get_MultipleArgs_FormatsAllPlaceholders()
    {
        WriteLanguageFile("test_get_b", new() { ["msg"] = "{0} has {1} lives" });
        _sut.LoadLanguage("test_get_b");

        string result = _sut.Get("msg", "Hero", 3);

        Assert.Equal("Hero has 3 lives", result);
    }

    [Fact]
    public void Get_MissingKey_ReturnsKeyAsFormattedFallback()
    {
        WriteLanguageFile("test_get_c", []);
        _sut.LoadLanguage("test_get_c");

        string result = _sut.Get("unknown.key");

        Assert.Equal("unknown.key", result);
    }

    // ── Resolved as IStringLocalizer from DI ──────────────────────────────────────

    [Fact]
    public void AsInterface_Indexer_ReturnsCorrectLocalizedString()
    {
        WriteLanguageFile("test_di_a", new() { ["title"] = "Game Title" });
        _sut.LoadLanguage("test_di_a");
        IStringLocalizer loc = _sut;

        LocalizedString result = loc["title"];

        Assert.Equal("Game Title", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void AsInterface_Get_ExtensionMethodAvailable()
    {
        WriteLanguageFile("test_di_b", new() { ["pts"] = "Points: {0}" });
        _sut.LoadLanguage("test_di_b");
        IStringLocalizer loc = _sut;

        string result = loc.Get("pts", 99);

        Assert.Equal("Points: 99", result);
    }
}
