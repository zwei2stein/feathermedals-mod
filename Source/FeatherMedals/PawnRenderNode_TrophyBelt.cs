using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class PawnRenderNode_TrophyBelt : PawnRenderNode_Apparel
{
    public PawnRenderNode_TrophyBelt(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
    }

    protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
    {
        if (apparel is not TrophyBelt belt)
            yield break;

        var graphic = belt.WornGraphic;
        if (graphic != null)
            yield return graphic;
    }

    // west has its own composed texture, so it is drawn on the unflipped mesh
    public override Mesh GetMesh(PawnDrawParms parms)
    {
        if (meshSet == null)
            return null;
        var mesh = meshSet.MeshAt(parms.facing == Rot4.West ? Rot4.East : parms.facing);
        if (FlipGraphic(parms))
            mesh = MeshPool.GridPlaneFlip(mesh);
        return mesh;
    }
}