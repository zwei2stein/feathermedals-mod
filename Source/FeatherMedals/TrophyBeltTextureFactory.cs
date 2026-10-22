using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class TrophyBeltTextureFactory
{
    // One texture per facing, indexed by Rot4.AsInt. Null if a belt texture is missing.
    public static Texture2D[] Compose(string texPath, Color beltColor, IReadOnlyList<Thing> trophies, List<TrophySlot> slots)
    {
        var composed = new Texture2D[4];
        foreach (var facing in Rot4.AllRotations)
        {
            var texture = ComposeFacing(texPath, facing, beltColor, trophies, slots);
            if (texture == null)
            {
                foreach (var done in composed)
                {
                    if (done != null)
                        Object.Destroy(done);
                }
                return null;
            }
            composed[facing.AsInt] = texture;
        }
        return composed;
    }

    private static Texture2D ComposeFacing(string texPath, Rot4 facing, Color beltColor, IReadOnlyList<Thing> trophies, List<TrophySlot> slots)
    {
        var beltTexture = BeltTexture(texPath, facing, out bool mirror);
        if (beltTexture == null)
            return null;

        int width = beltTexture.width;
        int height = beltTexture.height;

        Color32[] result = TextureCompositing.ReadPixels(beltTexture, width, height);
        if (mirror)
            result = TextureCompositing.MirrorHorizontally(result, width, height);
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = TextureCompositing.Tint(result[i], beltColor);
        }

        // trophy N hangs in slot N; later slots are drawn over earlier ones
        int count = Mathf.Min(trophies.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            var placement = slots[i].For(facing);
            if (placement == null)
                continue;
            PasteTrophy(result, width, height, trophies[i], placement.Value);
        }

        return TextureCompositing.MakeTexture(result, width, height, "TrophyBeltComposed");
    }

    private static Texture2D BeltTexture(string texPath, Rot4 facing, out bool mirror)
    {
        mirror = false;
        var path = texPath + Suffix(facing);
        if (facing == Rot4.West && ContentFinder<Texture2D>.Get(path, false) == null)
        {
            // like vanilla multi graphics: a missing west side is the east side mirrored
            mirror = true;
            path = texPath + Suffix(Rot4.East);
        }

        var texture = ContentFinder<Texture2D>.Get(path, false);
        if (texture == null)
            Log.ErrorOnce("TribalTrophies: trophy belt texture not found at " + path, path.GetHashCode());
        return texture;
    }

    private static string Suffix(Rot4 facing)
    {
        if (facing == Rot4.North)
            return "_north";
        if (facing == Rot4.East)
            return "_east";
        if (facing == Rot4.West)
            return "_west";
        return "_south";
    }

    // placement: x, y = trophy center, z = trophy width; fractions of the belt texture, y counted from the top
    private static void PasteTrophy(Color32[] result, int width, int height, Thing trophy, Vector3 placement)
    {
        // whatever the trophy shows as a single item: one skull, a scalp, ...
        var material = trophy.Graphic?.MatSingleFor(trophy);
        var texture = material != null ? material.mainTexture : null;
        if (texture == null)
            return;

        int pasteWidth = Mathf.RoundToInt(placement.z * width);
        int pasteHeight = Mathf.RoundToInt(pasteWidth * (float)texture.height / texture.width);
        if (pasteWidth <= 0 || pasteHeight <= 0)
            return;

        Color32[] pixels = TextureCompositing.ReadPixels(texture, pasteWidth, pasteHeight);

        // pixel rows start at the bottom, placement y at the top
        int left = Mathf.RoundToInt(placement.x * width - pasteWidth / 2f);
        int bottom = Mathf.RoundToInt((1f - placement.y) * height - pasteHeight / 2f);

        TextureCompositing.Paste(result, width, height, pixels, pasteWidth, pasteHeight, left, bottom, material.color);
    }
}