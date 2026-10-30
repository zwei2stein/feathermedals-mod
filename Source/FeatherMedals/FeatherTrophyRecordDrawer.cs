using System.Linq;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// One worn/already adorned feather trophy
// with tale
// with trait change that happened
// with ceremony that has already happened related info
public class FeatherTrophyRecordDrawer : ITrophyRecordRow
{
    private readonly FeatherTrophy _trophy;
    private readonly bool verbose;

    // verbose: also who it honors and how the ceremony went, for views of this one trophy
    public FeatherTrophyRecordDrawer(FeatherTrophy trophy, bool verbose = false)
    {
        this._trophy = trophy;
        this.verbose = verbose;
    }

    private bool HasAdorningInfo() => _trophy.adornedBy != null || _trophy.adorned != null;

    private string GetHonoringLine()
    {
        if (!verbose || !_trophy.IsAdorned)
            return null;
        return "TribalTrophies_AdorningCeremony_Common_Honoring".Translate(_trophy.AdornedPawn.Name.Named("PAWN"));
    }

    private string GetCraftsmanshipLine()
    {
        if (!verbose)
            return null;
        return _trophy.GetCraftsmanshipLabel();
    }

    private string GetCeremonyQualityLine()
    {
        if (!verbose || _trophy.ceremonyQuality < 0)
            return null;
        return "TribalTrophies_AdorningCeremony_Inspect_CeremonyQuality".Translate(
            FeatherTrophyUI.CeremonyQualityLabelColored(_trophy.ceremonyQuality).Named("QUALITY")).Resolve();
    }

    private string GetAdorningInfo()
    {
        var presentedBy = _trophy.GetAdornedByLabel();
        var date = _trophy.GetAdornedDateLabel();
        if (presentedBy != null && date != null)
            return "TribalTrophies_Trophy_PawnTab_AdornedByOnDate".Translate(
                presentedBy.Named("PRESENTER"),
                date.Named("DATE"));
        return presentedBy ?? date ?? "";
    }

    public float Height(float width)
    {
        var textWidth = GetTextWidth(width);
        var height = TrophyUIStyle.RowPadding;

        // each line below counts the gap drawn above it; the tale is drawn right under the line before it
        Text.Font = GameFont.Small;
        height += Text.CalcHeight(_trophy.TrophyLabel, textWidth);

        Text.Font = GameFont.Tiny;
        var honoring = GetHonoringLine();
        if (honoring != null)
            height += Text.CalcHeight(honoring, textWidth) + TrophyUIStyle.RowLineGap;

        if (_trophy.HasTale)
            height += FeatherTrophyUI.TaleHeight(_trophy.tale, textWidth);
        else
            height += TrophyUIStyle.DescriptionLineHeight + TrophyUIStyle.RowLineGap;

        var statLines = UIHelper.StatOffsetLines(_trophy.def).ToList();
        if (statLines.Count > 0)
            height += TrophyUIStyle.RowLineGap + UIHelper.CompactLinesHeight(statLines, textWidth);

        if (HasAdorningInfo())
            height += Text.CalcHeight(GetAdorningInfo(), textWidth) + TrophyUIStyle.RowLineGap;

        var craftsmanship = GetCraftsmanshipLine();
        if (craftsmanship != null)
            height += Text.CalcHeight(craftsmanship, textWidth) + TrophyUIStyle.RowLineGap;

        var quality = GetCeremonyQualityLine();
        if (quality != null)
            height += Text.CalcHeight(quality, textWidth) + TrophyUIStyle.RowLineGap;

        height += FeatherTrophyUI.TraitChangeSummaryHeight(_trophy, textWidth);

        Text.Font = GameFont.Small;
        height += TrophyUIStyle.RowPadding * 2f;
        return Mathf.Max(TrophyUIStyle.FeatherRowMinHeight, height);
    }

    private void DrawLockToggle(Rect rowRect)
    {
        var lockSize = TrophyUIStyle.LockButtonSize;
        var lockRect = new Rect(
            rowRect.xMax - lockSize - TrophyUIStyle.SidePadding,
            rowRect.y + TrophyUIStyle.RowPadding,
            lockSize,
            lockSize
        );
        var wearer = _trophy.Wearer;
        var tracker = wearer.apparel;
        var locked = tracker.IsLocked(_trophy);
        var lockIcon = locked ? TrophyTextures.LockedIcon : TrophyTextures.UnlockedIcon;
        var lockTip = locked
            ? "TribalTrophies_Trophy_PawnTab_Locked_Tooltip".Translate()
            : "TribalTrophies_Trophy_PawnTab_Unlocked_Tooltip".Translate();

        // only show when player can change worn things.
        var canToggle = wearer.IsColonistPlayerControlled || StrippableUtility.CanBeStrippedByColony(wearer);

        GUI.color = Color.gray;
        if (canToggle)
        {
            if (Widgets.ButtonImage(lockRect, lockIcon, GUI.color))
            {
                if (locked)
                    tracker.Unlock(_trophy);
                else
                    tracker.Lock(_trophy);
            }
        }
        else
        {
            GUI.DrawTexture(lockRect, lockIcon);
        }
        GUI.color = Color.white;
        TooltipHandler.TipRegion(lockRect, lockTip);
    }

