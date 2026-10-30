using System.Linq;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// One worn/already adorned feather trophy
// with tale
// with trait change that happened
// with ceremony that has already happened related info
public class FeatherTrophyRecordDrawer : TrophyRowDrawer
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

    protected override Thing IconThing => _trophy;

    protected override float IconSize => TrophyUIStyle.FeatherIconSize;

    protected override float ReservedRightWidth => TrophyUIStyle.ColumnGap + TrophyUIStyle.LockButtonSize;

    protected override float MinHeight => TrophyUIStyle.FeatherRowMinHeight;

    protected override bool CenterText => false;

    protected override float LayoutText(Rect area, bool draw)
    {
        var curY = area.y;

        // each line below counts the gap drawn above it; the tale is drawn right under the line before it

        // Feather trophy name
        UIHelper.LayoutLine(_trophy.TrophyLabel, GameFont.Small, Color.white, TextAnchor.MiddleLeft, area, ref curY, draw);

        // Honoring, verbose only
        var honoring = GetHonoringLine();
        if (honoring != null)
            UIHelper.LayoutLine(honoring, GameFont.Tiny, Color.white, TextAnchor.MiddleLeft, area, ref curY, draw, TrophyUIStyle.RowLineGap);

        Text.Font = GameFont.Tiny;
        if (_trophy.HasTale)
        {
            if (draw)
                FeatherTrophyUI.DrawTale(_trophy.tale, area, ref curY);
            else
                curY += FeatherTrophyUI.TaleHeight(_trophy.tale, area.width);
        }
        else
        {
            curY += TrophyUIStyle.RowLineGap;
            if (draw)
            {
                var descRect = new Rect(area.x, curY, area.width, TrophyUIStyle.DescriptionLineHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = TrophyUIStyle.MutedColor;
                Widgets.Label(descRect, (_trophy.def.description ?? "").Truncate(descRect.width));
                GUI.color = Color.white;
            }
            curY += TrophyUIStyle.DescriptionLineHeight;
        }

        // Stat bonuses summary
        var statLines = UIHelper.StatOffsetLines(_trophy.def).ToList();
        if (statLines.Count > 0)
        {
            curY += TrophyUIStyle.RowLineGap;
            if (draw)
                UIHelper.DrawCompactLines(statLines, TrophyUIStyle.GreenColor, TextAnchor.MiddleLeft, area, ref curY);
            else
                curY += UIHelper.CompactLinesHeight(statLines, area.width);
        }

        if (draw)
            FeatherTrophyUI.DrawTraitChangeSummary(_trophy, area, ref curY, TextAnchor.MiddleLeft);
        else
            curY += FeatherTrophyUI.TraitChangeSummaryHeight(_trophy, area.width);

        if (HasAdorningInfo())
        {
            Text.Font = GameFont.Tiny;
            var adorningText = GetAdorningInfo();
            curY += TrophyUIStyle.RowLineGap;
            var adorningHeight = Text.CalcHeight(adorningText, area.width);
            if (draw)
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = TrophyUIStyle.MutedColor;
                Widgets.Label(new Rect(area.x, curY, area.width, adorningHeight), adorningText);
                GUI.color = Color.white;
            }
            curY += adorningHeight;
        }

        // muted as text, not by GUI.color, so the quality word keeps its own color
        var craftsmanship = GetCraftsmanshipLine();
        if (craftsmanship != null)
            UIHelper.LayoutLine(craftsmanship, GameFont.Tiny, TrophyUIStyle.MutedColor, TextAnchor.MiddleLeft, area, ref curY, draw, TrophyUIStyle.RowLineGap);

        // Ceremony quality, verbose only
        // muted as text, not by GUI.color, so the quality word keeps its own color
        var quality = GetCeremonyQualityLine();
        if (quality != null)
            UIHelper.LayoutLine(quality, GameFont.Tiny, TrophyUIStyle.MutedColor, TextAnchor.MiddleLeft, area, ref curY, draw, TrophyUIStyle.RowLineGap);

        Text.Font = GameFont.Small;
        return curY - area.y;
    }

    protected override void DrawExtras(Rect rowRect)
    {
        if (_trophy.Wearer != null)
            DrawLockToggle(rowRect);
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
}