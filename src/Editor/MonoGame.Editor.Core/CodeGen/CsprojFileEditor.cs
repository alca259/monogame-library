using System.Xml;

namespace MonoGame.Editor.Core.CodeGen;

/// <summary>
/// Utilities for reading and modifying <c>.csproj</c> files to ensure generated files are included.
/// SDK-style projects use implicit wildcards — this class checks before editing.
/// </summary>
public static class CsprojFileEditor
{
    /// <summary>
    /// Ensures <paramref name="absoluteFilePath"/> is included in the project.
    /// If the project already covers the file via a glob, this is a no-op.
    /// Otherwise, an explicit <c>Compile Include</c> item is added.
    /// </summary>
    public static async Task EnsureFileIncludedAsync(string csprojPath, string absoluteFilePath)
    {
        if (IsFileCoveredByGlob(csprojPath, absoluteFilePath)) return;

        string content = await File.ReadAllTextAsync(csprojPath).ConfigureAwait(false);

        XmlDocument doc = new();
        doc.LoadXml(content);

        XmlNamespaceManager ns = new(doc.NameTable);

        // Find or create an ItemGroup for Compile items
        XmlNode? root = doc.DocumentElement;
        if (root is null) return;

        string relPath = Path.GetRelativePath(Path.GetDirectoryName(csprojPath)!, absoluteFilePath)
            .Replace('/', '\\');

        XmlElement compileItem = doc.CreateElement("Compile");
        compileItem.SetAttribute("Include", relPath);

        // Look for existing ItemGroup; add to first found or create new
        XmlNode? itemGroup = root.SelectSingleNode("ItemGroup[Compile]");
        if (itemGroup is null)
        {
            itemGroup = doc.CreateElement("ItemGroup");
            root.AppendChild(itemGroup);
        }

        itemGroup.AppendChild(compileItem);

        using StringWriter sw = new();
        using XmlTextWriter xw = new(sw);
        xw.Formatting = Formatting.Indented;
        doc.WriteTo(xw);

        await File.WriteAllTextAsync(csprojPath, sw.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns <c>true</c> if the project is an SDK-style project with default compile items active,
    /// meaning <paramref name="absoluteFilePath"/> is already implicitly included.
    /// </summary>
    public static bool IsFileCoveredByGlob(string csprojPath, string absoluteFilePath)
    {
        if (!File.Exists(csprojPath)) return false;

        string content;
        try
        {
            content = File.ReadAllText(csprojPath);
        }
        catch
        {
            return false;
        }

        // SDK-style projects include all *.cs implicitly unless explicitly disabled
        bool hasSdkAttribute = content.Contains("Sdk=\"Microsoft.NET.Sdk\"", StringComparison.OrdinalIgnoreCase)
                            || content.Contains("Sdk=\"Microsoft.NET.Sdk.", StringComparison.OrdinalIgnoreCase);

        if (!hasSdkAttribute) return false;

        bool defaultsDisabled = content.Contains("<EnableDefaultCompileItems>false</EnableDefaultCompileItems>",
            StringComparison.OrdinalIgnoreCase);

        if (defaultsDisabled) return false;

        // Check for explicit Remove of this file or its parent folder
        string relPath = Path.GetRelativePath(Path.GetDirectoryName(csprojPath)!, absoluteFilePath);
        return !content.Contains(relPath, StringComparison.OrdinalIgnoreCase);
    }
}
