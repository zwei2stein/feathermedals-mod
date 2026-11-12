using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class PawnRenderSubWorker_Trophy : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node.apparel is not AwardedTrophy featherTrophy)
            return false;

        // each kind has its own draw setting and layout
        var layout = featherTrophy.Kind.LayoutWorker;
        return layout.DrawOnPawns
               && layout.DrawsFacing(parms.facing)
               && AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count)
               && layout.Fits(node, parms, index, count);
    }

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.apparel is not AwardedTrophy featherTrophy
            || !AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        offset += featherTrophy.Kind.LayoutWorker.OffsetFor(node, parms, index, count);
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        if (node.apparel is not AwardedTrophy featherTrophy)
            return;

        var layout = featherTrophy.Kind.LayoutWorker;
        // the drawn node list is cached (see TrophyRenderUtility); one that no longer fits is hidden until it is rebuilt
        if (AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count)
            && !layout.Fits(node, parms, index, count))
        {
            scale = Vector3.zero;
            return;
        }

        scale *= layout.Scale;
    }
    
    public override void TransformRotation(PawnRenderNode node, PawnDrawParms parms, ref Quaternion rotation)
    {
        if (node.apparel is not AwardedTrophy featherTrophy
            || !AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        rotation = Quaternion.AngleAxis(featherTrophy.Kind.LayoutWorker.TiltFor(node, parms, index, count), Vector3.up) * rotation;
    }

    public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
    {
        if (node.apparel is not AwardedTrophy featherTrophy
            || !AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        layer += featherTrophy.Kind.LayoutWorker.LayerOffsetFor(node, parms, index, count);
    }
}

public static class TrophyRenderUtility
{
    // Display settings can change which trophies are drawn, we must rebuild cache
    public static void RecacheWornTrophies()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        foreach (var map in Find.Maps)
        {
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!WearsAwardedTrophy(pawn))
                    continue;

                var root = pawn.Drawer?.renderer?.renderTree?.rootNode;
                if (root != null)
                    root.requestRecache = true;
                PortraitsCache.SetDirty(pawn);
            }
        }
    }

    private static bool WearsAwardedTrophy(Pawn pawn)
    {
        var worn = pawn.apparel?.WornApparel;
        if (worn == null)
            return false;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is AwardedTrophy)
                return true;
        }
        return false;
    }
}
