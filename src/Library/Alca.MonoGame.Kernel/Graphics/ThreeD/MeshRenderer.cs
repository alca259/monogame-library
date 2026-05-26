namespace Alca.MonoGame.Kernel.Graphics.ThreeD;

using Camera;

/// <summary>Loads and draws a 3D model asset, with optional texture override and frustum culling support.</summary>
public sealed class MeshRenderer
{
    private Model? _model;
    private Texture2D? _textureOverride;
    private Matrix[]? _boneTransforms;

    /// <summary>Gets the world-space bounding sphere of the loaded model, or <see cref="BoundingSphere.CreateMerged"/> of all meshes.</summary>
    public BoundingSphere BoundingSphere { get; private set; }

    /// <summary>Loads the model from the content pipeline.</summary>
    public void Load(ContentManager content, string assetName)
    {
        _model = content.Load<Model>(assetName);
        _boneTransforms = new Matrix[_model.Bones.Count];

        BoundingSphere merged = new(Vector3.Zero, 0f);
        for (int i = 0; i < _model.Meshes.Count; i++)
            merged = BoundingSphere.CreateMerged(merged, _model.Meshes[i].BoundingSphere);

        BoundingSphere = merged;
    }

    /// <summary>Overrides the texture used by all <see cref="BasicEffect"/> mesh parts.</summary>
    public void SetTexture(Texture2D texture)
    {
        _textureOverride = texture;
    }

    /// <summary>Draws the model using the given camera matrices and world transform.</summary>
    public void Draw(Camera3D camera, Matrix worldTransform)
    {
        if (_model is null)
            return;

        if (_boneTransforms is not null)
            _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        for (int meshIdx = 0; meshIdx < _model.Meshes.Count; meshIdx++)
        {
            ModelMesh mesh = _model.Meshes[meshIdx];
            Matrix boneWorld = (_boneTransforms is not null && mesh.ParentBone is not null)
                ? _boneTransforms[mesh.ParentBone.Index] * worldTransform
                : worldTransform;

            for (int partIdx = 0; partIdx < mesh.MeshParts.Count; partIdx++)
            {
                if (mesh.MeshParts[partIdx].Effect is BasicEffect effect)
                {
                    effect.World      = boneWorld;
                    effect.View       = camera.View;
                    effect.Projection = camera.Projection;

                    if (_textureOverride is not null)
                    {
                        effect.TextureEnabled = true;
                        effect.Texture        = _textureOverride;
                    }

                    effect.EnableDefaultLighting();
                }
            }

            mesh.Draw();
        }
    }
}
