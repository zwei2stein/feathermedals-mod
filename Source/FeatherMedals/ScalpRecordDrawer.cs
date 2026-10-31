using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class ScalpRecordDrawer : TrophyRowDrawer
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

    protected override Thing IconThing => scalp;

    protected override float IconSize => TrophyUIStyle.TrophyBodyPartIconSize;

    // text block centered against the picture
    protected override float LayoutText(Rect area, bool draw)
    {
        var curY = area.y;
        foreach (var line in Lines())
            UIHelper.LayoutLine(line.text, line.font, line.color, TextAnchor.MiddleCenter, area, ref curY, draw, gapAfter: line.gapAfter);
        return curY - area.y;
    }

    // Scalp of (victim)        / (xenotype) of (faction)
    // (date) at (settlement)
    // By (scalper)             / of (faction)
    private List<Line> Lines()
    {
        var lines = new List<Line>();
        var victim = scalp.Victim;
        var scalper = scalp.Taker;
        var scalped = scalp.Taken;

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