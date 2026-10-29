using UnityEngine;
using Verse;

namespace TribalTrophies;

public class PawnRenderSubWorker_Trophy : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        return TribalTrophiesMod.Settings.DrawTrophiesOnPawns
               && node.apparel is FeatherTrophy featherTrophy
               && FeatherTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out _, out _);
    }

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.apparel is not FeatherTrophy featherTrophy
            || !FeatherTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        offset += TrophyLayout.OffsetFor(index, count, parms.facing);
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        scale *= TribalTrophiesMod.Settings.TrophyScale;
    }
    
    public override void TransformRotation(PawnRenderNode node, PawnDrawParms parms, ref Quaternion rotation)
    {
        if (node.apparel is not FeatherTrophy featherTrophy
            || !FeatherTrophyUtility.TryGetDisplaySlot(parms.pawn, featherTrophy, out var index, out var count))
            return;

        rotation = Quaternion.AngleAxis(TrophyLayout.TiltFor(index, count, parms.facing), Vector3.up) * rotation;
    }
}