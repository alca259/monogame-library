using System.Reflection;

namespace MonoGame.Editor.Core.Registry;

/// <summary>
/// Scans loaded assemblies for concrete types whose base-class chain includes a type
/// named <c>GameBehaviour</c>. Also tracks "pending" types found in source but not yet compiled.
/// </summary>
public sealed class GameObjectRegistry
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingTypeNames = new(StringComparer.Ordinal);

    /// <summary>All discovered <c>GameBehaviour</c> subclasses keyed by their full type name.</summary>
    public IReadOnlyDictionary<string, Type> RegisteredTypes => _types;

    /// <summary>
    /// Short type names found in source files but not yet compiled.
    /// Shown in the AddBehaviourDialog with grey italic style.
    /// </summary>
    public IReadOnlySet<string> PendingTypeNames => _pendingTypeNames;

    /// <summary>Scans all assemblies currently loaded in <see cref="AppDomain.CurrentDomain"/>.</summary>
    public void Scan() => Scan(AppDomain.CurrentDomain.GetAssemblies());

    /// <summary>Scans a specific set of <paramref name="assemblies"/> (primarily for unit testing).</summary>
    public void Scan(Assembly[] assemblies)
    {
        _types.Clear();
        for (int i = 0; i < assemblies.Length; i++)
        {
            try
            {
                Type[] types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    Type t = types[j];
                    if (t.IsAbstract || !t.IsClass) continue;
                    if (IsGameBehaviour(t))
                        _types[t.FullName ?? t.Name] = t;
                }
            }
            catch (ReflectionTypeLoadException) { }
            catch (NotSupportedException) { }
        }

        // Remove pending names that are now compiled
        _pendingTypeNames.RemoveWhere(name =>
        {
            foreach (string key in _types.Keys)
            {
                string shortName = GetShortName(key);
                if (string.Equals(shortName, name, StringComparison.Ordinal)) return true;
            }
            return false;
        });
    }

    /// <summary>
    /// Loads an external assembly from <paramref name="dllPath"/> and merges its
    /// <c>GameBehaviour</c> subclasses into <see cref="RegisteredTypes"/>.
    /// </summary>
    public Task ScanFromAssemblyAsync(string dllPath)
        => Task.Run(() => ScanFromAssembly(dllPath));

    private void ScanFromAssembly(string dllPath)
    {
        if (!File.Exists(dllPath)) return;

        try
        {
            Assembly assembly = Assembly.LoadFrom(dllPath);
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t.IsAbstract || !t.IsClass) continue;
                if (IsGameBehaviour(t))
                    _types[t.FullName ?? t.Name] = t;
            }

            // Remove pending names now compiled
            _pendingTypeNames.RemoveWhere(name =>
            {
                foreach (string key in _types.Keys)
                {
                    string shortName = GetShortName(key);
                    if (string.Equals(shortName, name, StringComparison.Ordinal)) return true;
                }
                return false;
            });
        }
        catch (ReflectionTypeLoadException) { }
        catch (BadImageFormatException) { }
        catch (FileLoadException) { }
    }

    /// <summary>
    /// Scans <paramref name="sourcePath"/> for <c>GameBehaviour</c> subclass names
    /// (text-based parse) and registers them as pending if not already compiled.
    /// </summary>
    public async Task ScanSourceAsync(string sourcePath)
    {
        IReadOnlyList<string> found = await CodeGen.GameBehaviourScanner
            .ScanSourceAsync(sourcePath)
            .ConfigureAwait(false);

        for (int i = 0; i < found.Count; i++)
        {
            string shortName = GetShortName(found[i]);
            // Only add as pending if not already in compiled registry
            bool alreadyCompiled = false;
            foreach (string key in _types.Keys)
            {
                if (string.Equals(GetShortName(key), shortName, StringComparison.Ordinal))
                {
                    alreadyCompiled = true;
                    break;
                }
            }
            if (!alreadyCompiled)
                _pendingTypeNames.Add(shortName);
        }
    }

    /// <summary>Walks the inheritance chain and returns <c>true</c> if any base type is named <c>GameBehaviour</c>.</summary>
    private static bool IsGameBehaviour(Type type)
    {
        Type? current = type.BaseType;
        while (current is not null)
        {
            if (current.Name == "GameBehaviour") return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string GetShortName(string fullName)
    {
        int dot = fullName.LastIndexOf('.');
        return dot >= 0 ? fullName[(dot + 1)..] : fullName;
    }
}
