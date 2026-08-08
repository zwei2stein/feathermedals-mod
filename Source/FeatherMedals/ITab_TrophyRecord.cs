using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class ITab_TrophyRecord : ITab
{

    private static readonly Color MutedColor = new(0.7f, 0.7f, 0.7f);

    public ITab_TrophyRecord()
    {
        this.size = new Vector2(400f, 520f);
        this.labelKey = "FeatherMedals_Record";
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
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        var nameHeight = Text.CalcHeight(medal.MedalLabel, rect.width);
        Widgets.Label(new Rect(rect.x, curY, rect.width, nameHeight), medal.MedalLabel);
        curY += nameHeight + 6f;

        // Award details, centered
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;

        if (medal.BiocodeComp is { Biocoded: true, CodedPawn: not null })
        {
            var awardLine = "FeatherMedals_Inspector_AwardedTo".Translate(medal.BiocodeComp.CodedPawn.Named("PAWN"));
            var awardHeight = Text.CalcHeight(awardLine, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, awardHeight), awardLine);
            curY += awardHeight + 4f;
        }
        
        if (medal.awardedBy != null)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = MutedColor;
            var presenterLine = medal.GetAwardedByLabel();
            var presenterHeight = Text.CalcHeight(presenterLine, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, presenterHeight), presenterLine);
            curY += presenterHeight + 4f;
            Text.Font = GameFont.Small;
        }

        // Date and ceremony quality on one line
        if (medal.awardedTick >= 0)
        {
            Text.Font = GameFont.Tiny;
            var tile = medal.Wearer?.Map?.Tile
                       ?? medal.MapHeld?.Tile
                       ?? Find.CurrentMap?.Tile ?? 0;
            var dateStr = GenDate.DateFullStringAt(
                GenDate.TickGameToAbs(medal.awardedTick),
                Find.WorldGrid.LongLatOf(tile));

            var dateLine = dateStr;
            if (medal.ceremonyQuality >= 0)
                dateLine += "FeatherMedals_AwardedAtQualityCeremony".Translate(AdorningQuality.GetQualityLabel(medal.ceremonyQuality).CapitalizeFirst());

            GUI.color = MutedColor;
            var dateHeight = Text.CalcHeight(dateLine, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, dateHeight), dateLine);
            GUI.color = Color.white;
            curY += dateHeight + 4f;
            Text.Font = GameFont.Small;
        }

        // Days worn
        if (medal.awardedTick >= 0)
        {
            Text.Font = GameFont.Tiny;
            var days = (Find.TickManager.TicksGame - medal.awardedTick) / GenDate.TicksPerDay;
            GUI.color = MutedColor;
            var daysText = "FeatherMedals_MedalWornFor".Translate(days.ToString());
            var daysHeight = Text.CalcHeight(daysText, rect.width);
            Widgets.Label(new Rect(rect.x, curY, rect.width, daysHeight), daysText);
            GUI.color = Color.white;
            curY += daysHeight + 4f;
            Text.Font = GameFont.Small;
        }

        curY += 6f;

        // Citation, gold, centered
        if (!medal.citation.NullOrEmpty())
        {
            UIHelper.DrawCitation(medal.def, medal.citation, rect, ref curY);
        }

        // honor bonus, gold, centered
        var ext = medal.def.GetModExtension<TrophyExtension>();
        var honor = ext?.honorAwarded ?? 0;
        if (ModsConfig.RoyaltyActive && honor > 0)
        {
            UIHelper.DrawHonorSummary(medal.def, rect, ref curY);
        }
        
        // Stat bonuses, green, centered
        UIHelper.DrawStatSummary(medal.def, rect, ref curY);

        // Trait effects, centered
        if (MedalMod.Settings.TrophyDynamicTraits && medal.BiocodeComp is not { Biocoded: true })
        {
            UIHelper.DrawTraitsSummary(medal.def, rect, ref curY);
        }

        // Reset
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }
}