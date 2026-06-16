using System.Reflection;

namespace MonoGame.Editor.Maui.Drawers;

/// <summary>
/// Registro singleton de <see cref="BehaviourEditor"/> personalizados.
/// Escanea los ensamblados cargados en busca de clases decoradas con
/// <see cref="MonoGame.Editor.Core.Attributes.CustomBehaviourEditorAttribute"/> al iniciarse.
/// </summary>
public static class BehaviourEditorRegistry
{
    private static readonly Dictionary<Type, BehaviourEditor> _byType = new();
    private static readonly Dictionary<string, BehaviourEditor> _byName = new();

    /// <summary>
    /// Escanea <see cref="AppDomain.CurrentDomain"/> en busca de subclases de
    /// <see cref="BehaviourEditor"/> con <see cref="MonoGame.Editor.Core.Attributes.CustomBehaviourEditorAttribute"/>.
    /// Llamar una vez al arrancar la app.
    /// </summary>
    public static void Initialize()
    {
        _byType.Clear();
        _byName.Clear();

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { ScanAssembly(asm); }
            catch { /* ensamblado no accesible */ }
        }
    }

    private static void ScanAssembly(Assembly asm)
    {
        foreach (Type type in asm.GetTypes())
        {
            if (type.IsAbstract || !typeof(BehaviourEditor).IsAssignableFrom(type)) continue;

            var attr = type.GetCustomAttribute<MonoGame.Editor.Core.Attributes.CustomBehaviourEditorAttribute>();
            if (attr is null) continue;

            try
            {
                var editor = (BehaviourEditor)Activator.CreateInstance(type)!;

                // Registro por tipo concreto (cuando está disponible en compilación)
                if (attr.TargetType is not null)
                {
                    _byType[attr.TargetType] = editor;
                    if (attr.TargetType.FullName is { } full) _byName[full] = editor;
                    if (attr.TargetType.AssemblyQualifiedName is { } aqn) _byName[aqn] = editor;
                }

                // Registro por nombre de tipo (cuando el tipo no está referenciado en compilación)
                if (attr.TargetTypeName is { } name)
                    _byName[name] = editor;
            }
            catch { }
        }
    }

    /// <summary>Devuelve el editor registrado para <paramref name="behaviourType"/>, o <c>null</c> si no hay ninguno.</summary>
    public static BehaviourEditor? GetEditor(Type behaviourType)
        => _byType.TryGetValue(behaviourType, out BehaviourEditor? e) ? e : null;

    /// <summary>Devuelve el editor registrado para el nombre de tipo indicado, o <c>null</c>.</summary>
    public static BehaviourEditor? GetEditor(string behaviourTypeName)
    {
        if (_byName.TryGetValue(behaviourTypeName, out BehaviourEditor? e)) return e;

        // Intento con el nombre corto (antes de la primera coma en el AssemblyQualifiedName)
        int comma = behaviourTypeName.IndexOf(',');
        if (comma > 0)
        {
            string shortKey = behaviourTypeName[..comma].Trim();
            if (_byName.TryGetValue(shortKey, out e)) return e;
        }

        return null;
    }

    /// <summary>Asigna el contexto de navegación y proyecto al editor antes de usarlo.</summary>
    internal static void PrepareEditor(BehaviourEditor editor)
    {
        editor.Navigation = Services.DialogService.Navigation;
        editor.ProjectRootPath = EditorContext.Instance.ActiveProject?.RootPath;
    }
}
