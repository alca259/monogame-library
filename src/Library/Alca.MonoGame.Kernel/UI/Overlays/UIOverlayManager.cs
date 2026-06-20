using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UI.Overlays;

/// <summary>Manages floating overlay elements (dropdowns, tooltips) drawn on top of the main UI tree.</summary>
public sealed class UIOverlayManager
{
    private readonly List<UIElement> _overlays = new(8);

    /// <summary>Adds an overlay to be updated and drawn each frame. No-op if already registered.</summary>
    public void Show(UIElement overlay)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (ReferenceEquals(_overlays[i], overlay)) return;
        }

        _overlays.Add(overlay);
    }

    /// <summary>Removes an overlay from the active set. No-op if not registered.</summary>
    public void Hide(UIElement overlay)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (ReferenceEquals(_overlays[i], overlay))
            {
                _overlays.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>Returns true when the given overlay is currently visible.</summary>
    public bool IsShowing(UIElement overlay)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (ReferenceEquals(_overlays[i], overlay)) return true;
        }

        return false;
    }

    /// <summary>Updates all enabled overlays. Call once per frame before Draw.</summary>
    public void Update(GameTime gameTime)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i].IsEnabled)
                _overlays[i].Update(gameTime);
        }
    }

    /// <summary>Draws all visible overlays on top of the main UI tree.</summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i].IsVisible)
                _overlays[i].Draw(spriteBatch);
        }
    }
}
