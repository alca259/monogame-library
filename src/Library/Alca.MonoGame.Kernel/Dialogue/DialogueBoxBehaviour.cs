using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// A <see cref="GameBehaviour"/> that renders a dialogue box for the active <see cref="DialogueManager"/> conversation.
/// Subscribes to manager events to show/hide itself and drive a <see cref="TypewriterEffect"/>.
/// Pressing Space or Enter advances the dialogue or completes the current typewriter animation.
/// </summary>
public sealed class DialogueBoxBehaviour : GameBehaviour
{
    private readonly DialogueManager _manager;
    private readonly SpriteFont _font;
    private readonly Texture2D? _backgroundTexture;
    private readonly TypewriterEffect _typewriter = new TypewriterEffect();

    private DialogueLine _currentLine;

    /// <summary>Gets or sets a value indicating whether this dialogue box is visible and updated.</summary>
    public bool Visible { get; set; } = false;

    /// <summary>Gets or sets the screen-space position of the dialogue box.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Gets or sets the size in pixels of the dialogue box. Defaults to 600×120.</summary>
    public Vector2 Size { get; set; } = new Vector2(600f, 120f);

    /// <summary>Gets or sets the background tint color of the dialogue panel. Defaults to semi-transparent black.</summary>
    public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 200);

    /// <summary>Gets or sets the text color used when drawing the dialogue content. Default is <see cref="Color.White"/>.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Gets or sets the pixel padding around content inside the dialogue box. Default is 10.</summary>
    public int Padding { get; set; } = 10;

    /// <summary>
    /// Creates a new <see cref="DialogueBoxBehaviour"/>.
    /// </summary>
    /// <param name="manager">The <see cref="DialogueManager"/> this box is driven by.</param>
    /// <param name="font">Font used to render speaker name and dialogue text.</param>
    /// <param name="backgroundTexture">Optional 1×1 white texture used to draw the panel background. When <see langword="null"/> no background is drawn.</param>
    /// <param name="portraitTexture">Optional portrait texture. Reserved for future portrait rendering.</param>
    public DialogueBoxBehaviour(DialogueManager manager, SpriteFont font, Texture2D? backgroundTexture = null, Texture2D? portraitTexture = null)
    {
        _manager = manager;
        _font = font;
        _backgroundTexture = backgroundTexture;
    }

    /// <inheritdoc/>
    public override void Awake()
    {
        _manager.OnLineChanged += HandleLineChanged;
        _manager.OnEnded += HandleEnded;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!Visible || !Enabled) return;

        _typewriter.Advance((float)gameTime.ElapsedGameTime.TotalSeconds);

        bool advancePressed =
            Kernel.Core.Input?.IsKeyPressed(Keys.Space) == true ||
            Kernel.Core.Input?.IsKeyPressed(Keys.Enter) == true;

        if (!advancePressed) return;

        if (!_typewriter.IsComplete)
            _typewriter.CompleteInstantly();
        else
            _manager.Advance();
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!Visible) return;

        int px = (int)Position.X;
        int py = (int)Position.Y;
        int sw = (int)Size.X;
        int sh = (int)Size.Y;

        if (_backgroundTexture is not null)
            spriteBatch.Draw(_backgroundTexture, new Rectangle(px, py, sw, sh), BackgroundColor);

        if (!string.IsNullOrEmpty(_currentLine.SpeakerId))
            spriteBatch.DrawString(_font, _currentLine.SpeakerId,
                new Vector2(px + Padding, py + Padding), Color.Yellow);

        spriteBatch.DrawString(_font, _typewriter.CurrentText,
            new Vector2(px + Padding, py + _font.LineSpacing + Padding * 2), TextColor);
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        _manager.OnLineChanged -= HandleLineChanged;
        _manager.OnEnded -= HandleEnded;
    }

    private void HandleLineChanged(DialogueLine line)
    {
        _currentLine = line;
        _typewriter.SetText(line.LocalizationKey);
        Visible = true;
    }

    private void HandleEnded()
    {
        Visible = false;
    }
}
