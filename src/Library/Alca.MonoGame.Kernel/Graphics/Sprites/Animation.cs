namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>Represents an animation consisting of multiple frames.</summary>
/// <remarks>This class manage frame sequences and timing for animations.</remarks>
public sealed class Animation
{
    /// <summary>The texture regions that make up the frames of this animation.</summary>
    /// <remarks>The order of the regions within the collection are the order that the frames should be displayed in.</remarks>
    public List<TextureRegion> Frames { get; set; } = [];
    /// <summary>The amount of time to delay between each frame before moving to the next frame for this animation.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);
    /// <summary>Gets or sets the optional identifier for this animation clip.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this animation loops back to the first frame after the last. Default is <c>true</c>.</summary>
    public bool IsLooping { get; set; } = true;
    /// <summary>Gets or sets the playback speed multiplier for this clip. Values greater than 1 play faster. Default is <c>1.0</c>.</summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Creates a new animation.</summary>
    public Animation() {}

    /// <summary>Creates a new animation with the specified frames and delay.</summary>
    /// <param name="frames">An ordered collection of the frames for this animation.</param>
    /// <param name="delay">The amount of time to delay between each frame of this animation.</param>
    public Animation(List<TextureRegion> frames, TimeSpan delay)
    {
        Frames = frames;
        Delay = delay;
    }
}
