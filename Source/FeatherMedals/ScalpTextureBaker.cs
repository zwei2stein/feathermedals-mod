using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    // Bakes scalp picture: ScalpBase tinted by skin color, hair texture tinted by hair color on top (1:1 aligned).
    // Single entry point: Bake. Caller owns the returned texture.
    public static class ScalpTextureBaker
    {
        private const string BasePath = "Things/Pawn/Humanlike/Scalp/ScalpBase";

        public static Texture2D Bake(Color skinColor, HairDef hairDef, Color hairColor)
        {
            var baseTexture = ContentFinder<Texture2D>.Get(BasePath, false);
            if (baseTexture == null)
            {
                Log.ErrorOnce("FeatherTrophies: scalp base texture not found at " + BasePath, 7354811);
                return null;
            }

            int width = baseTexture.width;
            int height = baseTexture.height;

            Color32[] result = ReadPixels(baseTexture, width, height);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Tint(result[i], skinColor);
            }

            var hairTexture = HairTexture(hairDef);
            if (hairTexture != null)
            {
                Color32[] hair = ReadPixels(hairTexture, width, height);
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Over(result[i], Tint(hair[i], hairColor));
                }
            }

            var baked = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ScalpBaked",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            baked.SetPixels32(result);
            // Second argument frees the CPU copy.
            baked.Apply(false, true);
            return baked;
        }

        private static Texture2D HairTexture(HairDef hairDef)
        {
            if (hairDef == null || hairDef.texPath.NullOrEmpty())
            {
                return null;
            }

            // Same lookup order as the pawn hair graphic: south first, then plain.
            return ContentFinder<Texture2D>.Get(hairDef.texPath + "_south", false)
                   ?? ContentFinder<Texture2D>.Get(hairDef.texPath, false);
        }

        // Works with non-readable textures: blit to render texture of requested size, read back.
        private static Color32[] ReadPixels(Texture source, int width, int height)
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

        private static Color32 Tint(Color32 pixel, Color tint)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.r * tint.r), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.g * tint.g), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.b * tint.b), 0, 255),
                pixel.a);
        }

        // Standard alpha "over".
        private static Color32 Over(Color32 under, Color32 over)
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
    }
}