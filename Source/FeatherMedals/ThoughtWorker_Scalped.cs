using RimWorld;
using Verse;

namespace FeatherMedals;

public class ThoughtWorker_Scalped : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return p.health.hediffSet.HasHediff(FeatherMedalDefOf.TribalTrophies_Scalping_Hediff_Scalped, mustBeVisible: true);
    }
}