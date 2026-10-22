using UnityEngine;

namespace FeatherMedals;

public static class TextureCompositing
{
    
    public static Color32[] ReadPixels(Texture source, int width, int height)
    {
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        var readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            return readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(readable);
        }
    }

    public static Color32[] MirrorHorizontally(Color32[] pixels, int width, int height)
    {
        var mirrored = new Color32[pixels.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                mirrored[y * width + x] = pixels[y * width + (width - 1 - x)];
            }
        }
        return mirrored;
    }

    public static Color32 Tint(Color32 pixel, Color tint)
    {
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.r * tint.r), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.g * tint.g), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.b * tint.b), 0, 255),
            pixel.a);
    }

    // Standard alpha "over".
    public static Color32 Over(Color32 under, Color32 over)
    {
        float overAlpha = over.a / 255f;
        float underAlpha = under.a / 255f;
        float alpha = overAlpha + underAlpha * (1f - overAlpha);
        if (alpha <= 0f)
        {
            return new Color32(0, 0, 0, 0);
        }

        float r = (over.r * overAlpha + under.r * underAlpha * (1f - overAlpha)) / alpha;
        float g = (over.g * overAlpha + under.g * underAlpha * (1f - overAlpha)) / alpha;
        float b = (over.b * overAlpha + under.b * underAlpha * (1f - overAlpha)) / alpha;
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(b), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
    }

    // Tinted source over target, source bottom left corner at (left, bottom). Parts outside the target are cut off.
    public static void Paste(Color32[] target, int targetWidth, int targetHeight,
        Color32[] source, int sourceWidth, int sourceHeight,
        int left, int bottom, Color tint)
    {
        for (int y = 0; y < sourceHeight; y++)
        {
            int targetY = bottom + y;
            if (targetY < 0 || targetY >= targetHeight)
                continue;
            for (int x = 0; x < sourceWidth; x++)
            {
                int targetX = left + x;
                if (targetX < 0 || targetX >= targetWidth)
                    continue;
                int index = targetY * targetWidth + targetX;
                target[index] = Over(target[index], Tint(source[y * sourceWidth + x], tint));
            }
        }
    }

    public static Texture2D MakeTexture(Color32[] pixels, int width, int height, string name)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}