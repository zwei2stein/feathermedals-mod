using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TribalTrophies;

// LordJob_AssaultColony derived
public class LordJob_ScalpHunt : LordJob
{
    // same as vanilla assault
    private static readonly IntRange AssaultTimeBeforeGiveUp = new IntRange(26000, 38000);

    private Faction assaulterFaction;
    private int raidId;

    public LordJob_ScalpHunt()
    {
    }

    public LordJob_ScalpHunt(Faction assaulterFaction, int raidId)
    {
        this.assaulterFaction = assaulterFaction;
        this.raidId = raidId;
    }

    public override bool GuiltyOnDowned => true;

    public ScalpHunt Hunt => ScalpHuntTracker.For(Map)?.Get(raidId);

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();

        var assault = new LordToil_AssaultColony();
        graph.AddToil(assault);

        var hunt = new LordToil_ScalpHunt { useAvoidGrid = true };
        graph.AddToil(hunt);

        var exit = new LordToil_ExitMap(LocomotionUrgency.Jog, interruptCurrentJob: false) { useAvoidGrid = true };
        graph.AddToil(exit);

        var huntSources = new List<LordToil> { hunt };

        var startHunt = new Transition(assault, hunt);
        startHunt.AddTrigger(new Trigger_ScalpTargetPresent());
        startHunt.AddPreAction(new TransitionAction_Custom(() => DevLog("taking scalps")));
        graph.AddTransition(startHunt);

        var goalReached = new Transition(assault, exit);
        goalReached.AddSources(huntSources);
        goalReached.AddTrigger(new Trigger_ScalpHuntGoalReached());
        goalReached.AddPreAction(new TransitionAction_Custom(AnnounceGoalReached));
        graph.AddTransition(goalReached);

        // vanilla assault exits
        var giveUp = new Transition(assault, exit);
        giveUp.AddSources(huntSources);
        var giveUpTimer = new Trigger_TicksPassed(AssaultTimeBeforeGiveUp.RandomInRange);
        giveUpTimer.WithFilter(new TriggerFilter_MapExitable());
        giveUp.AddTrigger(giveUpTimer);
        giveUp.AddPreAction(new TransitionAction_Message("MessageRaidersGivenUpLeaving".Translate(
            assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name)));
        graph.AddTransition(giveUp);

        var noLongerHostile = new Transition(assault, exit);
        noLongerHostile.AddSources(huntSources);
        noLongerHostile.AddTrigger(new Trigger_BecameNonHostileToPlayer());
        noLongerHostile.AddPreAction(new TransitionAction_Message("MessageRaidersLeaving".Translate(
            assaulterFaction.def.pawnsPlural.CapitalizeFirst(), assaulterFaction.Name)));
        graph.AddTransition(noLongerHostile);

        return graph;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        base.Notify_PawnLost(p, condition);

        // ExitMap tells the lord before it destroys what the pawn carries
        if (condition != PawnLostCondition.ExitedMap || p.carryTracker?.CarriedThing is not Scalp)
            return;

        var hunt = Hunt;
        if (hunt == null)
            return;
        hunt.escaped++;
        DevLog($"{p.LabelShort} escaped with a scalp ({hunt.escaped}/{hunt.goal})");
    }

    public override void Cleanup()
    {
        base.Cleanup();

        // the last group of this raid is gone: forget its goal
        foreach (var other in Map.lordManager.lords)
        {
            if (other != lord && other.LordJob is LordJob_ScalpHunt job && job.raidId == raidId)
                return;
        }
        ScalpHuntTracker.For(Map)?.Forget(raidId);
    }

    private void DevLog(string text)
    {
        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] Scalp hunt {raidId}: {text}.");
    }
    
    private void AnnounceGoalReached()
    {
        DevLog("goal reached, leaving");
        var hunt = Hunt;
        if (hunt == null || hunt.goalReachedAnnounced)
            return;
        hunt.goalReachedAnnounced = true;
        Messages.Message("TribalTrophies_ScalpHunt_Message_GoalReached".Translate(
                assaulterFaction.def.pawnsPlural.CapitalizeFirst().Named("PAWNSPLURAL"),
                assaulterFaction.Name.Named("FACTION")),
            MessageTypeDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref assaulterFaction, "assaulterFaction");
        Scribe_Values.Look(ref raidId, "raidId");
    }
}