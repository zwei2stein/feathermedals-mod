using RimWorld;
using Verse;

namespace TribalTrophies;

public class ScalpRequestComp : TrophyRequestComp
{
    protected override bool IsTrophyOf(Thing thing, Faction target) => TrophyRequestUtility.IsTrophyOf<Scalp>(thing, target);

    protected override string FulfilledSignal => "ScalpRequestFulfilled";

    protected override string InspectRequestedKey => "TribalTrophies_ScalpRequest_Inspect_Requested";
    protected override string DeliverLabelKey => "TribalTrophies_ScalpRequest_Gizmo_Deliver_Label";
    protected override string DeliverDescKey => "TribalTrophies_ScalpRequest_Gizmo_Deliver_Desc";
    protected override string DeliverNothingKey => "TribalTrophies_ScalpRequest_Gizmo_Deliver_NoScalps";
    protected override string DeliverConfirmKey => "TribalTrophies_ScalpRequest_Gizmo_Deliver_Confirm";
    protected override string DeliveredMessageKey => "TribalTrophies_ScalpRequest_Message_Delivered";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref targetFaction, "targetFaction");
        // Own names: vanilla TradeRequestComp saves "requestCount" and "expiration" into the same settlement node.
        Scribe_Values.Look(ref requestCount, "scalpRequestCount");
        Scribe_Values.Look(ref deliveredCount, "deliveredCount");
        Scribe_Values.Look(ref expiration, "scalpRequestExpiration", -1);
    }
}
