using UnityEngine;
using Verse;

namespace FeatherMedals;

// One worn/already awarded feather trophy
// with citation
// with trait change that happened
// with ceremony that has already happened related info
public class FeatherTrophyRecordDrawer : ITrophyRecordRow
{
    private readonly FeatherMedal medal;
    private readonly bool verbose;

    // verbose: also who it honors and how the ceremony went, for views of this one trophy
    public FeatherTrophyRecordDrawer(FeatherMedal medal, bool verbose = false)
    {
        this.medal = medal;
        this.verbose = verbose;
    }

    private bool HasAwardInfo() => medal.awardedBy != null || medal.awarded != null;

    private string GetHonoringLine()
    {
        if (!verbose || medal.BiocodeComp is not { Biocoded: true, CodedPawn: not null })
            return null;
        return "TribalTrophies_AdorningCeremony_Common_Honoring".Translate(medal.BiocodeComp.CodedPawn.Named("PAWN"));
    }

    private string GetCeremonyQualityLine()
    {
        if (!verbose || medal.ceremonyQuality < 0)
            return null;
        return "TribalTrophies_AdorningCeremony_Inspect_CeremonyQuality".Translate(
            FeatherTrophyUI.CeremonyQualityLabelColored(medal.ceremonyQuality).Named("QUALITY")).Resolve();
    }

    private string GetAwardInfo()
    {
        var presentedBy = medal.GetAwardedByLabel();
        var date = medal.GetAwardedDateLabel();
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
        height += Text.CalcHeight(medal.MedalLabel, textWidth);

        Text.Font = GameFont.Tiny;
        var honoring = GetHonoringLine();
        if (honoring != null)
            height += Text.CalcHeight(honoring, textWidth) + TrophyUIStyle.RowLineGap;

        if (medal.HasCitation)
            height += FeatherTrophyUI.CitationHeight(medal.citation, textWidth);
        else
            height += TrophyUIStyle.DescriptionLineHeight + TrophyUIStyle.RowLineGap;

        var statText = UIHelper.GetStatSummary(medal.def);
        if (!statText.NullOrEmpty())
            height += Text.CalcHeight(statText, textWidth) + TrophyUIStyle.RowLineGap;

        if (HasAwardInfo())
            height += Text.CalcHeight(GetAwardInfo(), textWidth) + TrophyUIStyle.RowLineGap;

        var quality = GetCeremonyQualityLine();
        if (quality != null)
            height += Text.CalcHeight(quality, textWidth) + TrophyUIStyle.RowLineGap;

        int lineCount = medal.GetTraitChangeText()?.Split('\n').Length ?? 0;
        height += lineCount * TrophyUIStyle.FixedLineHeight;

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
        var wearer = medal.Wearer;
        var tracker = wearer.apparel;
        var locked = tracker.IsLocked(medal);
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
                    tracker.Unlock(medal);
                else
                    tracker.Lock(medal);
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

        // Lock toggle, only while worn; an awarded trophy can also lie on the ground
        if (medal.Wearer != null)
            DrawLockToggle(rowRect);

        // Icon, centered in its column
        var iconSize = TrophyUIStyle.FeatherIconSize;
        var column = new Rect(rowRect.x + TrophyUIStyle.SidePadding, rowRect.y, TrophyUIStyle.IconColumnWidth, rowHeight);
        var iconRect = new Rect(column.center.x - iconSize / 2f, column.center.y - iconSize / 2f, iconSize, iconSize);
        Widgets.ThingIcon(iconRect, medal);

        var textX = column.xMax + TrophyUIStyle.ColumnGap;
        var textWidth = GetTextWidth(rowRect.width);

        // Medal name
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        var nameHeight = Text.CalcHeight(medal.MedalLabel, textWidth);
        var nameRect = new Rect(textX, rowRect.y + TrophyUIStyle.RowPadding, textWidth, nameHeight);
        Widgets.Label(nameRect, medal.MedalLabel);

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

        // Citation
        Text.Font = GameFont.Tiny;
        if (medal.HasCitation)
        {
            FeatherTrophyUI.DrawCitation(medal.citation, nameRect, ref descBottom);
        }
        else
        {
            var descRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, TrophyUIStyle.DescriptionLineHeight);
            GUI.color = TrophyUIStyle.MutedColor;
            Widgets.Label(descRect, (medal.def.description ?? "").Truncate(descRect.width));
            GUI.color = Color.white;
            descBottom = descRect.yMax;
        }

        // Stat bonuses summary
        var statText = UIHelper.GetStatSummary(medal.def);
        if (!statText.NullOrEmpty())
        {
            Text.Font = GameFont.Tiny;
            var statHeight = Text.CalcHeight(statText, textWidth);
            var statsRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, statHeight);
            GUI.color = TrophyUIStyle.GreenColor;
            Widgets.Label(statsRect, statText);
            GUI.color = Color.white;
            descBottom = statsRect.yMax;
        }

        FeatherTrophyUI.DrawTraitChangeSummary(medal, textX, rowRect, ref descBottom);

        // Award info
        if (HasAwardInfo())
        {
            Text.Font = GameFont.Tiny;
            var awardText = GetAwardInfo();
            var awardHeight = Text.CalcHeight(awardText, textWidth);
            var awardRect = new Rect(textX, descBottom + TrophyUIStyle.RowLineGap, textWidth, awardHeight);
            GUI.color = TrophyUIStyle.MutedColor;
            Widgets.Label(awardRect, awardText);
            GUI.color = Color.white;
            descBottom = awardRect.yMax;
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