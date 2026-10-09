using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualRole_Awardee : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn pawn, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(pawn, out reason, selectedTarget, ritual, assignments, precept, skipReason))
            return false;

        if (HonoringWarriorsUtil.ForbidsCeremony(pawn))
        {
            if (!skipReason) reason = "FeatherMedals_HW_ForbiddenRole".Translate(pawn.Named("PAWN"));
            return false;
        }

        if (selectedTarget.Thing is FeatherMedal trophy && !FeatherMedalUtil.CanWearTrophy(pawn, trophy))
        {
            if (!skipReason) reason = "FeatherTrophy_CannotBeAdornedWith".Translate(pawn.Named("PAWN"));
            return false;
        }

        return true;
    }
}