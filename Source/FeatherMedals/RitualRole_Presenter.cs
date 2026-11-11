using RimWorld;
using Verse;

namespace TribalTrophies;

public class RitualRole_Presenter : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
            return false;

        if (HonoringWarriorsUtility.ForbidsCeremony(p))
        {
            if (!skipReason) reason = "TribalTrophies_AdorningCeremony_RitualDialog_RoleForbiddenByPrecept".Translate(p.Named("PAWN"));
            return false;
        }

        // presenter roles come from the kind of the trophy being adorned
        var kind = (selectedTarget.Thing as AwardedTrophy ?? ritual?.selectedTarget.Thing as AwardedTrophy)?.Kind;
        if (kind == null)
            return false;

        if (kind.IsPresenter(p))
        {
            reason = null;
            return true;
        }
        if (!skipReason) reason = "TribalTrophies_AdorningCeremony_RitualDialog_RoleMustBeSpeaker".Translate(kind.PresenterRolesLabel.Named("SPEAKERS"));
        return false;
    }
}