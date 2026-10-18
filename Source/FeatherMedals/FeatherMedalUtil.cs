using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals;

public static class FeatherMedalUtil
{
    // float menu helper
    public static Apparel EvaluatingItem;
    
    public static bool IsMedalDef(ThingDef def) => def?.thingClass == typeof(FeatherMedal);

    public static bool IsOwnedBy(Thing thing, Pawn pawn)
        => thing is FeatherMedal { BiocodeComp: { Biocoded: true } comp } && comp.CodedPawn == pawn;

    public static string WearBlockedReason(FeatherMedal medal, Pawn pawn)
    {
        if (medal.BiocodeComp is { Biocoded: true, CodedPawn: { } owner } && owner != pawn)
            return "TribalTrophies_Trophy_Common_OwnedByOther".Translate(owner.Named("PAWN"), pawn.Named("IMPOSTOR"));

        return "TribalTrophies_Trophy_Common_NeedsCeremony".Translate().CapitalizeFirst();
    }

    public static string BlockedWearText(Apparel apparel, string reason, bool forcingOtherPawn)
    {
        var utility = apparel.def.apparel.LastLayer.IsUtilityLayer;
        var key = forcingOtherPawn
            ? (utility ? "CannotForceTargetToEquipApparel" : "CannotForceTargetToWear")
            : (utility ? "CannotEquipApparel" : "CannotWear");
        return key.Translate(apparel.Label, apparel) + ": " + reason;
    }

    public static FloatMenuOption BlockedWearOption(Apparel apparel, string reason, bool forcingOtherPawn)
        => new(BlockedWearText(apparel, reason, forcingOtherPawn), null);

    // "force <pawn> to wear" for a trophy that already belongs to somebody
    public static FloatMenuOption ForceWearOption(FeatherMedal medal, Pawn dresser)
    {
        if (!dresser.CanReach(medal, PathEndMode.ClosestTouch, Danger.Deadly))
            return BlockedWearOption(medal, "NoPath".Translate().CapitalizeFirst(), true);
        if (medal.IsBurning())
            return BlockedWearOption(medal, "Burning".Translate(), true);

        var key = medal.def.apparel.LastLayer.IsUtilityLayer ? "ForceTargetToEquipApparel" : "ForceTargetToWear";
        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(key.Translate(medal.LabelShort, medal), () =>
            {
                var queueOrder = KeyBindingDefOf.QueueOrder.IsDownEvent;
                Find.Targeter.BeginTargeting(TargetingParameters.ForForceWear(dresser),
                    target => ForceWearTargetChosen(medal, dresser, target, queueOrder));
            }),
            dresser, medal);
    }

    private static void ForceWearTargetChosen(FeatherMedal medal, Pawn dresser, LocalTargetInfo target, bool queueOrder)
    {
        if (!target.TryGetPawn(out var targetPawn) || targetPawn.apparel == null) return;

        void Reject(string reason)
            => Messages.Message(BlockedWearText(medal, reason, true), targetPawn, MessageTypeDefOf.RejectInput, false);

        if (!IsOwnedBy(medal, targetPawn))
        {
            Reject(WearBlockedReason(medal, targetPawn));
            return;
        }
        if (targetPawn.apparel.WouldReplaceLockedApparel(medal))
        {
            Reject("WouldReplaceLockedApparel".Translate().CapitalizeFirst());
            return;
        }
        if (targetPawn.IsMutant && targetPawn.mutant.Def.disableApparel)
        {
            Reject(targetPawn.mutant.Def.LabelCap);
            return;
        }

        bool hasParts;
        EvaluatingItem = medal;
        try { hasParts = ApparelUtility.HasPartsToWear(targetPawn, medal.def); }
        finally { EvaluatingItem = null; }

        if (!hasParts)
        {
            Reject("CannotWearBecauseOfMissingBodyParts".Translate().CapitalizeFirst());
            return;
        }
        if (!EquipmentUtility.CanEquip(medal, targetPawn, out var cantReason))
        {
            Reject(cantReason);
            return;
        }

        void Confirm()
        {
            medal.SetForbidden(false);
            var job = JobMaker.MakeJob(JobDefOf.ForceTargetWear, targetPawn, medal);
            dresser.jobs.TryTakeOrderedJob(job, JobTag.Misc, queueOrder);
        }

        var replaced = ApparelUtility.GetApparelReplacedByNewApparel(targetPawn, medal);
        if (replaced != null && ModsConfig.BiotechActive
                             && MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(targetPawn, replaced, Confirm))
            return;

        Confirm();
    }

    public static FloatMenuOption CeremonyOnlyForceWearOption(Apparel apparel)
        => BlockedWearOption(apparel, "TribalTrophies_Trophy_Common_NeedsCeremony".Translate().CapitalizeFirst(), true);

    public static bool NeedsCeremony(Thing thing)
        => thing is FeatherMedal { BiocodeComp: not { Biocoded: true } };
    
    public static bool CanWearTrophy(Pawn pawn, FeatherMedal medal)
    {
        if (pawn?.apparel == null)
            return false;

        // our wear gate refuses every pawn outside a ceremony
        var previous = RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward;
        RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = true;
        try
        {
            return ApparelUtility.HasPartsToWear(pawn, medal.def) && medal.PawnCanWear(pawn, true);
        }
        finally
        {
            RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = previous;
        }
    }

    public static bool TryGetDisplaySlot(Pawn pawn, FeatherMedal medal, out int index, out int count)
    {
        index = -1;
        count = 0;

        var worn = pawn?.apparel?.WornApparel;
        if (worn == null)
            return false;

        var maxDisplayed = MedalMod.Settings.MaxDisplayedTrophies;
        var groups = medal.def.apparel.bodyPartGroups;
        var seen = 0;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is not FeatherMedal other)
                continue;
            if (seen++ >= maxDisplayed)
                break;
            if (!SameGroups(other.def.apparel.bodyPartGroups, groups))
                continue;

            if (other == medal)
                index = count;
            count++;
        }

        return index >= 0;
    }
    
    private static bool SameGroups(List<BodyPartGroupDef> a, List<BodyPartGroupDef> b)
    {
        if (a == b)
            return true;
        if (a == null || b == null || a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!b.Contains(a[i]))
                return false;
        }
        return true;
    }
}