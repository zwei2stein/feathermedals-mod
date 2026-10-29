using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class PawnRenderNode_Trophy : PawnRenderNode_Apparel
{
    public PawnRenderNode_Trophy(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
    }

    protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
    {
        if (apparel is not FeatherTrophy featherTrophy)
            yield break;

        yield return GraphicDatabase.Get<Graphic_Single>(
            featherTrophy.def.graphicData.texPath,
            ShaderDatabase.CutoutComplex,
            Vector2.one,
            featherTrophy.DrawColor,
            featherTrophy.DrawColorTwo);
    }
}