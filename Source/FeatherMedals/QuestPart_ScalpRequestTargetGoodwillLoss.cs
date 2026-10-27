using RimWorld;
using Verse;

namespace FeatherMedals;

// See vanilla QuestPart_FactionGoodwillChange, with our own message.
public class QuestPart_ScalpRequestTargetGoodwillLoss : QuestPart
{
    public string inSignal;
    public Faction targetFaction;
    public int goodwillLoss;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag != inSignal || targetFaction == null || goodwillLoss <= 0)
            return;

        var before = targetFaction.PlayerGoodwill;

        // vanilla refuses permanent enemies and other factions without goodwill
        if (!Faction.OfPlayer.TryAffectGoodwillWith(targetFaction, -goodwillLoss,
                canSendMessage: false,
                canSendHostilityLetter: true,
                reason: TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_AcceptedScalpRequest))
            return;

        // already at the minimum, nothing changed
        var after = targetFaction.PlayerGoodwill;
        if (before == after)
            return;

        var text = "TribalTrophies_ScalpRequest_Message_TargetLearned".Translate(
                       targetFaction.NameColored.Named("FACTION")) + " " + "MessageGoodwillChanged".Translate(
                       targetFaction.Name,
                       before.ToString("F0"),
                       after.ToString("F0"));
        Messages.Message(text, MessageTypeDefOf.NegativeEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref targetFaction, "targetFaction");
        Scribe_Values.Look(ref goodwillLoss, "goodwillLoss");
    }
}