using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    public static class ScalpTextureFactory
    {
        private const string BasePath = "Things/Pawn/Humanlike/Scalp/ScalpBase";

        public static Texture2D Bake(Color skinColor, HairDef hairDef, Color hairColor, Rot4 hairFacing)
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

            var hairTexture = HairTexture(hairDef, hairFacing, out bool mirror);
            if (hairTexture != null)
            {
                Color32[] hair = ReadPixels(hairTexture, width, height);
                if (mirror)
                    hair = MirrorHorizontally(hair, width, height);
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

        private static Texture2D HairTexture(HairDef hairDef, Rot4 facing, out bool mirror)
        {
            mirror = false;
            if (hairDef == null || hairDef.texPath.NullOrEmpty())
            {
                return null;
            }

            var path = hairDef.texPath;
            if (facing == Rot4.East || facing == Rot4.West)
            {
                // like the pawn hair graphic: a missing side is drawn as the other side mirrored
                var ownSuffix = facing == Rot4.East ? "_east" : "_west";
                var otherSuffix = facing == Rot4.East ? "_west" : "_east";

                var side = ContentFinder<Texture2D>.Get(path + ownSuffix, false);
                if (side != null)
                    return side;

                side = ContentFinder<Texture2D>.Get(path + otherSuffix, false);
                if (side != null)
                {
                    mirror = true;
                    return side;
                }
            }

            // Same lookup order as the pawn hair graphic: south first, then plain.
            return ContentFinder<Texture2D>.Get(path + "_south", false)
                   ?? ContentFinder<Texture2D>.Get(path, false);
        }
        
        private static Color32[] MirrorHorizontally(Color32[] pixels, int width, int height)
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