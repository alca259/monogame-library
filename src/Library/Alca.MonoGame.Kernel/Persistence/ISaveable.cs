namespace Alca.MonoGame.Kernel.Persistence;

/// <summary>Implement on any object that should participate in save/load operations.</summary>
public interface ISaveable
{
    /// <summary>Writes this object's state to <paramref name="writer"/>.</summary>
    void Save(SaveDataWriter writer);

    /// <summary>Restores this object's state from <paramref name="reader"/>.</summary>
    void Load(SaveDataReader reader);
}
