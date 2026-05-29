using System.Reflection;

namespace MonoGame.Editor.Core.Registry;

/// <summary>
/// Analiza los ensamblados cargados en busca de tipos concretos cuya cadena de herencia
/// incluya un tipo llamado <c>GameBehaviour</c>. También lleva un seguimiento de los tipos
/// "pendientes" encontrados en el código fuente pero aún no compilados.
/// </summary>
public sealed class GameObjectRegistry
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingTypeNames = new(StringComparer.Ordinal);

    /// <summary>Todas las subclases de <c>GameBehaviour</c> descubiertas, indexadas por su nombre completo de tipo.</summary>
    public IReadOnlyDictionary<string, Type> RegisteredTypes => _types;

    /// <summary>
    /// Nombres cortos de tipos encontrados en archivos de origen pero aún no compilados.
    /// Se muestran en el AddBehaviourDialog con estilo gris cursivo.
    /// </summary>
    public IReadOnlySet<string> PendingTypeNames => _pendingTypeNames;

    /// <summary>Analiza todos los ensamblados cargados actualmente en <see cref="AppDomain.CurrentDomain"/>.</summary>
    public void Scan() => Scan(AppDomain.CurrentDomain.GetAssemblies());

    /// <summary>Analiza un conjunto específico de <paramref name="assemblies"/> (principalmente para pruebas unitarias).</summary>
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

        // Eliminar los nombres pendientes que ya están compilados
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
    /// Carga un ensamblado externo desde <paramref name="dllPath"/> y fusiona sus
    /// subclases de <c>GameBehaviour</c> en <see cref="RegisteredTypes"/>.
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

            // Eliminar los nombres pendientes ya compilados
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
    /// Analiza <paramref name="sourcePath"/> en busca de nombres de subclases de <c>GameBehaviour</c>
    /// (análisis basado en texto) y los registra como pendientes si no están ya compilados.
    /// </summary>
    public async Task ScanSourceAsync(string sourcePath)
    {
        IReadOnlyList<string> found = await CodeGen.GameBehaviourScanner
            .ScanSourceAsync(sourcePath)
            .ConfigureAwait(false);

        for (int i = 0; i < found.Count; i++)
        {
            string shortName = GetShortName(found[i]);
            // Solo agregar como pendiente si no está ya en el registro compilado
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

    /// <summary>Recorre la cadena de herencia y devuelve <c>true</c> si algún tipo base se llama <c>GameBehaviour</c>.</summary>
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
