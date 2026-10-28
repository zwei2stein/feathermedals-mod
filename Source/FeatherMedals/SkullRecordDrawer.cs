using UnityEngine;
using Verse;

namespace FeatherMedals;

// One skull: vanilla only remembers whose it was.
public class SkullRecordDrawer : ITrophyRecordRow
{
    private readonly Thing skull;
    private readonly string victimName;

    public SkullRecordDrawer(Thing skull, string victimName)
    {
        this.skull = skull;
        this.victimName = victimName;
    }

    // Skull of (victim), or just the def label when nobody is remembered
    private string Title => victimName.NullOrEmpty()
        ? skull.def.LabelCap.ToString()
        : "TribalTrophies_Trophy_SkullTab_SkullOf".Translate(
            victimName.StripTags().Colorize(TrophyUIStyle.GoldColor).Named("VICTIM")).Resolve();

    public float Height(float width)
    {
        var textHeight = UIHelper.LineHeight(Title, GameFont.Small, TextWidth(width), 0f);
        return Mathf.Max(TrophyUIStyle.SkullIconSize, textHeight) + TrophyUIStyle.RowPadding * 2f;
    }

    public void Draw(Rect rect)
    {
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlight(rect);

        // centered in its column
        var size = TrophyUIStyle.SkullIconSize;
        var column = new Rect(rect.x + TrophyUIStyle.SidePadding, rect.y, TrophyUIStyle.IconColumnWidth, rect.height);
        var pictureRect = new Rect(column.center.x - size / 2f, column.center.y - size / 2f, size, size);
        Widgets.ThingIcon(pictureRect, skull, stackOfOne: true);

        // title centered against the picture
        var title = Title;
        var textWidth = TextWidth(rect.width);
        var textArea = new Rect(column.xMax + TrophyUIStyle.ColumnGap, rect.y, textWidth, rect.height);
        var curY = rect.y + (rect.height - UIHelper.LineHeight(title, GameFont.Small, textWidth, 0f)) / 2f;
        UIHelper.DrawLine(title, GameFont.Small, Color.white, TextAnchor.MiddleCenter, textArea, ref curY, 0f);

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static float TextWidth(float rowWidth)
    {
        return rowWidth - TrophyUIStyle.SidePadding - TrophyUIStyle.IconColumnWidth - TrophyUIStyle.ColumnGap - TrophyUIStyle.SidePadding;
    }
}