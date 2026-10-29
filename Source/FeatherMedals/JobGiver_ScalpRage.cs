using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

public class JobGiver_ScalpRage : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        var state = pawn.MentalState as MentalState_ScalpRage;
        var victim = state?.target;
        if (victim == null)
            return null;

        Thing thing = victim.Dead ? victim.Corpse : victim;
        if (thing == null || !thing.Spawned)
            return null;

        // Another pawn stole target,pick again.
        if (!pawn.CanReserve(thing))
        {
            if (!state.TryFindNewTarget())
                return null;

            victim = state.target;
            thing = victim.Dead ? victim.Corpse : victim;
            if (thing == null || !thing.Spawned)
                return null;
        }

        if (ScalpingUtility.CanBeScalpedInBreak(thing))
        {
            var scalp = JobMaker.MakeJob(TribalTrophiesDefOf.TribalTrophies_Scalping_Job_ScalpInBreakJob, thing);
            scalp.canBashDoors = true;
            return scalp;
        }

        if (victim.Dead)
            return null;

        var attack = JobMaker.MakeJob(JobDefOf.AttackMelee, thing);
        attack.canBashDoors = true;
        attack.killIncappedTarget = false;
        return attack;
    }
}