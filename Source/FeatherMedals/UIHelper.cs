using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Generic drawing helpers
public static class UIHelper
{
    public static float LineHeight(string text, GameFont font, float width, float gapAfter = TrophyUIStyle.DetailLineGap)
    {
        Text.Font = font;
        var height = Text.CalcHeight(text, width) + gapAfter;
        Text.Font = GameFont.Small;
        return height;
    }

    // measures, draws and moves curY past the line
    public static void DrawLine(string text, GameFont font, Color color, TextAnchor anchor, Rect area, ref float curY, float gapAfter = TrophyUIStyle.DetailLineGap)
    {
        Text.Font = font;
        Text.Anchor = anchor;
        var height = Text.CalcHeight(text, area.width);
        // colored as text, not by GUI.color, so colored names inside keep their own color
        Widgets.Label(new Rect(area.x, curY, area.width, height), text.Colorize(color));
        curY += height + gapAfter;
        Text.Font = GameFont.Small;
    }

    public static void DrawSeparator(float x, float y, float width)
    {
        GUI.color = TrophyUIStyle.SeparatorColor;
        Widgets.DrawLineHorizontal(x, y, width);
        GUI.color = Color.white;
    }

    public static void DrawSeparatorVertical(float x, float y, float height)
    {
        GUI.color = TrophyUIStyle.SeparatorColor;
        Widgets.DrawLineVertical(x, y, height);
        GUI.color = Color.white;
    }

    // one stat offset as shown everywhere: "Research speed: +5%"
    public static string StatOffsetLine(StatModifier mod)
        => $"{mod.stat.LabelCap}: {mod.ValueToStringAsOffset}";

    public static float StatSummaryHeight(ThingDef def)
    {
        var statOffsets = def.equippedStatOffsets;
        if (statOffsets is not { Count: > 0 })
            return 0f;
        return statOffsets.Count * TrophyUIStyle.FixedLineHeight + TrophyUIStyle.BlockGap;
    }

    public static void DrawStatSummary(ThingDef def, Rect rect, ref float curY)
    {
        var statOffsets = def.equippedStatOffsets;
        if (statOffsets is { Count: > 0 })
        {
            Text.Font = GameFont.Tiny;
            GUI.color = TrophyUIStyle.GreenColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (var stat in statOffsets)
            {
                Widgets.Label(new Rect(rect.x, curY, rect.width, TrophyUIStyle.FixedLineHeight), StatOffsetLine(stat));
                curY += TrophyUIStyle.FixedLineHeight;
            }
            GUI.color = Color.white;
            curY += TrophyUIStyle.BlockGap;
            Text.Font = GameFont.Small;
        }
    }

    public static string GetStatSummary(ThingDef def)
    {
        var offsets = def.equippedStatOffsets;
        if (offsets == null || offsets.Count == 0) return null;

        var lines = new List<string>();
        foreach (var mod in offsets)
            lines.Add(StatOffsetLine(mod));
        return string.Join(", ", lines);
    }
}