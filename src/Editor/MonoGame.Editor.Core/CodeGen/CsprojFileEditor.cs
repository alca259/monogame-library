using System.Xml;

namespace MonoGame.Editor.Core.CodeGen;

/// <summary>
/// Utilidades para leer y modificar archivos <c>.csproj</c> y garantizar que los archivos generados estén incluidos.
/// Los proyectos de estilo SDK usan comodines implícitos — esta clase verifica antes de editar.
/// </summary>
public static class CsprojFileEditor
{
    /// <summary>
    /// Garantiza que <paramref name="absoluteFilePath"/> esté incluido en el proyecto.
    /// Si el proyecto ya cubre el archivo mediante un glob, esta operación no hace nada.
    /// En caso contrario, se añade un elemento <c>Compile Include</c> explícito.
    /// </summary>
    public static async Task EnsureFileIncludedAsync(string csprojPath, string absoluteFilePath)
    {
        if (IsFileCoveredByGlob(csprojPath, absoluteFilePath)) return;

        string content = await File.ReadAllTextAsync(csprojPath).ConfigureAwait(false);

        XmlDocument doc = new();
        doc.LoadXml(content);

        XmlNamespaceManager ns = new(doc.NameTable);

        // Buscar o crear un ItemGroup para elementos Compile
        XmlNode? root = doc.DocumentElement;
        if (root is null) return;

        string relPath = Path.GetRelativePath(Path.GetDirectoryName(csprojPath)!, absoluteFilePath)
            .Replace('/', '\\');

        XmlElement compileItem = doc.CreateElement("Compile");
        compileItem.SetAttribute("Include", relPath);

        // Buscar un ItemGroup existente; añadir al primero encontrado o crear uno nuevo
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
    /// Devuelve <c>true</c> si el proyecto es de estilo SDK con los elementos de compilación predeterminados activos,
    /// lo que significa que <paramref name="absoluteFilePath"/> ya está incluido implícitamente.
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

        // Los proyectos de estilo SDK incluyen todos los *.cs implícitamente a menos que se deshabilite explícitamente
        bool hasSdkAttribute = content.Contains("Sdk=\"Microsoft.NET.Sdk\"", StringComparison.OrdinalIgnoreCase)
                            || content.Contains("Sdk=\"Microsoft.NET.Sdk.", StringComparison.OrdinalIgnoreCase);

        if (!hasSdkAttribute) return false;

        bool defaultsDisabled = content.Contains("<EnableDefaultCompileItems>false</EnableDefaultCompileItems>",
            StringComparison.OrdinalIgnoreCase);

        if (defaultsDisabled) return false;

        // Verificar si existe un Remove explícito de este archivo o su carpeta padre
        string relPath = Path.GetRelativePath(Path.GetDirectoryName(csprojPath)!, absoluteFilePath);
        return !content.Contains(relPath, StringComparison.OrdinalIgnoreCase);
    }
}
