using System.Text;
using HarmonyLib;
using UnityEngine;

namespace TribalTrophies;
using RimWorld;
using Verse;

public class RitualOutcomeComp_TrophyPreview : RitualOutcomeComp
{
    public override bool Applies(LordJob_Ritual ritual) =>
        ritual?.selectedTarget.Thing is FeatherTrophy;

    public override bool DataRequired => false;

    public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data) => 0f;

    public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
    {
        if (ritual?.selectedTarget.Thing is not FeatherTrophy featherTrophy)
            return "TribalTrophies_AdorningCeremony_RitualDialog_TrophyUnavailable".Translate();
        return featherTrophy.TrophyLabel;
    }

    public override QualityFactor GetQualityFactor(
        Precept_Ritual ritual,
        TargetInfo ritualTarget,
        RitualObligation obligation,
        RitualRoleAssignments assignments,
        RitualOutcomeComp_Data data)
    {
        if (ritualTarget.Thing is not FeatherTrophy featherTrophy) return null;

        var sb = new StringBuilder();

        var statOffsets = featherTrophy.def.equippedStatOffsets;
        if (statOffsets is { Count: > 0 })
        {
            foreach (var stat in statOffsets)
                sb.AppendLine(UIHelper.StatOffsetLine(stat));
        }

        if (TribalTrophiesMod.Settings.TrophyDynamicTraits)
        {
            var ext = featherTrophy.def.GetModExtension<TrophyExtension>();
            if (ext != null)
            {
                var traitMultiplier = RitualOutcomeTraitUtility.GetTrophyTraitChanceMultiplier(featherTrophy);
                if (ext.removesTraits is { Count: > 0 })
                    foreach (var entry in ext.removesTraits)
                        sb.AppendLine("TribalTrophies_AdorningCeremony_Common_MayRemoveTrait"
                            .Translate(entry.Label.CapitalizeFirst(), (entry.chance * traitMultiplier).ToStringPercent()));
                if (ext.addsTraits is { Count: > 0 })
                    foreach (var entry in ext.addsTraits)
                        sb.AppendLine("TribalTrophies_AdorningCeremony_Common_MayGrantTrait"
                            .Translate(entry.Label.CapitalizeFirst(), (entry.chance * traitMultiplier).ToStringPercent()));
            }
        }

        return new QualityFactor
        {
            label = featherTrophy.TrophyLabel,
            count = "",
            qualityChange = "",
            quality = 0f,
            positive = true,
            present = true,
            noMiddleColumnInfo = false,
            toolTip = sb.ToString().TrimEnd(),
            priority = 100f
        };
    }
}

public class RitualOutcomeComp_TrophyQuality : RitualOutcomeComp
{
    public override bool Applies(LordJob_Ritual ritual) =>
        ritual?.selectedTarget.Thing is FeatherTrophy featherTrophy && featherTrophy.TryGetQuality(out _);

    public override bool DataRequired => false;

    public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data) =>
        ritual?.selectedTarget.Thing is FeatherTrophy featherTrophy ? AdorningQuality.GetTrophyQualityBonus(featherTrophy) : 0f;

    public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null) =>
        "TribalTrophies_AdorningCeremony_RitualDialog_TrophyQuality".Translate();

    public override QualityFactor GetQualityFactor(
        Precept_Ritual ritual,
        TargetInfo ritualTarget,
        RitualObligation obligation,
        RitualRoleAssignments assignments,
        RitualOutcomeComp_Data data)
    {
        if (ritualTarget.Thing is not FeatherTrophy featherTrophy || !featherTrophy.TryGetQuality(out var quality))
            return null;

        var bonus = AdorningQuality.GetTrophyQualityBonus(featherTrophy);

        return new QualityFactor
        {
            label = "TribalTrophies_AdorningCeremony_RitualDialog_TrophyQuality".Translate(),
            count = quality.GetLabel().CapitalizeFirst(),
            qualityChange = bonus >= 0f ? $"+{bonus.ToStringPercent()}" : bonus.ToStringPercent(),
            quality = bonus,
            positive = bonus >= 0f,
            present = true,
            toolTip = "TribalTrophies_AdorningCeremony_RitualDialog_TrophyQuality_Tooltip".Translate(
                AdorningQuality.MaxTrophyBonus.ToStringPercent().Named("MAX"),
                AdorningQuality.MaxTrophyPenalty.ToStringPercent().Named("MIN")),
            priority = 90f
        };
    }
}

public class RitualOutcomeComp_CeremonyAttendance : RitualOutcomeComp
{
    public override bool Applies(LordJob_Ritual ritual) => true;

    public override bool DataRequired => false;

    public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
    {
        if (ritual?.Map == null) return 0f;
        var adorned = ritual.assignments.FirstAssignedPawn(RitualRole_Adorned.ROLE_ID);
        var attendees = AdorningQuality.CountAttendees(ritual.assignments.Participants, adorned);
        var total = AdorningQuality.CountPossibleAttendees(ritual.Map, adorned);
        return AdorningQuality.GetAttendanceScore(attendees, total);
    }

    public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
    {
        return "TribalTrophies_AdorningCeremony_RitualDialog_Attendance".Translate();
    }

