using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class FeatherTrophyUI
{
    // ceremony stages: 0 poor, 1 decent, 2 grand, 3 legendary
    public static Color CeremonyQualityColor(int stage)
    {
        return stage switch
        {
            3 => TrophyUIStyle.GoldColor,
            2 => TrophyUIStyle.GreenColor,
            1 => Color.white,
            0 => TrophyUIStyle.MutedColor,
            _ => Color.white, // default fallback
        };
    }

    public static string CeremonyQualityLabelColored(int stage)
    {
        return AdorningQuality.GetQualityLabel(stage).CapitalizeFirst().Colorize(CeremonyQualityColor(stage));
    }

    public static float CitationHeight(string citation, float width)
    {
        return Text.CalcHeight($"\"{citation}\"", width - TrophyUIStyle.CitationSideMargin * 2f) + TrophyUIStyle.CitationGap;
    }

    public static void DrawCitation(string citation, Rect rect, ref float curY)
    {
        var previousAnchor = Text.Anchor;
        GUI.color = TrophyUIStyle.GoldColor;
        Text.Anchor = TextAnchor.MiddleCenter;
        var citationText = $"\"{citation}\"";
        var citationHeight = Text.CalcHeight(citationText, rect.width - TrophyUIStyle.CitationSideMargin * 2f);
        var citationRect = new Rect(rect.x + TrophyUIStyle.CitationSideMargin, curY, rect.width - TrophyUIStyle.CitationSideMargin * 2f, citationHeight);
        Widgets.Label(citationRect, citationText);
        GUI.color = Color.white;
        Text.Anchor = previousAnchor;
        curY += citationHeight + TrophyUIStyle.CitationGap;
    }

    public static void DrawTraitChangeSummary(FeatherTrophy featherTrophy, float textX, Rect rect, ref float curY)
    {
        DrawTraitChangeSummary(featherTrophy, new Rect(textX, rect.y, rect.xMax - textX, rect.height), ref curY, TextAnchor.MiddleLeft);
    }

    public static void DrawTraitChangeSummary(FeatherTrophy featherTrophy, Rect area, ref float curY, TextAnchor anchor)
    {
        DrawTraitChangeLine(featherTrophy.GetDecoratedTraitLabel(true), TrophyUIStyle.GoldColor, area, ref curY, anchor);
        DrawTraitChangeLine(featherTrophy.GetAddedTraitLabel(true), Color.green, area, ref curY, anchor);
        DrawTraitChangeLine(featherTrophy.GetRemovedTraitLabel(true), Color.red, area, ref curY, anchor);
    }

    private static void DrawTraitChangeLine(string text, Color color, Rect area, ref float curY, TextAnchor anchor)
    {
        if (text == null) return;

        Text.Font = GameFont.Tiny;
        GUI.color = color;
        Text.Anchor = anchor;
        Widgets.Label(new Rect(area.x, curY, area.width, TrophyUIStyle.FixedLineHeight), text);
        GUI.color = Color.white;
        curY += TrophyUIStyle.FixedLineHeight;
        Text.Font = GameFont.Small;
    }

    public static float TraitsSummaryHeight(FeatherTrophy trophy)
    {
        var ext = trophy.def.GetModExtension<TrophyExtension>();
        if (ext == null)
            return 0f;
        var count = (ext.removesTraits?.Count ?? 0) + (ext.addsTraits?.Count ?? 0);
        return count * TrophyUIStyle.FixedLineHeight;
    }

    public static void DrawTraitsSummary(FeatherTrophy trophy, Rect rect, ref float curY)
    {
        var ext = trophy.def.GetModExtension<TrophyExtension>();
        var chanceMultiplier = RitualOutcomeTraitUtil.GetTrophyTraitChanceMultiplier(trophy);

        Text.Font = GameFont.Tiny;

        if (ext != null && ((ext.removesTraits is { Count: > 0 }) || (ext.addsTraits is { Count: > 0 })))
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            if (ext.removesTraits != null)
            {
                foreach (var entry in ext.removesTraits)
                {
                    var label = entry.Label;
                    GUI.color = TrophyUIStyle.MutedColor;
                    var line = "TribalTrophies_AdorningCeremony_Common_MayRemoveTrait".Translate(
                        label.CapitalizeFirst(),
                        (entry.chance * chanceMultiplier).ToStringPercent());
                    Widgets.Label(new Rect(rect.x, curY, rect.width, TrophyUIStyle.FixedLineHeight), line);
                    curY += TrophyUIStyle.FixedLineHeight;
                }
            }
            if (ext.addsTraits != null)
            {
                foreach (var entry in ext.addsTraits)
                {
                    var label = entry.Label;
                    GUI.color = TrophyUIStyle.MutedColor;
                    var line = "TribalTrophies_AdorningCeremony_Common_MayGrantTrait".Translate(
                        label.CapitalizeFirst(),
                        (entry.chance * chanceMultiplier).ToStringPercent());
                    Widgets.Label(new Rect(rect.x, curY, rect.width, TrophyUIStyle.FixedLineHeight), line);
                    curY += TrophyUIStyle.FixedLineHeight;
                }
            }
        }

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
    }
}