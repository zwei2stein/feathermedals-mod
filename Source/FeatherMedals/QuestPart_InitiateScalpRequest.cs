using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TribalTrophies;

// See vanilla QuestPart_InitiateTradeRequest.
public class QuestPart_InitiateScalpRequest : QuestPart
{
    public string inSignal;
    public Settlement settlement;
    public Faction targetFaction;
    public int requestedCount;
    public int requestDuration;
    public bool keepAfterQuestEnds;

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            foreach (var target in base.QuestLookTargets)
                yield return target;
            if (settlement != null)
                yield return settlement;
        }
    }

    public override IEnumerable<Faction> InvolvedFactions
    {
        get
        {
            foreach (var faction in base.InvolvedFactions)
                yield return faction;
            if (settlement?.Faction != null)
                yield return settlement.Faction;
            if (targetFaction != null)
                yield return targetFaction;
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag != inSignal)
            return;
        var comp = settlement?.GetComponent<ScalpRequestComp>();
        if (comp == null)
            return;
        if (comp.ActiveRequest)
        {
            Log.Error($"[TribalTrophies] Settlement {settlement.Label} already has an active scalp request.");
            return;
        }
        comp.Start(targetFaction, requestedCount, requestDuration);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (keepAfterQuestEnds)
            return;
        var comp = settlement?.GetComponent<ScalpRequestComp>();
        if (comp == null || !comp.ActiveRequest)
            return;
        comp.Disable();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref settlement, "settlement");
        Scribe_References.Look(ref targetFaction, "targetFaction");
        Scribe_Values.Look(ref requestedCount, "requestedCount");
        Scribe_Values.Look(ref requestDuration, "requestDuration");
        Scribe_Values.Look(ref keepAfterQuestEnds, "keepAfterQuestEnds");
    }
}