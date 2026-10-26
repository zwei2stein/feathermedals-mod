using UnityEngine;
using Verse;

namespace FeatherMedals;

// not yet awarded feather trophy,
// possibly with citation,
// with potential effects like trait changes that are yet to be realized
// but no award ceremony details (it did not happen yet)
public class FeatherTrophyDetailDrawer : ITrophyRecordRow
{
    private readonly FeatherTrophy _trophy;
    private readonly string citation;

    public FeatherTrophyDetailDrawer(FeatherTrophy trophy) : this(trophy, trophy.citation)
    {
    }

    // citation being written, shown instead of the saved one
    public FeatherTrophyDetailDrawer(FeatherTrophy trophy, string citation)
    {
        this._trophy = trophy;
        this.citation = citation;
    }

    public float Height(float width) => Layout(new Rect(0f, 0f, width, 0f), false);

    public void Draw(Rect rect) => Layout(rect, true);

    private float Layout(Rect rect, bool draw)
    {
        var curY = rect.y;

        // Medal icon, centered
        var iconSize = TrophyUIStyle.DetailIconSize;
        if (draw)
        {
            var iconRect = new Rect(rect.x + (rect.width - iconSize) / 2f, curY, iconSize, iconSize);
            Widgets.ThingIcon(iconRect, _trophy);
        }
        curY += iconSize + TrophyUIStyle.DetailIconGap;

        // Medal name, centered
        var description = _trophy.def.description;
        var hasDescription = !description.NullOrEmpty();
        Line(_trophy.TrophyLabel, GameFont.Medium, Color.white, rect, ref curY, draw,
            hasDescription ? TrophyUIStyle.DetailLineGap : TrophyUIStyle.DetailSectionGap);

        // Description, muted, centered
        if (hasDescription)
            Line(description, GameFont.Tiny, TrophyUIStyle.MutedColor, rect, ref curY, draw, TrophyUIStyle.DetailSectionGap);

        // Citation, gold, centered
        if (!citation.NullOrEmpty())
        {
            if (draw)
                FeatherTrophyUI.DrawCitation(citation, rect, ref curY);
            else
                curY += FeatherTrophyUI.CitationHeight(citation, rect.width);
        }

        // Stat bonuses, green, centered
        if (draw)
            UIHelper.DrawStatSummary(_trophy.def, rect, ref curY);
        else
            curY += UIHelper.StatSummaryHeight(_trophy.def);

        // Trait effects, centered
        if (TribalTrophiesMod.Settings.TrophyDynamicTraits && _trophy.BiocodeComp is not { Biocoded: true })
        {
            if (draw)
                FeatherTrophyUI.DrawTraitsSummary(_trophy, rect, ref curY);
            else
                curY += FeatherTrophyUI.TraitsSummaryHeight(_trophy);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        return curY - rect.y;
    }

    private static void Line(string text, GameFont font, Color color, Rect rect, ref float curY, bool draw,
        float gapAfter = TrophyUIStyle.DetailLineGap)
    {
        if (draw)
            UIHelper.DrawLine(text, font, color, TextAnchor.MiddleCenter, rect, ref curY, gapAfter);
        else
            curY += UIHelper.LineHeight(text, font, rect.width, gapAfter);
    }
}