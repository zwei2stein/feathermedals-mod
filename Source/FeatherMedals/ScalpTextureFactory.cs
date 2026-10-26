using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    public static class ScalpTextureFactory
    {
        private const string BasePath = "Things/Pawn/Humanlike/Scalp/ScalpBase";

        public static Texture2D Compose(Color skinColor, HairDef hairDef, Color hairColor, Rot4 hairFacing)
        {
            var baseTexture = ContentFinder<Texture2D>.Get(BasePath, false);
            if (baseTexture == null)
            {
                Log.ErrorOnce("[TribalTrophies]: scalp base texture not found at " + BasePath, 7354811);
                return null;
            }

            int width = baseTexture.width;
            int height = baseTexture.height;

            Color32[] result = TextureCompositing.ReadPixels(baseTexture, width, height);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = TextureCompositing.Tint(result[i], skinColor);
            }

            var hairTexture = HairTexture(hairDef, hairFacing, out bool mirror);
            if (hairTexture != null)
            {
                Color32[] hair = TextureCompositing.ReadPixels(hairTexture, width, height);
                if (mirror)
                    hair = TextureCompositing.MirrorHorizontally(hair, width, height);
                TextureCompositing.Paste(result, width, height, hair, width, height, 0, 0, hairColor);
            }

            return TextureCompositing.MakeTexture(result, width, height, "ScalpComposed");
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
    }
}