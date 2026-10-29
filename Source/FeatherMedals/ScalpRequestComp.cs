using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies;

[StaticConstructorOnStartup]
public class ScalpRequestComp : WorldObjectComp
{
    public Faction targetFaction;
    public int requestCount;
    public int deliveredCount;
    public int expiration = -1;

    private static readonly Texture2D DeliverCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");

    public bool ActiveRequest => expiration > Find.TickManager.TicksGame;

    public int Remaining => requestCount - deliveredCount;

    public bool Wants(Thing thing) => ActiveRequest && ScalpRequestUtility.IsScalpOf(thing, targetFaction);

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
        return "TribalTrophies_ScalpRequest_Inspect_Requested".Translate(
            TargetName.Named("TARGET"),
            deliveredCount.Named("DELIVERED"),
            requestCount.Named("TOTAL"),
            (expiration - Find.TickManager.TicksGame).ToStringTicksToDays().Named("TIME"));
    }

    private string TargetName => targetFaction != null
        ? targetFaction.NameColored.Resolve()
        : "TribalTrophies_Scalping_Misc_UnknownName".Translate().Resolve();

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
            defaultLabel = "TribalTrophies_ScalpRequest_Gizmo_Deliver_Label".Translate(),
            defaultDesc = "TribalTrophies_ScalpRequest_Gizmo_Deliver_Desc".Translate(TargetName.Named("TARGET")),
            icon = DeliverCommandTex,
            action = () =>
            {
                if (!ActiveRequest)
                {
                    Log.Error("[TribalTrophies] Attempted to deliver scalps to an inactive request");
                    return;
                }
                var count = CountInCaravan(caravan);
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "TribalTrophies_ScalpRequest_Gizmo_Deliver_Confirm".Translate(
                        count.Named("COUNT"),
                        TargetName.Named("TARGET"),
                        parent.Faction.NameColored.Named("FACTION")),
                    () => DeliverFrom(caravan)));
            }
        };
        if (CountInCaravan(caravan) == 0)
            command.Disable("TribalTrophies_ScalpRequest_Gizmo_Deliver_NoScalps".Translate(TargetName.Named("TARGET")));
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

    private void Accept(List<Thing> scalps, Caravan caravan)
    {
        if (scalps.Count == 0)
            return;

        foreach (var scalp in scalps)
            scalp.Destroy();
        deliveredCount += scalps.Count;

        if (deliveredCount < requestCount)
        {
            Messages.Message("TribalTrophies_ScalpRequest_Message_Delivered".Translate(
                    parent.Faction.NameColored.Named("FACTION"),
                    scalps.Count.Named("COUNT"),
                    TargetName.Named("TARGET"),
                    deliveredCount.Named("DELIVERED"),
                    requestCount.Named("TOTAL")),
                parent, MessageTypeDefOf.PositiveEvent);
            return;
        }

        if (caravan != null)
            QuestUtility.SendQuestTargetSignals(parent.questTags, "ScalpRequestFulfilled", parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
        else
            QuestUtility.SendQuestTargetSignals(parent.questTags, "ScalpRequestFulfilled", parent.Named("SUBJECT"));
        Disable();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref targetFaction, "targetFaction");
        Scribe_Values.Look(ref requestCount, "requestCount");
        Scribe_Values.Look(ref deliveredCount, "deliveredCount");
        Scribe_Values.Look(ref expiration, "expiration", -1);
    }
}