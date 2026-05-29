namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// Animates text character-by-character (typewriter effect) with a configurable characters-per-second rate.
/// Designed for zero heap allocations per frame; one <c>string</c> allocation occurs each time new characters
/// are revealed (i.e., once per batch of characters added, not once per character).
/// </summary>
public sealed class TypewriterEffect
{
    private readonly char[] _buffer;
    private int _charIndex;
    private int _fullTextLength;
    private float _accumulator;

    /// <summary>Gets or sets the number of characters revealed per second. Default is 30.</summary>
    public float CharsPerSecond { get; set; } = 30f;

    /// <summary>Gets a value indicating whether all characters have been revealed.</summary>
    public bool IsComplete => _charIndex >= _fullTextLength;

    /// <summary>Gets the currently visible portion of the text.</summary>
    public string CurrentText { get; private set; } = string.Empty;

    /// <summary>Raised when the last character has been revealed.</summary>
    public Action? OnComplete;

    /// <summary>
    /// Creates a new <see cref="TypewriterEffect"/> with an internal character buffer.
    /// </summary>
    /// <param name="maxCapacity">Maximum number of characters the buffer can hold. Defaults to 512.</param>
    public TypewriterEffect(int maxCapacity = 512)
    {
        _buffer = new char[maxCapacity];
    }

    /// <summary>
    /// Loads a new text string and resets the animation to the beginning.
    /// </summary>
    public void SetText(string text)
    {
        _charIndex = 0;
        _accumulator = 0f;
        _fullTextLength = Math.Min(text.Length, _buffer.Length);
        text.CopyTo(0, _buffer, 0, _fullTextLength);
        CurrentText = string.Empty;
    }

    /// <summary>
    /// Advances the typewriter by <paramref name="deltaTime"/> seconds, revealing new characters as needed.
    /// One <c>string</c> allocation occurs when at least one new character is added.
    /// </summary>
    public void Advance(float deltaTime)
    {
        if (IsComplete) return;

        _accumulator += deltaTime * CharsPerSecond;
        int toAdd = (int)_accumulator;
        _accumulator -= toAdd;

        if (toAdd <= 0) return;

        int prev = _charIndex;
        _charIndex = Math.Min(_charIndex + toAdd, _fullTextLength);

        if (_charIndex != prev)
            CurrentText = new string(_buffer, 0, _charIndex);

        if (IsComplete)
            OnComplete?.Invoke();
    }

    /// <summary>Reveals all remaining characters at once and fires <see cref="OnComplete"/> if not already complete.</summary>
    public void CompleteInstantly()
    {
        if (IsComplete) return;

        _charIndex = _fullTextLength;
        CurrentText = new string(_buffer, 0, _charIndex);
        OnComplete?.Invoke();
    }

    /// <summary>Resets the effect to its initial empty state without changing the buffer contents.</summary>
    public void Reset()
    {
        _charIndex = 0;
        _accumulator = 0f;
        CurrentText = string.Empty;
        _fullTextLength = 0;
    }
}
