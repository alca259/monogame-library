using System.Text.Json;

namespace Alca.MonoGame.Kernel.Persistence;

/// <summary>
/// Manages save slots on disk. Each slot consists of a binary <c>.sav</c> file
/// (serialized via <see cref="ISaveable"/>) and a JSON <c>.meta.json</c> sidecar (<see cref="SaveSlot"/>).
/// </summary>
public sealed class SaveManager
{
    private const string SaveExtension = ".sav";
    private const string MetaExtension = ".meta.json";

    private readonly string _rootPath;

    /// <summary>
    /// Initializes a new <see cref="SaveManager"/> rooted at the default application-data saves folder.
    /// </summary>
    public SaveManager() : this(BuildDefaultPath()) { }

    /// <summary>Initializes a new <see cref="SaveManager"/> rooted at <paramref name="rootPath"/>.</summary>
    public SaveManager(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    #region Public API
    /// <summary>Returns <c>true</c> if a save file exists for <paramref name="slotName"/>.</summary>
    public bool SlotExists(string slotName) => File.Exists(SavePath(slotName));

    /// <summary>
    /// Serializes all <paramref name="objects"/> and writes the binary data plus metadata to disk.
    /// </summary>
    public async Task SaveAsync(string slotName, IEnumerable<ISaveable> objects,
        float playTimeSeconds = 0f, string? thumbnailPath = null,
        CancellationToken ct = default)
    {
        byte[] payload;
        using (var writer = new SaveDataWriter())
        {
            foreach (var obj in objects)
                obj.Save(writer);

            payload = writer.ToReadOnlySpan().ToArray();
        }

        string savePath = SavePath(slotName);
        string metaPath = MetaPath(slotName);

        var slot = new SaveSlot
        {
            Name = slotName,
            Timestamp = DateTimeOffset.UtcNow,
            PlayTimeSeconds = playTimeSeconds,
            ThumbnailPath = thumbnailPath,
        };

        byte[] metaBytes = JsonSerializer.SerializeToUtf8Bytes(slot);

        await Task.Run(async () =>
        {
            await File.WriteAllBytesAsync(savePath, payload, ct).ConfigureAwait(false);
            await File.WriteAllBytesAsync(metaPath, metaBytes, ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the binary save data for <paramref name="slotName"/> and calls <see cref="ISaveable.Load"/>
    /// on each object in order. Returns <c>false</c> if the slot does not exist.
    /// </summary>
    public async Task<bool> LoadAsync(string slotName, IEnumerable<ISaveable> objects,
        CancellationToken ct = default)
    {
        string savePath = SavePath(slotName);
        if (!File.Exists(savePath))
            return false;

        byte[] payload = await Task.Run(() => File.ReadAllBytes(savePath), ct).ConfigureAwait(false);

        var reader = new SaveDataReader(payload);
        foreach (var obj in objects)
            obj.Load(reader);

        return true;
    }

    /// <summary>Returns metadata for all slots currently on disk, sorted by <see cref="SaveSlot.Timestamp"/> descending.</summary>
    public async Task<IReadOnlyList<SaveSlot>> GetSlotsAsync(CancellationToken ct = default)
    {
        string[] metaFiles = await Task.Run(
            () => Directory.GetFiles(_rootPath, "*" + MetaExtension), ct).ConfigureAwait(false);

        var slots = new List<SaveSlot>(metaFiles.Length);
        foreach (string path in metaFiles)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
                SaveSlot? slot = JsonSerializer.Deserialize<SaveSlot>(bytes);
                if (slot is not null)
                    slots.Add(slot);
            }
            catch (Exception)
            {
                // Skip corrupt metadata files.
            }
        }

        slots.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return slots;
    }

    /// <summary>Deletes the <c>.sav</c> and <c>.meta.json</c> files for <paramref name="slotName"/>. No-op if the slot does not exist.</summary>
    public void DeleteSlot(string slotName)
    {
        string savePath = SavePath(slotName);
        string metaPath = MetaPath(slotName);

        if (File.Exists(savePath)) File.Delete(savePath);
        if (File.Exists(metaPath)) File.Delete(metaPath);
    }
    #endregion

    #region Internal helpers
    private string SavePath(string slotName) => Path.Combine(_rootPath, slotName + SaveExtension);
    private string MetaPath(string slotName) => Path.Combine(_rootPath, slotName + MetaExtension);

    private static string BuildDefaultPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Alca.MonoGame", "saves");
    }
    #endregion
}
