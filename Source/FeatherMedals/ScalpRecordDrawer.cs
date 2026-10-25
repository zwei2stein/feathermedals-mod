using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class ScalpRecordDrawer : ITrophyRecordRow
{
    private readonly Scalp scalp;

    public ScalpRecordDrawer(Scalp scalp)
    {
        this.scalp = scalp;
    }

    private class Line
    {
        public string text;
        public GameFont font;
        public Color color;
        public float gapAfter = TrophyUIStyle.RowLineGap;
    }

    public float Height(float width)
    {
        return Mathf.Max(TrophyUIStyle.ScalpIconSize, TextHeight(Lines(), TextWidth(width))) + TrophyUIStyle.RowPadding * 2f;
    }

    public void Draw(Rect rect)
    {
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlight(rect);

        // centered in its column
        var size = TrophyUIStyle.ScalpIconSize;
        var column = new Rect(rect.x + TrophyUIStyle.SidePadding, rect.y, TrophyUIStyle.IconColumnWidth, rect.height);
        var pictureRect = new Rect(column.center.x - size / 2f, column.center.y - size / 2f, size, size);
        Widgets.ThingIcon(pictureRect, scalp);

        // text block centered against the picture
        var lines = Lines();
        var textWidth = TextWidth(rect.width);
        var textArea = new Rect(column.xMax + TrophyUIStyle.ColumnGap, rect.y, textWidth, rect.height);
        var curY = rect.y + (rect.height - TextHeight(lines, textWidth)) / 2f;
        foreach (var line in lines)
            UIHelper.DrawLine(line.text, line.font, line.color, TextAnchor.MiddleCenter, textArea, ref curY, line.gapAfter);

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static float TextWidth(float rowWidth)
    {
        return rowWidth - TrophyUIStyle.SidePadding - TrophyUIStyle.IconColumnWidth - TrophyUIStyle.ColumnGap - TrophyUIStyle.SidePadding;
    }

    private static float TextHeight(List<Line> lines, float width)
    {
        var height = 0f;
        foreach (var line in lines)
            height += UIHelper.LineHeight(line.text, line.font, width, line.gapAfter);
        return height;
    }

    // Scalp of (victim)        / (xenotype) of (faction)
    // (date) at (settlement)
    // By (scalper)             / of (faction)
    private List<Line> Lines()
    {
        var lines = new List<Line>();
        var victim = scalp.Victim;
        var scalper = scalp.Scalper;
        var scalped = scalp.Scalped;

        var victimName = victim?.Name;
        Add(lines, victimName.NullOrEmpty()
            ? scalp.LabelCap.ToString()
            : "TribalTrophies_Scalping_ScalpTab_ScalpOf".Translate(Highlight(victimName, TrophyUIStyle.GoldColor).Named("VICTIM")).Resolve(),
            GameFont.Small, Color.white);
        Add(lines, Origin(scalp.XenotypeName, victim?.FactionNameColored));
        EndBlock(lines);

        if (scalped != null)
        {
            var settlement = scalped.SettlementLabel;
            var placePart = settlement == null
                ? ""
                : "TribalTrophies_Scalping_ScalpTab_At".Translate(settlement.Named("SETTLEMENT")).Resolve();
            Add(lines, "TribalTrophies_Scalping_ScalpTab_When".Translate(
                scalped.DateLabel.Named("DATE"), placePart.Named("PLACEPART")).Resolve());
            EndBlock(lines);
        }

        var scalperName = scalper?.Name;
        if (!scalperName.NullOrEmpty())
        {
            Add(lines, "TribalTrophies_Scalping_ScalpTab_By".Translate(Highlight(scalperName, TrophyUIStyle.GreenColor).Named("SCALPER")).Resolve());
            Add(lines, OfFaction(scalper.FactionNameColored));
        }

        // nothing below the last line
        if (lines.Count > 0)
            lines[lines.Count - 1].gapAfter = 0f;
        return lines;
    }

    private static string Origin(string xenotype, string faction)
    {
        if (xenotype.NullOrEmpty())
            return OfFaction(faction);
        if (faction.NullOrEmpty())
            return xenotype.CapitalizeFirst();
        return "TribalTrophies_Scalping_ScalpTab_XenotypeOf".Translate(
            xenotype.CapitalizeFirst().Named("XENOTYPE"), faction.Named("FACTION")).Resolve();
    }

    private static string Highlight(string name, Color color)
    {
        return name.StripTags().Colorize(color);
    }

    private static string OfFaction(string faction)
    {
        return faction.NullOrEmpty() ? null : "TribalTrophies_Scalping_ScalpTab_Of".Translate(faction.Named("FACTION")).Resolve();
    }

    private static void Add(List<Line> lines, string text, GameFont font = GameFont.Tiny, Color? color = null)
    {
        if (text.NullOrEmpty())
            return;
        lines.Add(new Line { text = text, font = font, color = color ?? TrophyUIStyle.MutedColor });
    }

    private static void EndBlock(List<Line> lines)
    {
        if (lines.Count > 0)
            lines[lines.Count - 1].gapAfter = TrophyUIStyle.BlockGap;
    }
}