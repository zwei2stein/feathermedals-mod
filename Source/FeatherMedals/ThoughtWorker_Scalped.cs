using RimWorld;
using Verse;

namespace TribalTrophies;

public class ThoughtWorker_Scalped : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return p.health.hediffSet.HasHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped, mustBeVisible: true);
    }
}