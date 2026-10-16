using Verse;

namespace FeatherMedals
{
    public class PawnRenderNode_Scalp : PawnRenderNode
    {
        private const string FreshTexPath = "Things/Pawn/Humanlike/Scalped/ScalpedFresh";

        public PawnRenderNode_Scalp(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(pawn);
        }

        // Fresh wound only while young and not rotting. Rotting corpses and old scalps use the plain texture.
        protected override string TexPathFor(Pawn pawn)
        {
            if (hediff is Hediff_Scalped scalped && scalped.IsFresh
                                                 && pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Fresh)
            {
                return FreshTexPath;
            }
            return base.TexPathFor(pawn);
        }
    }

    public class PawnRenderSubWorker_HideWhenScalped : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            HediffSet hediffs = parms.pawn?.health?.hediffSet;
            if (hediffs == null)
            {
                return true;
            }
            return !hediffs.HasHediff(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalped, mustBeVisible: true);
        }
    }
}