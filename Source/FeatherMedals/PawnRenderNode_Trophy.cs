using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class PawnRenderNode_Trophy : PawnRenderNode_Apparel
{
    public PawnRenderNode_Trophy(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
    }

    protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
    {
        if (apparel is not FeatherTrophy medal)
            yield break;

        yield return GraphicDatabase.Get<Graphic_Single>(
            medal.def.graphicData.texPath,
            ShaderDatabase.CutoutComplex,
            Vector2.one,
            medal.DrawColor,
            medal.DrawColorTwo);
    }
}