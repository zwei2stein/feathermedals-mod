using UnityEngine;
using Verse;

namespace TribalTrophies;

public abstract class TrophyRowDrawer : ITrophyRecordRow
{
    protected abstract Thing IconThing { get; }

    protected abstract float IconSize { get; }

    protected virtual bool IconStackOfOne => false;

    protected virtual float ReservedRightWidth => 0f;

    protected virtual float MinHeight => IconSize + TrophyUIStyle.RowPadding * 2f;

    protected virtual bool CenterText => true;

    protected abstract float LayoutText(Rect area, bool draw);

    protected virtual void DrawExtras(Rect rowRect)
    {
    }

    public float Height(float width)
    {
        var textHeight = LayoutText(new Rect(0f, 0f, TextWidth(width), 0f), false);
        return Mathf.Max(MinHeight, textHeight + TrophyUIStyle.RowPadding * 2f);
    }

    public void Draw(Rect rect)
    {
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlight(rect);

        // icon, centered in left column
        var size = IconSize;
        var column = new Rect(rect.x + TrophyUIStyle.SidePadding, rect.y, TrophyUIStyle.IconColumnWidth, rect.height);
        var iconRect = new Rect(column.center.x - size / 2f, column.center.y - size / 2f, size, size);
        Widgets.ThingIcon(iconRect, IconThing, stackOfOne: IconStackOfOne);

        // text in right column
        var textWidth = TextWidth(rect.width);
        var top = rect.y + TrophyUIStyle.RowPadding;
        if (CenterText)
            top = rect.y + (rect.height - LayoutText(new Rect(0f, 0f, textWidth, 0f), false)) / 2f;
        LayoutText(new Rect(column.xMax + TrophyUIStyle.ColumnGap, top, textWidth, rect.height), true);

        DrawExtras(rect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    private float TextWidth(float rowWidth)
    {
        return rowWidth - TrophyUIStyle.SidePadding - TrophyUIStyle.IconColumnWidth - TrophyUIStyle.ColumnGap
               - ReservedRightWidth - TrophyUIStyle.SidePadding;
    }
}
