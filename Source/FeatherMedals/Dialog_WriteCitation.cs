using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_WriteCitation : Window
{
    private readonly FeatherMedal medal;
    private string draft;
    private const int MAX_LENGTH = 300;
    private const float MEDAL_COLUMN_WIDTH = TrophyUIStyle.TaleDialogFeatherColumnWidth;
    private const float BUTTON_HEIGHT = TrophyUIStyle.ButtonHeight;
    private const float BUTTON_AREA_HEIGHT = TrophyUIStyle.ButtonHeight + TrophyUIStyle.ColumnGap;

    private Vector2 medalScrollPosition;

    public Dialog_WriteCitation(FeatherMedal medal)
    {
        this.medal = medal;
        
        if (MedalMod.Settings.AutoGenerateCitation && !medal.HasCitation)
            this.draft = CitationUtil.GenerateForPlayer() ?? "";
        else
            this.draft = medal.citation ?? "";
        
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
    }

    public override Vector2 InitialSize => new(TrophyUIStyle.TaleDialogWidth, TrophyUIStyle.TabSize.y);

    public override void DoWindowContents(Rect inRect)
    {
        var medalCol = new Rect(inRect.x, inRect.y, MEDAL_COLUMN_WIDTH, inRect.height);

        // Vertical separator
        var separatorX = medalCol.xMax + TrophyUIStyle.ColumnGap;
        UIHelper.DrawSeparatorVertical(separatorX, inRect.y, inRect.height);

        // writing first, so the preview shows the text typed this frame
        var writeCol = new Rect(separatorX + TrophyUIStyle.ColumnGap, inRect.y, inRect.width - MEDAL_COLUMN_WIDTH - TrophyUIStyle.ColumnGap * 3f, inRect.height);
        DrawWritingColumn(writeCol);

        DrawMedalColumn(medalCol);
    }
    
    private void DrawMedalColumn(Rect rect)
    {
        // the trophy as it will be adorned, with the tale being written; scroll instead of overflowing
        var row = new FeatherTrophyDetailDrawer(medal, draft);
        var viewWidth = rect.width - TrophyUIStyle.ScrollbarWidth;
        var viewRect = new Rect(0f, 0f, viewWidth, row.Height(viewWidth));
        Widgets.BeginScrollView(rect, ref medalScrollPosition, viewRect);
        row.Draw(viewRect);
        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawWritingColumn(Rect rect)
    {
        // Header
        Text.Font = GameFont.Medium;
        var headerRect = new Rect(rect.x, rect.y + 10f, rect.width, TrophyUIStyle.HeaderHeight);
        Widgets.Label(headerRect, "TribalTrophies_AdorningCeremony_TaleDialog_Title".Translate());
        Text.Font = GameFont.Small;

        // Instruction
        var instrRect = new Rect(rect.x, headerRect.yMax + 4f, rect.width, 36f);
        GUI.color = TrophyUIStyle.MutedColor;
        Text.Font = GameFont.Tiny;
        Widgets.Label(instrRect, "TribalTrophies_AdorningCeremony_TaleDialog_Desc".Translate());
        GUI.color = Color.white;

        // Text area — fills all space between instructions and the button row
        Text.Font = GameFont.Small;
        var btnY = rect.yMax - BUTTON_AREA_HEIGHT;
        var textRect = new Rect(rect.x, instrRect.yMax + 8f, rect.width, btnY - instrRect.yMax - 8f - 28f);
        draft = Widgets.TextArea(textRect, draft);
        if (draft.Length > MAX_LENGTH)
            draft = draft.Substring(0, MAX_LENGTH);

        // Character count — sits just above the buttons
        var countColor = draft.Length > MAX_LENGTH - 30 ? Color.yellow : TrophyUIStyle.MutedColor;
        GUI.color = countColor;
        Text.Font = GameFont.Tiny;
        var countRect = new Rect(rect.x, textRect.yMax + 4f, rect.width, 20f);
        Text.Anchor = TextAnchor.UpperRight;
        Widgets.Label(countRect, $"{draft.Length} / {MAX_LENGTH}");
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Buttons — anchored to bottom of column
        Text.Font = GameFont.Small;
        var btnWidth = (rect.width - 20f) / 3f;

        if (Widgets.ButtonText(new Rect(rect.x, btnY, btnWidth, BUTTON_HEIGHT), "Confirm".Translate()))
        {
            medal.citation = draft.NullOrEmpty() ? null : draft.Trim();
            Close();
        }

        if (Widgets.ButtonText(new Rect(rect.x + btnWidth + 10f, btnY, btnWidth, BUTTON_HEIGHT), "TribalTrophies_AdorningCeremony_TaleDialog_Suggest".Translate()))
        {
            draft = CitationUtil.GenerateForPlayer() ?? draft;
        }

        if (Widgets.ButtonText(new Rect(rect.x + (btnWidth + 10f) * 2f, btnY, btnWidth, BUTTON_HEIGHT), "TribalTrophies_AdorningCeremony_TaleDialog_Clear".Translate()))
        {
            draft = "";
        }
    }
}