namespace MonoGame.Editor.Core.Assets;

/// <summary>Clasifica un archivo de asset según su rol en el proyecto de juego.</summary>
public enum AssetType
{
    /// <summary>Tipo de archivo no reconocido o no compatible.</summary>
    Unknown,

    /// <summary>Archivo de imagen usado como textura (png, jpg, bmp, tga, …).</summary>
    Texture,

    /// <summary>Archivo de audio (wav, mp3, ogg, wma).</summary>
    Audio,

    /// <summary>Fuente de mapa de bits o sprite (.spritefont, .fnt).</summary>
    Font,

    /// <summary>Mapa o tileset de Tiled (.tmx, .tsx).</summary>
    TiledMap,

    /// <summary>Descriptor de escena del editor (.scene.json).</summary>
    Scene,

    /// <summary>Definición de prefab (.prefab.json).</summary>
    Prefab,

    /// <summary>Efecto de partículas (.particles.json).</summary>
    Particles,

    /// <summary>Animación de sprite (.anim.json).</summary>
    Animation,

    /// <summary>Mapa de acciones de entrada (.input.json).</summary>
    InputMap,

    /// <summary>Archivo fuente de C# (.cs).</summary>
    Script,

    /// <summary>Metadatos de sprite con bordes de 9 porciones y configuración de importación (.sprite.json).</summary>
    Sprite,

    /// <summary>Descriptor de material con shader y sobreescrituras de propiedades (.mat.json).</summary>
    Material,

    /// <summary>Descriptor de tema de UI con rutas de textura NineSlice e insets de borde por tipo de control (.uitheme.json).</summary>
    UITheme,
}
