using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class ITab_TrophyRecord : ITab
{

    public ITab_TrophyRecord()
    {
        this.size = new Vector2(400f, 520f);
        this.labelKey = "TribalTrophies_Trophy_TrophyTab_Label";
    }

    public override bool IsVisible => SelThing is FeatherMedal;

    protected override void FillTab()
    {
        if (SelThing is not FeatherMedal medal) return;

        var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(16f);
        var curY = rect.y;

        // Medal icon, centered
        var iconSize = 128f;
        var iconRect = new Rect(rect.x + (rect.width - iconSize) / 2f, curY, iconSize, iconSize);
        Widgets.ThingIcon(iconRect, medal);
        curY += iconSize + 10f;

        // Medal name, centered
        UIHelper.DrawLine(medal.MedalLabel, GameFont.Medium, Color.white, TextAnchor.MiddleCenter, rect, ref curY, 6f);

        // Award details, centered
        if (medal.BiocodeComp is { Biocoded: true, CodedPawn: not null })
        {
            var awardLine = "TribalTrophies_AdorningCeremony_Common_Honoring".Translate(medal.BiocodeComp.CodedPawn.Named("PAWN"));
            UIHelper.DrawLine(awardLine, GameFont.Small, Color.white, TextAnchor.MiddleCenter, rect, ref curY);
        }

        if (medal.awardedBy != null)
        {
            UIHelper.DrawLine(medal.GetAwardedByLabel(), GameFont.Tiny, UIHelper.MutedColor, TextAnchor.MiddleCenter, rect, ref curY);
        }

        // Date and ceremony quality on one line
        var dateStr = medal.GetAwardedDateLabel();
        if (dateStr != null)
        {
            var dateLine = dateStr;
            if (medal.ceremonyQuality >= 0)
                dateLine = "TribalTrophies_AdorningCeremony_TrophyTab_AdornedAtCeremony".Translate(
                    dateStr.Named("DATE"),
                    AdorningQuality.GetQualityLabel(medal.ceremonyQuality).Named("QUALITY"));

            UIHelper.DrawLine(dateLine, GameFont.Tiny, UIHelper.MutedColor, TextAnchor.MiddleCenter, rect, ref curY);
        }

        // Days worn
        if (medal.awarded != null)
        {
            var days = medal.awarded.TicksAgo / GenDate.TicksPerDay;
            var daysText = "TribalTrophies_AdorningCeremony_TrophyTab_WornFor".Translate(days.ToString());
            UIHelper.DrawLine(daysText, GameFont.Tiny, UIHelper.MutedColor, TextAnchor.MiddleCenter, rect, ref curY);
        }

        curY += 6f;

        // Citation, gold, centered
        if (medal.HasCitation)
        {
            UIHelper.DrawCitation(medal.def, medal.citation, rect, ref curY);
        }

        // Stat bonuses, green, centered
        UIHelper.DrawStatSummary(medal.def, rect, ref curY);

        // Trait effects, centered
        if (MedalMod.Settings.TrophyDynamicTraits && medal.BiocodeComp is not { Biocoded: true })
        {
            UIHelper.DrawTraitsSummary(medal, rect, ref curY);
        }

        // Reset
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }
}