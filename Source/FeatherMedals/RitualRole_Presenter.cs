using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualRole_Presenter : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
            return false;
        if (PreceptDefOf.IsSpeaker(p))
        {
            reason = null;
            return true;
        }
        if (!skipReason) reason = "FeatherMedals_MustBeLeaderOrGuide".Translate();
        return false;
    }
}