using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

public class LordToil_ScalpHunt : LordToil_DoOpportunisticTaskOrCover
{
 
    protected override DutyDef DutyDef => TribalTrophiesDefOf.TribalTrophies_Scalping_ScalpHuntDuty;

    public override bool ForceHighStoryDanger => true;

    public override bool AllowSelfTend => false;

    protected override bool TryFindGoodOpportunisticTaskTarget(Pawn pawn, out Thing target, List<Thing> alreadyTakenTargets)
    {
        target = null;
        if (!pawn.Spawned)
            return false;

        // already carrying a scalp off: keeps the duty
        if (pawn.mindState.duty?.def == DutyDef && pawn.carryTracker.CarriedThing is Scalp scalp)
        {
            target = scalp;
            return true;
        }

        var found = ScalpHuntUtility.TryFindVictim(pawn, out var victim, alreadyTakenTargets);
        target = victim;
        return found;
    }
}