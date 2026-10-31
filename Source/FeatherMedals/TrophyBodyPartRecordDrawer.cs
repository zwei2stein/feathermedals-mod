using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class TrophyBodyPartRecordDrawer : TrophyRowDrawer
{
    private readonly TrophyBodyPart trophy;

    public TrophyBodyPartRecordDrawer(TrophyBodyPart trophy)
    {
        this.trophy = trophy;
    }

    private class Line
    {
        public string text;
        public GameFont font;
        public Color color;
        public float gapAfter = TrophyUIStyle.RowLineGap;
    }

    protected override Thing IconThing => trophy;

    protected override float IconSize => TrophyUIStyle.TrophyBodyPartIconSize;

    // text block centered against the picture
    protected override float LayoutText(Rect area, bool draw)
    {
        var curY = area.y;
        foreach (var line in Lines())
            UIHelper.LayoutLine(line.text, line.font, line.color, TextAnchor.MiddleCenter, area, ref curY, draw, gapAfter: line.gapAfter);
        return curY - area.y;
    }

    // (Trophy) of (victim)     / (xenotype) of (faction)
    // (date) at (settlement)
    // (Taken) by (taker)       / of (faction)
    private List<Line> Lines()
    {
        var lines = new List<Line>();
        var victim = trophy.Victim;
        var taker = trophy.Taker;
        var taken = trophy.Taken;

        var victimName = victim?.Name;
        Add(lines, victimName.NullOrEmpty()
            ? trophy.LabelCap.ToString()
            : trophy.RecordTitle(Highlight(victimName, TrophyUIStyle.GoldColor)),
            GameFont.Small, Color.white);
        Add(lines, Origin(trophy.XenotypeName, victim?.FactionNameColored));
        EndBlock(lines);

        if (taken != null)
        {
            var settlement = taken.SettlementLabel;
            var placePart = settlement == null
                ? ""
                : "TribalTrophies_TrophyBodyPart_RecordTab_At".Translate(settlement.Named("SETTLEMENT")).Resolve();
            Add(lines, "TribalTrophies_TrophyBodyPart_RecordTab_When".Translate(
                taken.DateLabel.Named("DATE"), placePart.Named("PLACEPART")).Resolve());
            EndBlock(lines);
        }

        var takerName = taker?.Name;
        if (!takerName.NullOrEmpty())
        {
            Add(lines, trophy.RecordTakenBy(Highlight(takerName, TrophyUIStyle.GreenColor)));
            Add(lines, OfFaction(taker.FactionNameColored));
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
        return "TribalTrophies_TrophyBodyPart_RecordTab_XenotypeOf".Translate(
            xenotype.CapitalizeFirst().Named("XENOTYPE"), faction.Named("FACTION")).Resolve();
    }

    private static string Highlight(string name, Color color)
    {
        return name.StripTags().Colorize(color);
    }

    private static string OfFaction(string faction)
    {
        return faction.NullOrEmpty() ? null : "TribalTrophies_TrophyBodyPart_RecordTab_Of".Translate(faction.Named("FACTION")).Resolve();
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
