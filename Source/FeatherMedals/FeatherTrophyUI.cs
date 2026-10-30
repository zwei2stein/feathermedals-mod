using UnityEngine;
using Verse;

namespace TribalTrophies;

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

    public static float TaleHeight(string tale, float width)
    {
        return Text.CalcHeight($"\"{tale}\"", width - TrophyUIStyle.TaleSideMargin * 2f) + TrophyUIStyle.TaleGap;
    }

    public static void DrawTale(string tale, Rect rect, ref float curY)
    {
        var previousAnchor = Text.Anchor;
        GUI.color = TrophyUIStyle.GoldColor;
        Text.Anchor = TextAnchor.MiddleCenter;
        var taleText = $"\"{tale}\"";
        var taleHeight = Text.CalcHeight(taleText, rect.width - TrophyUIStyle.TaleSideMargin * 2f);
        Widgets.Label(new Rect(rect.x + TrophyUIStyle.TaleSideMargin, curY, rect.width - TrophyUIStyle.TaleSideMargin * 2f, taleHeight), taleText);
        GUI.color = Color.white;
        Text.Anchor = previousAnchor;
        curY += taleHeight + TrophyUIStyle.TaleGap;
    }

    public static float TraitChangeSummaryHeight(FeatherTrophy featherTrophy, float width)
    {
        var height = 0f;
        foreach (var text in new[] { featherTrophy.GetDecoratedTraitLabel(true), featherTrophy.GetAddedTraitLabel(true), featherTrophy.GetRemovedTraitLabel(true) })
        {
            if (text != null)
                height += UIHelper.CompactLineHeight(text, width);
        }
        return height;
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

        UIHelper.DrawCompactLine(text, color, anchor, area, ref curY);
    }
}