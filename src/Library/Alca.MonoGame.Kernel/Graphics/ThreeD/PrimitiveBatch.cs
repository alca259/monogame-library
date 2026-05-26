namespace Alca.MonoGame.Kernel.Graphics.ThreeD;

using Camera;

/// <summary>Renders immediate-mode primitives (lines, wireframes) without heap allocations per frame.</summary>
public sealed class PrimitiveBatch : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private readonly VertexPositionColor[] _vertices;
    private int _vertexCount;
    private PrimitiveType _primitiveType;
    private bool _hasBegun;
    private bool _disposed;

    /// <summary>Creates the primitive batch with a fixed vertex buffer of <paramref name="capacity"/> vertices.</summary>
    public PrimitiveBatch(GraphicsDevice graphicsDevice, int capacity = 1024)
    {
        _graphicsDevice = graphicsDevice;
        _vertices       = new VertexPositionColor[capacity];
        _effect         = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled    = false,
        };
    }

    /// <summary>Begins a batch of primitives with the given camera and primitive type.</summary>
    public void Begin(Camera3D camera, PrimitiveType type)
    {
        _effect.View       = camera.View;
        _effect.Projection = camera.Projection;
        _effect.World      = Matrix.Identity;

        _primitiveType = type;
        _vertexCount   = 0;
        _hasBegun      = true;
    }

    /// <summary>Adds a vertex to the current batch. Flushes automatically if the buffer is full.</summary>
    public void AddVertex(Vector3 pos, Color color)
    {
        if (!_hasBegun)
            return;

        if (_vertexCount == _vertices.Length)
            Flush();

        _vertices[_vertexCount++] = new VertexPositionColor(pos, color);
    }

    /// <summary>Flushes all buffered vertices to the GPU and resets the index.</summary>
    public void End()
    {
        if (!_hasBegun)
            return;

        Flush();
        _hasBegun = false;
    }

    /// <summary>Draws a line between two world-space points.</summary>
    public void DrawLine(Vector3 a, Vector3 b, Color color)
    {
        AddVertex(a, color);
        AddVertex(b, color);
    }

    /// <summary>Draws a wireframe sphere approximated by three orthogonal circles.</summary>
    public void DrawWireSphere(Vector3 center, float radius, Color color, int segments = 16)
    {
        float step = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);

            DrawLine(
                center + new Vector3(radius * MathF.Cos(a0), radius * MathF.Sin(a0), 0f),
                center + new Vector3(radius * MathF.Cos(a1), radius * MathF.Sin(a1), 0f),
                color);

            DrawLine(
                center + new Vector3(radius * MathF.Cos(a0), 0f, radius * MathF.Sin(a0)),
                center + new Vector3(radius * MathF.Cos(a1), 0f, radius * MathF.Sin(a1)),
                color);

            DrawLine(
                center + new Vector3(0f, radius * MathF.Cos(a0), radius * MathF.Sin(a0)),
                center + new Vector3(0f, radius * MathF.Cos(a1), radius * MathF.Sin(a1)),
                color);
        }
    }

    /// <summary>Draws a wireframe axis-aligned box.</summary>
    public void DrawWireBox(BoundingBox box, Color color)
    {
        Vector3 min = box.Min;
        Vector3 max = box.Max;

        Vector3 v000 = min;
        Vector3 v100 = new(max.X, min.Y, min.Z);
        Vector3 v010 = new(min.X, max.Y, min.Z);
        Vector3 v110 = new(max.X, max.Y, min.Z);
        Vector3 v001 = new(min.X, min.Y, max.Z);
        Vector3 v101 = new(max.X, min.Y, max.Z);
        Vector3 v011 = new(min.X, max.Y, max.Z);
        Vector3 v111 = max;

        // Bottom face
        DrawLine(v000, v100, color);
        DrawLine(v100, v110, color);
        DrawLine(v110, v010, color);
        DrawLine(v010, v000, color);

        // Top face
        DrawLine(v001, v101, color);
        DrawLine(v101, v111, color);
        DrawLine(v111, v011, color);
        DrawLine(v011, v001, color);

        // Vertical edges
        DrawLine(v000, v001, color);
        DrawLine(v100, v101, color);
        DrawLine(v110, v111, color);
        DrawLine(v010, v011, color);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _effect.Dispose();
        _disposed = true;
    }

    private void Flush()
    {
        if (_vertexCount == 0)
            return;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            int primitiveCount = _primitiveType switch
            {
                PrimitiveType.LineList      => _vertexCount / 2,
                PrimitiveType.LineStrip     => _vertexCount - 1,
                PrimitiveType.TriangleList  => _vertexCount / 3,
                PrimitiveType.TriangleStrip => _vertexCount - 2,
                _                           => _vertexCount / 2,
            };

            _graphicsDevice.DrawUserPrimitives(_primitiveType, _vertices, 0, primitiveCount);
        }

        _vertexCount = 0;
    }
}
