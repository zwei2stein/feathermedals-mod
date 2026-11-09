using UnityEngine;
using Verse;

namespace TribalTrophies;

public class PawnRenderSubWorker_Trophy : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        return TribalTrophiesMod.Settings.DrawFeatherTrophiesOnPawns
               && node.apparel is AwardedTrophy featherTrophy
               && AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out _, out _);
    }

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.apparel is not AwardedTrophy featherTrophy
            || !AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        offset += featherTrophy.Kind.LayoutWorker.OffsetFor(index, count, parms.facing);
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        if (node.apparel is AwardedTrophy featherTrophy)
            scale *= featherTrophy.Kind.LayoutWorker.ScaleFeather;
    }
    
    public override void TransformRotation(PawnRenderNode node, PawnDrawParms parms, ref Quaternion rotation)
    {
        if (node.apparel is not AwardedTrophy featherTrophy
            || !AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        rotation = Quaternion.AngleAxis(featherTrophy.Kind.LayoutWorker.TiltFor(index, count, parms.facing), Vector3.up) * rotation;
    }
}