using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>Represents a collection of texture regions that make up all of our sprites.</summary>
public sealed class TextureAtlas
{
    private readonly Dictionary<string, TextureRegion> _regions = [];
    private readonly Dictionary<string, Animation> _animations = [];

    /// <summary>Gets or Sets the source texture represented by this texture atlas.</summary>
    public required Texture2D Texture { get; set; }

    /// <summary>Creates a new texture atlas.</summary>
    [SetsRequiredMembers]
    public TextureAtlas() { }
    /// <summary>Creates a new texture atlas with the specified texture.</summary>
    [SetsRequiredMembers]
    public TextureAtlas(Texture2D texture)
    {
        Texture = texture;
    }

    /// <summary>Adds a texture region to the atlas.</summary>
    public TextureRegion AddRegion(string name, int x, int y, int width, int height)
    {
        TextureRegion region = new(Texture, x, y, width, height);
        if (_regions.TryAdd(name, region))
        {
            return region;
        }
        throw new ArgumentException($"A texture region with the name '{name}' already exists in the atlas.");
    }

    /// <summary>Gets the region from this texture atlas with the specified name.</summary>
    /// <param name="name">The name of the region to retrieve.</param>
    public TextureRegion? GetRegion(string name)
        => _regions.TryGetValue(name, out TextureRegion region) ? region : null;

    /// <summary>Removes the region from this texture atlas with the specified name.</summary>
    /// <param name="name">The name of the texture region to remove.</param>
    public bool RemoveRegion(string name)
        => _regions.Remove(name);

    /// <summary>Removes all regions from this texture atlas.</summary>
    public void Clear()
        => _regions.Clear();

    /// <summary>Creates a new sprite using the region from this texture atlas with the specified name.</summary>
    /// <param name="regionName">The name of the region to create the sprite with.</param>
    /// <returns>A new Sprite using the texture region with the specified name.</returns>
    public Sprite CreateSprite(string regionName)
    {
        TextureRegion? region = GetRegion(regionName)
            ?? throw new ArgumentException($"No texture region with the name '{regionName}' exists in the atlas.");
        return new Sprite(region);
    }

    /// <summary>Adds the given animation to this texture atlas with the specified name.</summary>
    /// <param name="animationName">The name of the animation to add.</param>
    /// <param name="animation">The animation to add.</param>
    public void AddAnimation(string animationName, Animation animation)
    {
        if (_animations.TryAdd(animationName, animation))
        {
            return;
        }
        throw new ArgumentException($"An animation with the name '{animationName}' already exists in the atlas.");
    }

    /// <summary>Gets the animation from this texture atlas with the specified name.</summary>
    /// <param name="animationName">The name of the animation to retrieve.</param>
    /// <returns>The animation with the specified name.</returns>
    public Animation GetAnimation(string animationName)
    {
        return _animations[animationName];
    }

    /// <summary>Removes the animation with the specified name from this texture atlas.</summary>
    /// <param name="animationName">The name of the animation to remove.</param>
    /// <returns>true if the animation is removed successfully; otherwise, false.</returns>
    public bool RemoveAnimation(string animationName)
    {
        return _animations.Remove(animationName);
    }

    /// <summary>Creates a new animated sprite using the animation from this texture atlas with the specified name.</summary>
    /// <param name="animationName">The name of the animation to use.</param>
    /// <returns>A new AnimatedSprite using the animation with the specified name.</returns>
    public AnimatedSprite CreateAnimatedSprite(string animationName)
    {
        Animation animation = GetAnimation(animationName);
        return new AnimatedSprite(animation);
    }

    /// <summary>Creates a new texture atlas based a texture atlas xml configuration file.</summary>
    /// <param name="content">The content manager used to load the texture for the atlas.</param>
    /// <param name="fileName">The path to the xml file, relative to the content root directory.</param>
    /// <returns>The texture atlas created by this method.</returns>
    public static TextureAtlas FromFile(ContentManager content, string fileName)
    {
        TextureAtlas atlas = new();

        string filePath = Path.Combine(content.RootDirectory, fileName);

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);
        XDocument? doc = XDocument.Load(reader);
        XElement? root = doc?.Root;

        if (doc == null || root == null)
        {
            throw new InvalidOperationException("Failed to load the texture atlas XML file or the file is empty.");
        }

        // The <Texture> element contains the content path for the Texture2D to load.
        // So we will retrieve that value then use the content manager to load the texture.
        string? texturePath = root?.Element("Texture")?.Value;
        if (texturePath == null)
        {
            throw new InvalidOperationException("The <Texture> element is missing or empty in the texture atlas XML.");
        }
        atlas.Texture = content.Load<Texture2D>(texturePath);

        // The <Regions> element contains individual <Region> elements, each one describing
        // a different texture region within the atlas.
        //
        // Example:
        // <Regions>
        //      <Region name="spriteOne" x="0" y="0" width="32" height="32" />
        //      <Region name="spriteTwo" x="32" y="0" width="32" height="32" />
        // </Regions>
        //
        // So we retrieve all of the <Region> elements then loop through each one
        // and generate a new TextureRegion instance from it and add it to this atlas.
        IEnumerable<XElement> regions = root!.Element("Regions")?.Elements("Region") ?? [];

        foreach (var region in regions)
        {
            string? name = region.Attribute("name")?.Value;
            int x = int.Parse(region.Attribute("x")?.Value ?? "0");
            int y = int.Parse(region.Attribute("y")?.Value ?? "0");
            int width = int.Parse(region.Attribute("width")?.Value ?? "0");
            int height = int.Parse(region.Attribute("height")?.Value ?? "0");

            if (!string.IsNullOrEmpty(name))
            {
                atlas.AddRegion(name, x, y, width, height);
            }
        }

        // The <Animations> element contains individual <Animation> elements, each one describing
        // a different animation within the atlas.
        //
        // Example:
        // <Animations>
        //      <Animation name="animation" delay="100">
        //          <Frame region="spriteOne" />
        //          <Frame region="spriteTwo" />
        //      </Animation>
        // </Animations>
        //
        // So we retrieve all of the <Animation> elements then loop through each one
        // and generate a new Animation instance from it and add it to this atlas.
        var animationElements = root.Element("Animations")?.Elements("Animation") ?? [];

        foreach (var animationElement in animationElements)
        {
            string? name = animationElement.Attribute("name")?.Value;
            float delayInMilliseconds = float.Parse(animationElement.Attribute("delay")?.Value ?? "0");
            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

            List<TextureRegion> frames = [];

            IEnumerable<XElement> frameElements = animationElement.Elements("Frame") ?? [];
            foreach (var frameElement in frameElements)
            {
                string? regionName = frameElement.Attribute("region")?.Value ?? string.Empty;
                TextureRegion region = atlas.GetRegion(regionName);
                frames.Add(region);
            }

            Animation animation = new(frames, delay);
            if (!string.IsNullOrEmpty(name))
            {
                atlas.AddAnimation(name, animation);
            }
        }

        return atlas;
    }
}
