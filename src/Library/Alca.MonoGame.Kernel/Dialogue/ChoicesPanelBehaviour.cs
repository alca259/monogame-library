using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// A <see cref="GameBehaviour"/> that renders a panel of selectable choices for a branching <see cref="DialogueLine"/>.
/// Subscribes to <see cref="DialogueManager"/> events to show the panel only when choices are available.
/// Press keys 1–4 to select the corresponding choice.
/// </summary>
public sealed class ChoicesPanelBehaviour : GameBehaviour
{
    private readonly DialogueManager _manager;
    private readonly SpriteFont _font;
    private readonly Texture2D? _buttonTexture;
    private readonly int _maxChoices;

    private bool _visible;
    private readonly DialogueChoice[] _currentChoices;
    private readonly int[] _originalIndices;
    private int _choiceCount;

    /// <summary>Gets or sets the screen-space position of the first choice button.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Gets or sets the height in pixels of each choice button. Default is 40.</summary>
    public float ButtonHeight { get; set; } = 40f;

    /// <summary>Gets or sets the width in pixels of each choice button. Default is 300.</summary>
    public float ButtonWidth { get; set; } = 300f;

    /// <summary>Gets or sets the vertical gap in pixels between consecutive buttons. Default is 5.</summary>
    public float ButtonSpacing { get; set; } = 5f;

    /// <summary>Gets or sets the background color for unselected buttons. Default is <see cref="Color.DarkGray"/>.</summary>
    public Color NormalColor { get; set; } = Color.DarkGray;

    /// <summary>Gets or sets the background color for the hovered button. Default is <see cref="Color.Gray"/>.</summary>
    public Color HoverColor { get; set; } = Color.Gray;

    /// <summary>Gets or sets the color used to render choice text. Default is <see cref="Color.White"/>.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Gets the per-choice input actions. Defaults map choice 1–4 to D1–D4 and X/Y/LB/RB.</summary>
    public InputAction[] ChoiceActions { get; }

    private static readonly Keys[] _defaultChoiceKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4];
    private static readonly Buttons[] _defaultChoiceButtons = [Buttons.X, Buttons.Y, Buttons.LeftShoulder, Buttons.RightShoulder];

    /// <summary>
    /// Creates a new <see cref="ChoicesPanelBehaviour"/>.
    /// </summary>
    /// <param name="manager">The <see cref="DialogueManager"/> this panel is driven by.</param>
    /// <param name="font">Font used to render choice text.</param>
    /// <param name="buttonTexture">Optional 1×1 white texture for drawing button backgrounds. When <see langword="null"/> no background is drawn.</param>
    /// <param name="maxChoices">Maximum number of choices that can be displayed simultaneously. Defaults to 4.</param>
    public ChoicesPanelBehaviour(DialogueManager manager, SpriteFont font, Texture2D? buttonTexture = null, int maxChoices = 4)
    {
        _manager = manager;
        _font = font;
        _buttonTexture = buttonTexture;
        _maxChoices = maxChoices;
        _currentChoices = new DialogueChoice[maxChoices];
        _originalIndices = new int[maxChoices];

        ChoiceActions = new InputAction[maxChoices];
        for (int i = 0; i < maxChoices; i++)
        {
            Keys[] keys = i < _defaultChoiceKeys.Length ? [_defaultChoiceKeys[i]] : [];
            Buttons[] buttons = i < _defaultChoiceButtons.Length ? [_defaultChoiceButtons[i]] : [];
            ChoiceActions[i] = new InputAction($"Choice_{i + 1}", keys, buttons);
        }
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
        if (!_visible || !Enabled) return;

        if (Kernel.Core.Input is not null)
        {
            KeyboardState currKb = Kernel.Core.Input.Keyboard.CurrentState;
            KeyboardState prevKb = Kernel.Core.Input.Keyboard.PreviousState;
            MouseState currMs = Kernel.Core.Input.Mouse.CurrentState;
            MouseState prevMs = Kernel.Core.Input.Mouse.PreviousState;
            GamePadState currPad = Kernel.Core.Input.GamePads[0].CurrentState;
            GamePadState prevPad = Kernel.Core.Input.GamePads[0].PreviousState;

            for (int i = 0; i < _choiceCount; i++)
                ChoiceActions[i].Update(currKb, prevKb, currMs, prevMs, currPad, prevPad);
        }

        for (int i = 0; i < _choiceCount; i++)
        {
            if (ChoiceActions[i].IsPressed)
            {
                SelectChoice(i);
                break;
            }
        }
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!_visible) return;

        float stepY = ButtonHeight + ButtonSpacing;

        for (int i = 0; i < _choiceCount; i++)
        {
            var buttonPos = new Vector2(Position.X, Position.Y + i * stepY);

            if (_buttonTexture is not null)
            {
                var rect = new Rectangle((int)buttonPos.X, (int)buttonPos.Y, (int)ButtonWidth, (int)ButtonHeight);
                spriteBatch.Draw(_buttonTexture, rect, NormalColor);
            }

            spriteBatch.DrawString(_font, _currentChoices[i].LocalizationKey,
                new Vector2(buttonPos.X + 8f, buttonPos.Y + (ButtonHeight - _font.LineSpacing) * 0.5f),
                TextColor);
        }
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        _manager.OnLineChanged -= HandleLineChanged;
        _manager.OnEnded -= HandleEnded;
    }

    private void HandleLineChanged(DialogueLine line)
    {
        if (!line.HasChoices)
        {
            _visible = false;
            return;
        }

        _choiceCount = 0;
        _visible = true;

        for (int i = 0; i < line.Choices.Length && _choiceCount < _maxChoices; i++)
        {
            DialogueChoice choice = line.Choices[i];
            if (_manager.EvaluateCondition(choice.Condition))
            {
                _currentChoices[_choiceCount] = choice;
                _originalIndices[_choiceCount] = i;
                _choiceCount++;
            }
        }
    }

    private void HandleEnded()
    {
        _visible = false;
    }

    private void SelectChoice(int index)
    {
        if ((uint)index >= (uint)_choiceCount) return;
        _manager.SelectChoice(_originalIndices[index]);
    }
}
