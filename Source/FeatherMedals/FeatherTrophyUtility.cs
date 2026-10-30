using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

public static class FeatherTrophyUtility
{
    // float menu helper
    public static Apparel EvaluatingItem;

    public static bool ApplyingCeremonyAdorning;
    
    public static bool IsFeatherTrophyDef(ThingDef def) => def?.thingClass == typeof(FeatherTrophy);

    public static string WearBlockedReason(FeatherTrophy trophy, Pawn pawn)
    {
        if (trophy.IsAdorned && !trophy.IsOwnedBy(pawn))
            return "TribalTrophies_Trophy_Common_OwnedByOther".Translate(trophy.AdornedPawn.ShortName.Named("PAWN"), pawn.Named("IMPOSTOR"));

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
    public static FloatMenuOption ForceWearOption(FeatherTrophy trophy, Pawn dresser)
    {
        if (!dresser.CanReach(trophy, PathEndMode.ClosestTouch, Danger.Deadly))
            return BlockedWearOption(trophy, "NoPath".Translate().CapitalizeFirst(), true);
        if (trophy.IsBurning())
            return BlockedWearOption(trophy, "Burning".Translate(), true);

        var key = trophy.def.apparel.LastLayer.IsUtilityLayer ? "ForceTargetToEquipApparel" : "ForceTargetToWear";
        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(key.Translate(trophy.LabelShort, trophy), () =>
            {
                var queueOrder = KeyBindingDefOf.QueueOrder.IsDownEvent;
                Find.Targeter.BeginTargeting(TargetingParameters.ForForceWear(dresser),
                    target => ForceWearTargetChosen(trophy, dresser, target, queueOrder));
            }),
            dresser, trophy);
    }

    private static void ForceWearTargetChosen(FeatherTrophy trophy, Pawn dresser, LocalTargetInfo target, bool queueOrder)
    {
        if (!target.TryGetPawn(out var targetPawn) || targetPawn.apparel == null) return;

        void Reject(string reason)
            => Messages.Message(BlockedWearText(trophy, reason, true), targetPawn, MessageTypeDefOf.RejectInput, false);

        if (!trophy.IsOwnedBy(targetPawn))
        {
            Reject(WearBlockedReason(trophy, targetPawn));
            return;
        }
        if (targetPawn.apparel.WouldReplaceLockedApparel(trophy))
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
        EvaluatingItem = trophy;
        try { hasParts = ApparelUtility.HasPartsToWear(targetPawn, trophy.def); }
        finally { EvaluatingItem = null; }

        if (!hasParts)
        {
            Reject("CannotWearBecauseOfMissingBodyParts".Translate().CapitalizeFirst());
            return;
        }
        if (!EquipmentUtility.CanEquip(trophy, targetPawn, out var cantReason))
        {
            Reject(cantReason);
            return;
        }

        void Confirm()
        {
            trophy.SetForbidden(false);
            var job = JobMaker.MakeJob(JobDefOf.ForceTargetWear, targetPawn, trophy);
            dresser.jobs.TryTakeOrderedJob(job, JobTag.Misc, queueOrder);
        }

        var replaced = ApparelUtility.GetApparelReplacedByNewApparel(targetPawn, trophy);
        if (replaced != null && ModsConfig.BiotechActive
                             && MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(targetPawn, replaced, Confirm))
            return;

        Confirm();
    }

    public static FloatMenuOption CeremonyOnlyForceWearOption(Apparel apparel)
        => BlockedWearOption(apparel, "TribalTrophies_Trophy_Common_NeedsCeremony".Translate().CapitalizeFirst(), true);

    public static bool NeedsCeremony(Thing thing)
        => thing is FeatherTrophy { IsAdorned: false };
    
    public static bool CanWearTrophy(Pawn pawn, FeatherTrophy trophy)
    {
        if (pawn?.apparel == null)
            return false;

        // our wear gate refuses every pawn outside a ceremony
        var previous = ApplyingCeremonyAdorning;
        ApplyingCeremonyAdorning = true;
        try
        {
            return ApparelUtility.HasPartsToWear(pawn, trophy.def) && trophy.PawnCanWear(pawn, true);
        }
        finally
        {
            ApplyingCeremonyAdorning = previous;
        }
    }

    public static bool PawnOwnsBiocodedFeatherTrophyDef(Pawn p, ThingDef apparelDef)
    {
        var worn = p.apparel?.WornApparel;
        if (worn != null)
        {
            for (var i = 0; i < worn.Count; i++)
            {
                if (worn[i].def != apparelDef) continue;
                if (worn[i] is FeatherTrophy featherTrophy && featherTrophy.IsOwnedBy(p))
                    return true;
            }
        }
        var inv = p.inventory?.innerContainer;
        if (inv == null) return false;
        for (var i = 0; i < inv.Count; i++)
        {
            if (inv[i].def != apparelDef) continue;
            if (inv[i] is FeatherTrophy featherTrophy && featherTrophy.IsOwnedBy(p))
                return true;
        }
        return false;
    }


    public static bool TryGetDisplaySlot(Pawn pawn, FeatherTrophy trophy, out int index, out int count)
    {
        index = -1;
        count = 0;

        var worn = pawn?.apparel?.WornApparel;
        if (worn == null)
            return false;

        var maxDisplayed = TribalTrophiesMod.Settings.MaxDisplayedTrophies;
        var groups = trophy.def.apparel.bodyPartGroups;
        var seen = 0;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is not FeatherTrophy other)
                continue;
            if (seen++ >= maxDisplayed)
                break;
            if (!SameGroups(other.def.apparel.bodyPartGroups, groups))
                continue;

            if (other == trophy)
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