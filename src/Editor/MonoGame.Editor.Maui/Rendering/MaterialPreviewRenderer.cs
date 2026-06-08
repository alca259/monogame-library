using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MonoGame.Editor.Core.Models;

namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// Software-renders a Phong-shaded UV sphere preview for an <see cref="EditorMaterial"/>
/// and returns the result as PNG bytes for display in a MAUI <see cref="Microsoft.Maui.Controls.Image"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public static class MaterialPreviewRenderer
{
    #region Constants

    private const int Size = 256;
    private const float R = 0.9f;

    #endregion

    #region Static fields

    private static readonly Vector3 _lightDir = Vector3.Normalize(new Vector3(0.6f, 0.8f, 0.5f));
    private static readonly Vector3 _viewDir  = new(0f, 0f, 1f);
    private static readonly Vector3 _bgColor  = new(0.22f, 0.22f, 0.22f);
    private static readonly Vector3 _ambient  = new(0.18f, 0.18f, 0.22f);
    private static readonly Vector3 _f0Base   = new(0.04f);

    #endregion

    #region Public API

    /// <summary>
    /// Renders a Phong-shaded sphere preview of the given material and returns PNG bytes.
    /// Safe to call on a background thread.
    /// </summary>
    /// <param name="material">Material to preview; <see langword="null"/> renders a white sphere.</param>
    /// <param name="projectContentRoot">Absolute path to the Content folder for texture lookup.</param>
    public static byte[] Render(EditorMaterial? material, string projectContentRoot)
    {
        Vector3 diffuseColor = Vector3.One;
        float metallic   = 0f;
        float smoothness = 0.5f;
        int tw = 0, th = 0;
        byte[]? texels = null;

        if (material is not null)
        {
            diffuseColor = ReadColor(material, "AlbedoColor", "TintColor");
            metallic     = Math.Clamp(ReadFloat(material, "Metallic"),        0f, 1f);
            smoothness   = Math.Clamp(ReadFloat(material, "Smoothness", 0.5f), 0f, 1f);
            (tw, th, texels) = TryLoadTexels(material, "AlbedoTexture", projectContentRoot);
        }

        float r2        = R * R;
        float shininess = 4f + smoothness * 124f;
        var   rowBuffer = new byte[Size * 4];

        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        BitmapData bd = bmp.LockBits(
            new Rectangle(0, 0, Size, Size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float nx = (px + 0.5f) / Size * 2f - 1f;
                float ny = -((py + 0.5f) / Size * 2f - 1f);
                float d2 = nx * nx + ny * ny;

                Vector3 col;
                if (d2 > r2)
                {
                    col = _bgColor;
                }
                else
                {
                    // N is already unit-length: |(nx/R, ny/R, nz)|² = d²/R² + (R²-d²)/R² = 1
                    float nz = MathF.Sqrt(r2 - d2) / R;
                    var N = new Vector3(nx / R, ny / R, nz);

                    Vector3 albedo = texels is not null
                        ? SampleTexels(texels, tw, th, N) * diffuseColor
                        : diffuseColor;

                    Vector3 f0 = Vector3.Lerp(_f0Base, albedo, metallic);

                    float ndotl = MathF.Max(0f, Vector3.Dot(N, _lightDir));
                    Vector3 diffuse = albedo * (1f - metallic) * ndotl;

                    Vector3 H     = Vector3.Normalize(_lightDir + _viewDir);
                    float   ndoth = MathF.Max(0f, Vector3.Dot(N, H));
                    Vector3 specular = f0 * MathF.Pow(ndoth, shininess);

                    col = _ambient * albedo + diffuse + specular;
                    col = Vector3.Clamp(col, Vector3.Zero, Vector3.One);
                    col = GammaCorrect(col);
                }

                // Format32bppArgb in memory: B, G, R, A
                int i = px * 4;
                rowBuffer[i + 0] = (byte)(col.Z * 255f);
                rowBuffer[i + 1] = (byte)(col.Y * 255f);
                rowBuffer[i + 2] = (byte)(col.X * 255f);
                rowBuffer[i + 3] = 255;
            }

            Marshal.Copy(rowBuffer, 0, bd.Scan0 + py * bd.Stride, Size * 4);
        }

        bmp.UnlockBits(bd);

        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    #endregion

    #region Helpers

    private static Vector3 GammaCorrect(Vector3 c) =>
        new(MathF.Pow(c.X, 1f / 2.2f), MathF.Pow(c.Y, 1f / 2.2f), MathF.Pow(c.Z, 1f / 2.2f));

    private static Vector3 ReadColor(EditorMaterial mat, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (mat.Properties.TryGetValue(key, out EditorMaterialProperty? p)
                && p.Type == EditorMaterialPropertyType.Color
                && p.Data?.Length >= 3)
                return new Vector3(p.Data[0], p.Data[1], p.Data[2]);
        }
        return Vector3.One;
    }

    private static float ReadFloat(EditorMaterial mat, string key, float fallback = 0f)
    {
        if (mat.Properties.TryGetValue(key, out EditorMaterialProperty? p)
            && p.Type == EditorMaterialPropertyType.Float
            && p.Data?.Length >= 1)
            return p.Data[0];
        return fallback;
    }

    private static (int w, int h, byte[]? texels) TryLoadTexels(
        EditorMaterial mat, string key, string contentRoot)
    {
        if (!mat.Properties.TryGetValue(key, out EditorMaterialProperty? p)
            || p.Type != EditorMaterialPropertyType.Texture2D
            || string.IsNullOrEmpty(p.TexturePath)
            || string.IsNullOrEmpty(contentRoot))
            return (0, 0, null);

        foreach (string ext in (string[])[".png", ".jpg", ".jpeg", ".bmp"])
        {
            string full = Path.Combine(contentRoot, p.TexturePath + ext);
            if (!File.Exists(full)) continue;
            try { return LoadTexels(full); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MaterialPreview] Could not load texture {full}, skipping: {ex.Message}"); }
        }
        return (0, 0, null);
    }

    private static (int w, int h, byte[] texels) LoadTexels(string path)
    {
        using var src = new Bitmap(path);
        using var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
            g.DrawImage(src, 0, 0);

        BitmapData bd = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var texels = new byte[bmp.Width * bmp.Height * 4];
        for (int y = 0; y < bmp.Height; y++)
            Marshal.Copy(bd.Scan0 + y * bd.Stride, texels, y * bmp.Width * 4, bmp.Width * 4);

        bmp.UnlockBits(bd);
        return (bmp.Width, bmp.Height, texels);
    }

    private static Vector3 SampleTexels(byte[] texels, int tw, int th, Vector3 N)
    {
        float u = 0.5f + MathF.Atan2(N.Z, N.X) / (2f * MathF.PI);
        float v = 0.5f - MathF.Asin(Math.Clamp(N.Y, -1f, 1f)) / MathF.PI;
        int x = Math.Clamp((int)(u * tw), 0, tw - 1);
        int y = Math.Clamp((int)(v * th), 0, th - 1);
        int idx = (y * tw + x) * 4;
        // Format32bppArgb in memory: B, G, R, A
        return new Vector3(texels[idx + 2] / 255f, texels[idx + 1] / 255f, texels[idx + 0] / 255f);
    }

    #endregion
}
