using Alca.MonoGame.Kernel.Graphics.Sprites;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Sprites;

[Collection(GraphicsCollection.Name)]
public sealed class AnimatedSpriteTests
{
    private readonly GraphicsDeviceFixture _fx;

    public AnimatedSpriteTests(GraphicsDeviceFixture fx) => _fx = fx;

    private Animation MakeAnimation(int frameCount, int delayMs = 100, bool looping = true)
    {
        Texture2D texture = new(_fx.GraphicsDevice, frameCount * 16, 16);
        List<TextureRegion> frames = new(frameCount);
        for (int i = 0; i < frameCount; i++)
            frames.Add(new TextureRegion(texture, i * 16, 0, 16, 16));
        return new Animation(frames, TimeSpan.FromMilliseconds(delayMs)) { IsLooping = looping };
    }

    private static GameTime Tick(double ms) => new(TimeSpan.Zero, TimeSpan.FromMilliseconds(ms));

    [Fact]
    public void Update_WhenLooping_WrapsToFirstFrame()
    {
        Animation anim = MakeAnimation(2, 100, looping: true);
        AnimatedSprite sprite = new(anim);

        sprite.Update(Tick(100)); // → frame 1
        sprite.Update(Tick(100)); // → wraps to frame 0

        Assert.Equal(anim.Frames[0], sprite.Region);
        Assert.True(sprite.IsPlaying);
        Assert.False(sprite.IsComplete);
    }

    [Fact]
    public void Update_WhenNotLooping_StopsOnLastFrame()
    {
        Animation anim = MakeAnimation(2, 100, looping: false);
        AnimatedSprite sprite = new(anim);

        sprite.Update(Tick(100)); // → frame 1
        sprite.Update(Tick(100)); // → tries frame 2, clamps to last (1)
        sprite.Update(Tick(100)); // already stopped

        Assert.Equal(anim.Frames[1], sprite.Region);
        Assert.True(sprite.IsComplete);
        Assert.False(sprite.IsPlaying);
    }

    [Fact]
    public void Update_WhenNotLooping_InvokesOnComplete()
    {
        Animation anim = MakeAnimation(2, 100, looping: false);
        AnimatedSprite sprite = new(anim);
        int callCount = 0;
        sprite.OnComplete = () => callCount++;

        sprite.Update(Tick(100)); // → frame 1
        sprite.Update(Tick(100)); // → complete, fires OnComplete
        sprite.Update(Tick(100)); // already stopped — no second fire

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Pause_FreezesCurrentFrame()
    {
        Animation anim = MakeAnimation(2, 100);
        AnimatedSprite sprite = new(anim);

        sprite.Pause();
        sprite.Update(Tick(300)); // would advance 3 frames if playing
        sprite.Update(Tick(300));

        Assert.Equal(anim.Frames[0], sprite.Region);
        Assert.False(sprite.IsPlaying);
    }

    [Fact]
    public void Stop_ResetsToFrame0AndClearsIsComplete()
    {
        Animation anim = MakeAnimation(2, 100, looping: false);
        AnimatedSprite sprite = new(anim);

        sprite.Update(Tick(100)); // → frame 1
        sprite.Update(Tick(100)); // → complete

        Assert.True(sprite.IsComplete);

        sprite.Stop();

        Assert.False(sprite.IsComplete);
        Assert.False(sprite.IsPlaying);
        Assert.Equal(anim.Frames[0], sprite.Region);
    }

    [Fact]
    public void PlaybackSpeed_DoublesFrameRate()
    {
        Animation animNormal = MakeAnimation(2, 100);
        Animation animFast = MakeAnimation(2, 100);
        AnimatedSprite normal = new(animNormal);
        AnimatedSprite fast = new(animFast) { PlaybackSpeed = 2.0f };

        normal.Update(Tick(50)); // 50ms < 100ms → no advance
        fast.Update(Tick(50));   // 50ms at 2× → effective 100ms → advances

        Assert.Equal(animNormal.Frames[0], normal.Region);
        Assert.Equal(animFast.Frames[1], fast.Region);
    }

    [Fact]
    public void Play_WhenComplete_ResetsToFrame0AndRestarts()
    {
        Animation anim = MakeAnimation(2, 100, looping: false);
        AnimatedSprite sprite = new(anim);

        sprite.Update(Tick(100));
        sprite.Update(Tick(100)); // complete

        Assert.True(sprite.IsComplete);

        sprite.Play();

        Assert.False(sprite.IsComplete);
        Assert.True(sprite.IsPlaying);
        Assert.Equal(anim.Frames[0], sprite.Region);
    }

    [Fact]
    public void Resume_AfterPause_ContinuesFromCurrentFrame()
    {
        Animation anim = MakeAnimation(3, 100);
        AnimatedSprite sprite = new(anim);

        sprite.Update(Tick(100)); // → frame 1
        sprite.Pause();
        sprite.Resume(); // should not reset

        Assert.True(sprite.IsPlaying);
        Assert.Equal(anim.Frames[1], sprite.Region);
    }
}