    private static float GetTextWidth(float availableWidth)
    {
        return availableWidth - TrophyUIStyle.SidePadding - TrophyUIStyle.IconColumnWidth - TrophyUIStyle.ColumnGap
               - TrophyUIStyle.ColumnGap - TrophyUIStyle.LockButtonSize - TrophyUIStyle.SidePadding;
    }

    public void Draw(Rect rowRect)
    {
        var rowHeight = rowRect.height;

        if (Mouse.IsOver(rowRect))
            Widgets.DrawHighlight(rowRect);

        // Lock toggle, only while worn; an adorned trophy can also lie on the ground
        if (_trophy.Wearer != null)
            DrawLockToggle(rowRect);

        // Icon, centered in its column
        var iconSize = TrophyUIStyle.FeatherIconSize;
        var column = new Rect(rowRect.x + TrophyUIStyle.SidePadding, rowRect.y, TrophyUIStyle.IconColumnWidth, rowHeight);
        var iconRect = new Rect(column.center.x - iconSize / 2f, column.center.y - iconSize / 2f, iconSize, iconSize);
        Widgets.ThingIcon(iconRect, _trophy);

        var textX = column.xMax + TrophyUIStyle.ColumnGap;
        var textWidth = GetTextWidth(rowRect.width);

        // Feather trophy name
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        var nameHeight = Text.CalcHeight(_trophy.TrophyLabel, textWidth);
        var nameRect = new Rect(textX, rowRect.y + TrophyUIStyle.RowPadding, textWidth, nameHeight);
        Widgets.Label(nameRect, _trophy.TrophyLabel);

        // Honoring, verbose only
        Text.Font = GameFont.Tiny;
        float descBottom = nameRect.yMax;
        var honoring = GetHonoringLine();
        if (honoring != null)
        {
            var honoringHeight = Text.CalcHeight(honoring, textWidth);
            var honoringRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, honoringHeight);
            Widgets.Label(honoringRect, honoring);
            descBottom = honoringRect.yMax;
        }

        Text.Font = GameFont.Tiny;
        if (_trophy.HasTale)
        {
            FeatherTrophyUI.DrawTale(_trophy.tale, nameRect, ref descBottom);
        }
        else
        {
            var descRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, TrophyUIStyle.DescriptionLineHeight);
            GUI.color = TrophyUIStyle.MutedColor;
            Widgets.Label(descRect, (_trophy.def.description ?? "").Truncate(descRect.width));
            GUI.color = Color.white;
            descBottom = descRect.yMax;
        }

        // Stat bonuses summary
        var statLines = UIHelper.StatOffsetLines(_trophy.def).ToList();
        if (statLines.Count > 0)
        {
            descBottom += TrophyUIStyle.RowLineGap;
            var statsArea = new Rect(textX, rowRect.y, textWidth, rowRect.height);
            UIHelper.DrawCompactLines(statLines, TrophyUIStyle.GreenColor, TextAnchor.MiddleLeft, statsArea, ref descBottom);
        }

        FeatherTrophyUI.DrawTraitChangeSummary(_trophy, new Rect(textX, rowRect.y, textWidth, rowRect.height), ref descBottom, TextAnchor.MiddleLeft);

        if (HasAdorningInfo())
        {
            Text.Font = GameFont.Tiny;
            var adorningText = GetAdorningInfo();
            var adorningRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, Text.CalcHeight(adorningText, textWidth));
            GUI.color = TrophyUIStyle.MutedColor;
            Widgets.Label(adorningRect, adorningText);
            GUI.color = Color.white;
            descBottom = adorningRect.yMax;
        }

        var craftsmanship = GetCraftsmanshipLine();
        if (craftsmanship != null)
        {
            Text.Font = GameFont.Tiny;
            var craftsmanshipHeight = Text.CalcHeight(craftsmanship, textWidth);
            var craftsmanshipRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, craftsmanshipHeight);
            // muted as text, not by GUI.color, so the quality word keeps its own color
            Widgets.Label(craftsmanshipRect, craftsmanship.Colorize(TrophyUIStyle.MutedColor));
            descBottom = craftsmanshipRect.yMax;
        }

        // Ceremony quality, verbose only
        var quality = GetCeremonyQualityLine();
        if (quality != null)
        {
            Text.Font = GameFont.Tiny;
            var qualityHeight = Text.CalcHeight(quality, textWidth);
            var qualityRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, qualityHeight);
            // muted as text, not by GUI.color, so the quality word keeps its own color
            Widgets.Label(qualityRect, quality.Colorize(TrophyUIStyle.MutedColor));
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}