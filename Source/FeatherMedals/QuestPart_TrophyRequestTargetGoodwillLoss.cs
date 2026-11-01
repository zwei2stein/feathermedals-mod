using RimWorld;
using Verse;

namespace TribalTrophies;

// See vanilla QuestPart_FactionGoodwillChange, with our own message.
public abstract class QuestPart_TrophyRequestTargetGoodwillLoss : QuestPart
{
    public string inSignal;
    public Faction targetFaction;
    public int goodwillLoss;

    protected abstract HistoryEventDef Reason { get; }

    protected abstract string TargetLearnedKey { get; }

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
                reason: Reason))
            return;

        // already at the minimum, nothing changed
        var after = targetFaction.PlayerGoodwill;
        if (before == after)
            return;

        var text = TargetLearnedKey.Translate(
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
