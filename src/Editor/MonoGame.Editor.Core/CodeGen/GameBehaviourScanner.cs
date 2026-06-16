using System.Reflection;
using System.Text.RegularExpressions;

namespace MonoGame.Editor.Core.CodeGen;

/// <summary>
/// Discovers <c>GameBehaviour</c> subclasses either by loading a compiled assembly
/// or by performing simple text-based source scanning (no Roslyn required).
/// </summary>
public sealed class GameBehaviourScanner
{
    private static readonly Regex _classRegex = new(
        @"class\s+(\w+)\s*:\s*([\w.]+)",
        RegexOptions.Compiled);

    /// <summary>Directories skipped during recursive source scan.</summary>
    private static readonly HashSet<string> _excludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", ".editor", "node_modules",
    };

    /// <summary>
    /// Scans a compiled DLL for concrete <c>GameBehaviour</c> subclasses.
    /// </summary>
    public static Task<IReadOnlyDictionary<string, TypeDescriptor>> ScanAssemblyAsync(string assemblyPath)
        => Task.Run<IReadOnlyDictionary<string, TypeDescriptor>>(() => ScanAssembly(assemblyPath));

    private static IReadOnlyDictionary<string, TypeDescriptor> ScanAssembly(string assemblyPath)
    {
        Dictionary<string, TypeDescriptor> result = new(StringComparer.Ordinal);

        if (!File.Exists(assemblyPath)) return result;

        try
        {
            Assembly asm = Assembly.LoadFrom(assemblyPath);
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).ToArray()!; }

            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t is null || t.IsAbstract || !t.IsClass) continue;
                if (!IsGameBehaviourSubclass(t)) continue;

                string fullName = t.FullName ?? t.Name;
                string shortName = t.Name;
                string ns = t.Namespace ?? string.Empty;

                result[fullName] = new TypeDescriptor(fullName, shortName, ns);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GameBehaviourScanner] Failed to scan assembly, continuing: {ex.Message}"); }

        return result;
    }

    /// <summary>
    /// Scans all <c>.cs</c> files under <paramref name="sourcePath"/> for classes
    /// that directly extend <c>GameBehaviour</c> (simple text analysis, no Roslyn).
    /// Skips <c>bin/</c>, <c>obj/</c>, <c>.git/</c> and other non-source directories.
    /// </summary>
    public static Task<IReadOnlyList<string>> ScanSourceAsync(string sourcePath)
        => Task.Run<IReadOnlyList<string>>(() => ScanSource(sourcePath));

    private static IReadOnlyList<string> ScanSource(string sourcePath)
    {
        List<string> result = [];
        if (!Directory.Exists(sourcePath)) return result;
        CollectMatches(sourcePath, result);
        return result;
    }

    private static void CollectMatches(string dir, List<string> result)
    {
        try
        {
            string[] files = Directory.GetFiles(dir, "*.cs");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string text = File.ReadAllText(files[i]);
                    MatchCollection matches = _classRegex.Matches(text);
                    for (int j = 0; j < matches.Count; j++)
                    {
                        Match m = matches[j];
                        string baseType = m.Groups[2].Value;
                        if (baseType == "GameBehaviour" || baseType.EndsWith(".GameBehaviour", StringComparison.Ordinal))
                            result.Add(m.Groups[1].Value);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GameBehaviourScanner] Could not read {files[i]}, skipping: {ex.Message}"); }
            }

            string[] subdirs = Directory.GetDirectories(dir);
            for (int i = 0; i < subdirs.Length; i++)
            {
                string name = Path.GetFileName(subdirs[i]);
                if (!_excludedDirs.Contains(name))
                    CollectMatches(subdirs[i], result);
            }
        }
        catch (UnauthorizedAccessException) { }
    }

    private static bool IsGameBehaviourSubclass(Type type)
    {
        Type? current = type.BaseType;
        while (current is not null)
        {
            if (current.Name == "GameBehaviour") return true;
            current = current.BaseType;
        }
        return false;
    }
}
