using RimWorld;
using Verse;

namespace TribalTrophies;

public class TongueRequestComp : TrophyRequestComp
{
    protected override bool IsTrophyOf(Thing thing, Faction target) => TrophyRequestUtility.IsTrophyOf<DriedTongue>(thing, target);

    protected override string FulfilledSignal => "TongueRequestFulfilled";

    protected override string InspectRequestedKey => "TribalTrophies_TongueRequest_Inspect_Requested";
    protected override string DeliverLabelKey => "TribalTrophies_TongueRequest_Gizmo_Deliver_Label";
    protected override string DeliverDescKey => "TribalTrophies_TongueRequest_Gizmo_Deliver_Desc";
    protected override string DeliverNothingKey => "TribalTrophies_TongueRequest_Gizmo_Deliver_NoTongues";
    protected override string DeliverConfirmKey => "TribalTrophies_TongueRequest_Gizmo_Deliver_Confirm";
    protected override string DeliveredMessageKey => "TribalTrophies_TongueRequest_Message_Delivered";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref targetFaction, "tongueRequestTargetFaction");
        Scribe_Values.Look(ref requestCount, "tongueRequestCount");
        Scribe_Values.Look(ref deliveredCount, "tongueRequestDeliveredCount");
        Scribe_Values.Look(ref expiration, "tongueRequestExpiration", -1);
    }
}
