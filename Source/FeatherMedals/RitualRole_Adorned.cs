using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualRole_Adorned : RitualRoleColonist
{
    public const string ROLE_ID = "adorned";
    
    public override bool AppliesToPawn(Pawn pawn, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(pawn, out reason, selectedTarget, ritual, assignments, precept, skipReason))
            return false;

        if (HonoringWarriorsUtility.ForbidsCeremony(pawn))
        {
            if (!skipReason) reason = "TribalTrophies_AdorningCeremony_RitualDialog_RoleForbiddenByPrecept".Translate(pawn.Named("PAWN"));
            return false;
        }

        if (selectedTarget.Thing is FeatherTrophy trophy && !FeatherTrophyUtility.CanWearTrophy(pawn, trophy))
        {
            if (!skipReason) reason = "TribalTrophies_AdorningCeremony_RitualDialog_RoleCannotBeAdorned".Translate(pawn.Named("PAWN"));
            return false;
        }

        return true;
    }
}