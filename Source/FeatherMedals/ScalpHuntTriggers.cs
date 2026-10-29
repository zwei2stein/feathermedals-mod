using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TribalTrophies;

// Trigger_KidnapVictimPresent
public class Trigger_ScalpTargetPresent : Trigger
{
    private const int CheckInterval = 120;
    // fight calm for this long
    private const int MinTicksSinceHarm = 180;

    public override bool ActivateOn(Lord lord, TriggerSignal signal)
    {
        if (signal.type != TriggerSignalType.Tick || Find.TickManager.TicksGame % CheckInterval != 0)
            return false;
        if (Find.TickManager.TicksGame - lord.lastPawnHarmTick <= MinTicksSinceHarm)
            return false;
        // nothing to take
        if (!ScalpHuntUtility.AnyColonistDownToScalp(lord.Map))
            return false;

        foreach (var pawn in lord.ownedPawns)
        {
            if (pawn.MentalStateDef == null
                && ScalpHuntUtility.TryFindVictim(pawn, out _)
                && !GenAI.InDangerousCombat(pawn))
                return true;
        }
        return false;
    }
}


// The whole raid, every group, has carried off enough scalps.
public class Trigger_ScalpHuntGoalReached : Trigger
{
    private const int CheckInterval = 60;

    public override bool ActivateOn(Lord lord, TriggerSignal signal)
    {
        if (signal.type != TriggerSignalType.Tick || Find.TickManager.TicksGame % CheckInterval != 0)
            return false;
        return lord.LordJob is LordJob_ScalpHunt job && job.Hunt is { GoalReached: true };
    }
}