    public override QualityFactor GetQualityFactor(
        Precept_Ritual ritual,
        TargetInfo ritualTarget,
        RitualObligation obligation,
        RitualRoleAssignments assignments,
        RitualOutcomeComp_Data data)
    {
        var map = ritualTarget.Map ?? Find.CurrentMap;
        if (map == null) return null;

        var adorned = assignments.FirstAssignedPawn(RitualRole_Adorned.ROLE_ID);
        var attendees = AdorningQuality.CountAttendees(assignments.Participants, adorned);
        var total = AdorningQuality.CountPossibleAttendees(map, adorned);
        var ratio = AdorningQuality.GetAttendanceRatio(attendees, total);
        var contribution = AdorningQuality.GetAttendanceScore(attendees, total);

        return new QualityFactor
        {
            label = "TribalTrophies_AdorningCeremony_RitualDialog_Attendance".Translate(),
            count = $"{attendees} / {total}",
            qualityChange = $"+{contribution.ToStringPercent()}",
            quality = contribution,
            positive = ratio >= 0.25f,
            present = true,
            toolTip = "TribalTrophies_AdorningCeremony_RitualDialog_Attendance_Tooltip".Translate(AdorningQuality.AttendanceWeight.ToStringPercent().Named("QUALITY")),
            priority = 50f
        };
    }
}

public class RitualOutcomeComp_CeremonyRoom : RitualOutcomeComp
{
    public override bool Applies(LordJob_Ritual ritual) => true;

    public override bool DataRequired => false;

    public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
    {
        var impressiveness = AdorningQuality.GetRoomImpressiveness(ritual.selectedTarget);
        return AdorningQuality.GetRoomScore(impressiveness);
    }

    public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
    {
        return "TribalTrophies_AdorningCeremony_RitualDialog_Venue".Translate();
    }

    public override QualityFactor GetQualityFactor(
        Precept_Ritual ritual,
        TargetInfo ritualTarget,
        RitualObligation obligation,
        RitualRoleAssignments assignments,
        RitualOutcomeComp_Data data)
    {
        var impressiveness = AdorningQuality.GetRoomImpressiveness(ritualTarget);
        var contribution = AdorningQuality.GetRoomScore(impressiveness);

        var roomLabel = impressiveness <= 0f ? "TribalTrophies_AdorningCeremony_RitualDialog_Venue_Outdoors".Translate().Resolve() : $"{impressiveness:F0}";

        return new QualityFactor
        {
            label ="TribalTrophies_AdorningCeremony_RitualDialog_Venue".Translate(),
            count = roomLabel,
            qualityChange = $"+{contribution.ToStringPercent()}",
            quality = contribution,
            positive = impressiveness >= 25f,
            present = true,
            toolTip = "TribalTrophies_AdorningCeremony_RitualDialog_Venue_Tooltip".Translate(AdorningQuality.RoomWeight.ToStringPercent().Named("QUALITY")),
            priority = 40f
        };
    }
}

[HarmonyPatch(typeof(Dialog_BeginLordJob), nameof(Dialog_BeginLordJob.DoLeftColumn))]
public static class PatchDrawFeatherTrophyInRitual
{
    private static readonly AccessTools.FieldRef<Dialog_BeginRitual, TargetInfo> TargetRef =
        AccessTools.FieldRefAccess<Dialog_BeginRitual, TargetInfo>("target");

    public static void Prefix(Dialog_BeginLordJob __instance, ref RectDivider layout)
    {
        if (__instance is not Dialog_BeginRitual ritual) return;
        
        var target = TargetRef(ritual);
        if (target.Thing is not FeatherTrophy featherTrophy) return;
        
        var row = layout.NewRow(60f, marginOverride: 6f);
        var iconRect = new Rect(row.Rect.x, row.Rect.y, 50f, 50f);
        Widgets.ThingIcon(iconRect, featherTrophy);
        
        var labelRect = new Rect(iconRect.xMax + 10f, row.Rect.y, row.Rect.width - 60f, 50f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        Widgets.Label(labelRect, featherTrophy.TrophyLabel);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}

public class RitualOutcomeComp_CeremonyTale : RitualOutcomeComp
{
    public override bool Applies(LordJob_Ritual ritual) =>
        ritual?.selectedTarget.Thing is FeatherTrophy;

    public override bool DataRequired => false;

    public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
    {
        if (ritual?.selectedTarget.Thing is not FeatherTrophy featherTrophy)
            return 0f;
        
        return AdorningQuality.GetTaleScore(featherTrophy.HasTale);
    }

    public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
    {
        return "TribalTrophies_AdorningCeremony_RitualDialog_Tale".Translate();
    }

    public override QualityFactor GetQualityFactor(
        Precept_Ritual ritual,
        TargetInfo ritualTarget,
        RitualObligation obligation,
        RitualRoleAssignments assignments,
        RitualOutcomeComp_Data data)
    {
        if (ritualTarget.Thing is not FeatherTrophy featherTrophy)
            return null;

        var contribution = AdorningQuality.GetTaleScore(featherTrophy.HasTale);
        
        return new QualityFactor
        {
            label = "TribalTrophies_AdorningCeremony_RitualDialog_Tale".Translate(),
            count = featherTrophy.HasTale ? "TribalTrophies_AdorningCeremony_RitualDialog_Tale_Written".Translate() : "TribalTrophies_AdorningCeremony_RitualDialog_Tale_None".Translate(),
            qualityChange = $"+{contribution.ToStringPercent()}",
            quality = contribution,
            positive = featherTrophy.HasTale,
            present = true,
            toolTip = featherTrophy.HasTale 
                ? $"\"{featherTrophy.tale}\"\n\n" + "TribalTrophies_AdorningCeremony_RitualDialog_Tale_Written_Tooltip".Translate(AdorningQuality.TaleWeight.ToStringPercent().Named("QUALITY"))
                : "TribalTrophies_AdorningCeremony_RitualDialog_Tale_None_Tooltip".Translate(AdorningQuality.TaleWeight.ToStringPercent().Named("QUALITY")),
            priority = 30f
        };
    }
}