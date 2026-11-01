using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Settlement side of a trophy request quest (scalps, dried tongues): what was asked for, delivery by caravan or pods.
// See vanilla TradeRequestComp.
[StaticConstructorOnStartup]
public abstract class TrophyRequestComp : WorldObjectComp
{
    public Faction targetFaction;
    public int requestCount;
    public int deliveredCount;
    public int expiration = -1;

    private static readonly Texture2D DeliverCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");

    // Loose trophy of this kind, taken from a member of the target faction.
    protected abstract bool IsTrophyOf(Thing thing, Faction target);

    // Own signal per kind: quests of one settlement hear each other's signals.
    protected abstract string FulfilledSignal { get; }

    protected abstract string InspectRequestedKey { get; }
    protected abstract string DeliverLabelKey { get; }
    protected abstract string DeliverDescKey { get; }
    protected abstract string DeliverNothingKey { get; }
    protected abstract string DeliverConfirmKey { get; }
    protected abstract string DeliveredMessageKey { get; }

    // Comps of one settlement save into the same node: each kind saves its fields under its own keys in PostExposeData.

    public bool ActiveRequest => expiration > Find.TickManager.TicksGame;

    public int Remaining => requestCount - deliveredCount;

    public bool Wants(Thing thing) => ActiveRequest && IsTrophyOf(thing, targetFaction);

    public void Start(Faction target, int count, int duration)
    {
        targetFaction = target;
        requestCount = count;
        deliveredCount = 0;
        expiration = Find.TickManager.TicksGame + duration;
    }

    public void Disable() => expiration = -1;

    public override string CompInspectStringExtra()
    {
        if (!ActiveRequest)
            return null;
        return InspectRequestedKey.Translate(
            TargetName.Named("TARGET"),
            deliveredCount.Named("DELIVERED"),
            requestCount.Named("TOTAL"),
            (expiration - Find.TickManager.TicksGame).ToStringTicksToDays().Named("TIME"));
    }

    private string TargetName => targetFaction != null
        ? targetFaction.NameColored.Resolve()
        : "TribalTrophies_TrophyBodyPart_Misc_UnknownName".Translate().Resolve();

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        if (ActiveRequest && CaravanVisitUtility.SettlementVisitedNow(caravan) == parent)
            yield return DeliverCommand(caravan);
    }

    private int CountInCaravan(Caravan caravan)
    {
        var count = 0;
        foreach (var thing in CaravanInventoryUtility.AllInventoryItems(caravan))
        {
            if (Wants(thing))
                count++;
        }
        return Mathf.Min(count, Remaining);
    }

    private Command DeliverCommand(Caravan caravan)
    {
        var command = new Command_Action
        {
            defaultLabel = DeliverLabelKey.Translate(),
            defaultDesc = DeliverDescKey.Translate(TargetName.Named("TARGET")),
            icon = DeliverCommandTex,
            action = () =>
            {
                if (!ActiveRequest)
                {
                    Log.Error("[TribalTrophies] Attempted to deliver trophies to an inactive request");
                    return;
                }
                var count = CountInCaravan(caravan);
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    DeliverConfirmKey.Translate(
                        count.Named("COUNT"),
                        TargetName.Named("TARGET"),
                        parent.Faction.NameColored.Named("FACTION")),
                    () => DeliverFrom(caravan)));
            }
        };
        if (CountInCaravan(caravan) == 0)
            command.Disable(DeliverNothingKey.Translate(TargetName.Named("TARGET")));
        return command;
    }

    private void DeliverFrom(Caravan caravan)
    {
        var left = Remaining;
        var taken = CaravanInventoryUtility.TakeThings(caravan, thing =>
        {
            if (left <= 0 || !Wants(thing))
                return 0;
            left--;
            return 1;
        });
        Accept(taken, caravan);
    }

    public void TakeFromPods(List<ActiveTransporterInfo> pods)
    {
        if (!ActiveRequest)
            return;
        var taken = new List<Thing>();
        foreach (var pod in pods)
        {
            var contents = pod.innerContainer;
            for (var i = contents.Count - 1; i >= 0; i--)
            {
                if (taken.Count >= Remaining)
                    break;
                var thing = contents[i];
                if (!Wants(thing))
                    continue;
                contents.Remove(thing);
                taken.Add(thing);
            }
        }
        Accept(taken, null);
    }

    private void Accept(List<Thing> trophies, Caravan caravan)
    {
        if (trophies.Count == 0)
            return;

        foreach (var trophy in trophies)
            trophy.Destroy();
        deliveredCount += trophies.Count;

        if (deliveredCount < requestCount)
        {
            Messages.Message(DeliveredMessageKey.Translate(
                    parent.Faction.NameColored.Named("FACTION"),
                    trophies.Count.Named("COUNT"),
                    TargetName.Named("TARGET"),
                    deliveredCount.Named("DELIVERED"),
                    requestCount.Named("TOTAL")),
                parent, MessageTypeDefOf.PositiveEvent);
            return;
        }

        if (caravan != null)
            QuestUtility.SendQuestTargetSignals(parent.questTags, FulfilledSignal, parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
        else
            QuestUtility.SendQuestTargetSignals(parent.questTags, FulfilledSignal, parent.Named("SUBJECT"));
        Disable();
    }
}
