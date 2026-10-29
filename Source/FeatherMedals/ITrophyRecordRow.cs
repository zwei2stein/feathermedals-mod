using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public interface ITrophyRecordRow
{
    float Height(float width);
    void Draw(Rect rect);
}

public abstract class ITab_TrophyRows : ITab
{
    private Vector2 scrollPosition;

    protected ITab_TrophyRows(string labelKey)
    {
        size = TrophyUIStyle.TabSize;
        this.labelKey = labelKey;
    }

    protected abstract List<ITrophyRecordRow> Rows();

    protected abstract string Header(int rowCount);

    public override bool IsVisible => Rows().Count > 0;

    protected override void FillTab()
    {
        var rows = Rows();
        if (rows.Count == 0) return;

        var outerRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(TrophyUIStyle.TabPadding);

        var headerRect = new Rect(outerRect.x, outerRect.y, outerRect.width, TrophyUIStyle.HeaderHeight);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(headerRect, Header(rows.Count));
        Text.Font = GameFont.Small;

        var listTop = headerRect.yMax + TrophyUIStyle.TabPadding;
        var listRect = new Rect(outerRect.x, listTop, outerRect.width, outerRect.yMax - listTop);
        var viewWidth = listRect.width - TrophyUIStyle.ScrollbarWidth;

        var heights = new List<float>();
        var totalHeight = 0f;
        foreach (var row in rows)
        {
            var height = row.Height(viewWidth);
            heights.Add(height);
            totalHeight += height;
        }
        totalHeight += TrophyUIStyle.RowGap * (rows.Count - 1);
        var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

        Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

        var curY = 0f;
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0)
                UIHelper.DrawSeparator(0f, curY - TrophyUIStyle.RowGap / 2f, viewWidth);

            rows[i].Draw(new Rect(0f, curY, viewWidth, heights[i]));
            curY += heights[i] + TrophyUIStyle.RowGap;
        }

        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }
}