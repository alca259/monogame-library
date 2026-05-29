namespace MonoGame.Editor.Core.Assets;

/// <summary>Classifies an asset file by its role in the game project.</summary>
public enum AssetType
{
    /// <summary>Unrecognized or unsupported file type.</summary>
    Unknown,

    /// <summary>Image file used as a texture (png, jpg, bmp, tga, …).</summary>
    Texture,

    /// <summary>Audio file (wav, mp3, ogg, wma).</summary>
    Audio,

    /// <summary>Bitmap or sprite font (.spritefont, .fnt).</summary>
    Font,

    /// <summary>Tiled map or tileset (.tmx, .tsx).</summary>
    TiledMap,

    /// <summary>Editor scene descriptor (.scene.json).</summary>
    Scene,

    /// <summary>Prefab definition (.prefab.json).</summary>
    Prefab,

    /// <summary>Particle effect (.particles.json).</summary>
    Particles,

    /// <summary>Sprite animation (.anim.json).</summary>
    Animation,

    /// <summary>Input action map (.input.json).</summary>
    InputMap,

    /// <summary>C# source file (.cs).</summary>
    Script,

    /// <summary>Sprite metadata with 9-slice borders and import settings (.sprite.json).</summary>
    Sprite,

    /// <summary>Material descriptor with shader and property overrides (.mat.json).</summary>
    Material,
}
