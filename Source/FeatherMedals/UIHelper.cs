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

    // one pass for measuring (draw false) and drawing: moves curY past the gap before, the line and the gap after
    public static void LayoutLine(string text, GameFont font, Color color, TextAnchor anchor, Rect area, ref float curY, bool draw,
        float gapBefore = 0f, float gapAfter = 0f)
    {
        curY += gapBefore;
        if (draw)
            DrawLine(text, font, color, anchor, area, ref curY, gapAfter);
        else
            curY += LineHeight(text, font, area.width, gapAfter);
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

    public static IEnumerable<string> StatOffsetLines(ThingDef def)
    {
        if (def.equippedStatOffsets == null)
            yield break;
        foreach (var mod in def.equippedStatOffsets)
            yield return StatOffsetLine(mod);
    }

    public static float CompactLinesHeight(IEnumerable<string> lines, float width)
    {
        var height = 0f;
        foreach (var line in lines)
            height += CompactLineHeight(line, width);
        return height;
    }

    public static float CompactLineHeight(string text, float width)
    {
        Text.Font = GameFont.Tiny;
        var height = Text.CalcHeight(text, width);
        Text.Font = GameFont.Small;
        return height;
    }

    public static void DrawCompactLines(IEnumerable<string> lines, Color color, TextAnchor anchor, Rect area, ref float curY)
    {
        foreach (var line in lines)
            DrawCompactLine(line, color, anchor, area, ref curY);
    }

    public static void DrawCompactLine(string text, Color color, TextAnchor anchor, Rect area, ref float curY)
    {
        var height = CompactLineHeight(text, area.width);
        Text.Font = GameFont.Tiny;
        Text.Anchor = anchor;
        // colored as text, not by GUI.color, so colored words inside (quality colors) keep their own color
        Widgets.Label(new Rect(area.x, curY, area.width, height), text.Colorize(color));
        curY += height;
        Text.Font = GameFont.Small;
    }
}