using UnityEngine;
using Verse;

namespace FeatherMedals;

public class PawnRenderSubWorker_Trophy : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        return MedalMod.Settings.DrawTrophiesOnPawns
               && node.apparel is FeatherTrophy medal
               && FeatherMedalUtil.TryGetDisplaySlot(parms.pawn, medal, out _, out _);
    }

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        if (node.apparel is not FeatherTrophy medal
            || !FeatherMedalUtil.TryGetDisplaySlot(parms.pawn, medal, out var index, out var count))
            return;

        offset += TrophyLayout.OffsetFor(index, count, parms.facing);
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        scale *= MedalMod.Settings.TrophyScale;
    }
    
    public override void TransformRotation(PawnRenderNode node, PawnDrawParms parms, ref Quaternion rotation)
    {
        if (node.apparel is not FeatherTrophy medal
            || !FeatherMedalUtil.TryGetDisplaySlot(parms.pawn, medal, out var index, out var count))
            return;

        rotation = Quaternion.AngleAxis(TrophyLayout.TiltFor(index, count, parms.facing), Vector3.up) * rotation;
    }
}