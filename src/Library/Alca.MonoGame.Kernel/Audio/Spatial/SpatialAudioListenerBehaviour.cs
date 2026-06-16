using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Audio.Spatial;

/// <summary>
/// GameBehaviour that acts as the 3D audio listener in the scene.
/// Synchronises the <see cref="AudioController"/> listener with the entity's world-space
/// <see cref="TransformBehaviour.Position"/> (X, Y, Z) and forward direction each frame.
/// Attach to the camera or player entity in a 2.5D scene.
/// </summary>
public sealed class SpatialAudioListenerBehaviour : GameBehaviour
{
    private AudioController? _controller;

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary listener that drives global 3D audio positioning.
    /// Only one listener should have this set to true at a time. Default is true.
    /// </summary>
    public bool IsMain { get; set; } = true;

    /// <inheritdoc/>
    public override void Awake()
    {
        _controller = Entity.World.AudioController;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsMain || _controller is null) return;

        Vector3 position = Entity.Transform.Position;
        Vector3 forward = Vector3.TransformNormal(Vector3.Forward, Entity.Transform.LocalToWorldMatrix);

        if (forward == Vector3.Zero)
            forward = Vector3.Forward;

        _controller.UpdateListener(position, forward);
    }
}
