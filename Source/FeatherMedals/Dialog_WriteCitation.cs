using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_WriteCitation : Window
{
    private readonly FeatherMedal medal;
    private string draft;
    private const int MAX_LENGTH = 300;
    private const float MEDAL_COLUMN_WIDTH = 160f;
    private const float ICON_SIZE = 160f;
    private const float BUTTON_HEIGHT = 35f;
    private const float BUTTON_AREA_HEIGHT = BUTTON_HEIGHT + 10f;
    
    private const float SCROLLBAR_WIDTH = 16f;

    private Vector2 medalScrollPosition;
    private float medalScrollViewHeight;

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

    public override Vector2 InitialSize => new(600f, 400f);

    public override void DoWindowContents(Rect inRect)
    {
        var medalCol = new Rect(inRect.x, inRect.y, MEDAL_COLUMN_WIDTH, inRect.height - 45f);
        DrawMedalColumn(medalCol);

        // Vertical separator
        var separatorX = medalCol.xMax + 10f;
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        Widgets.DrawLineVertical(separatorX, inRect.y, inRect.height - 45f);
        GUI.color = Color.white;

        var writeCol = new Rect(separatorX + 10f, inRect.y, inRect.width - MEDAL_COLUMN_WIDTH - 30f, inRect.height);
        DrawWritingColumn(writeCol);
    }
    
    private void DrawMedalColumn(Rect rect)
    {
        // Medal icon
        var iconX = rect.x + (rect.width - ICON_SIZE) / 2f;
        var iconRect = new Rect(iconX, rect.y + 10f, ICON_SIZE, ICON_SIZE);
        Widgets.ThingIcon(iconRect, medal);

        // scroll instead of overflowing
        var scrollRect = new Rect(rect.x, iconRect.yMax + 10f, rect.width, rect.yMax - iconRect.yMax - 10f);
        var viewRect = new Rect(0f, 0f, scrollRect.width - SCROLLBAR_WIDTH, medalScrollViewHeight);
        Widgets.BeginScrollView(scrollRect, ref medalScrollPosition, viewRect);

        var curY = 0f;

        // Medal name
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperCenter;
        var nameHeight = Text.CalcHeight(medal.LabelCap, viewRect.width);
        var nameRect = new Rect(0f, curY, viewRect.width, nameHeight);
        Widgets.Label(nameRect, medal.LabelCap);
        curY = nameRect.yMax + 6f;

        // Medal description
        Text.Font = GameFont.Tiny;
        var desc = medal.def.description ?? "";
        var descHeight = Text.CalcHeight(desc, viewRect.width - 10f);
        var descRect = new Rect(5f, curY, viewRect.width - 10f, descHeight);
        GUI.color = Color.gray;
        Widgets.Label(descRect, desc);
        GUI.color = Color.white;
        curY = descRect.yMax + 10f;

        // Stat bonuses
        var offsets = medal.def.equippedStatOffsets;
        if (offsets is { Count: > 0 })
        {
            GUI.color = UIHelper.GreenColor;
            foreach (var mod in offsets)
            {
                var line = UIHelper.StatOffsetLine(mod);
                var lineHeight = Text.CalcHeight(line, viewRect.width - 10f);
                Widgets.Label(new Rect(5f, curY, viewRect.width - 10f, lineHeight), line);
                curY += lineHeight;
            }
            GUI.color = Color.white;
        }

        medalScrollViewHeight = curY;
        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawWritingColumn(Rect rect)
    {
        // Header
        Text.Font = GameFont.Medium;
        var headerRect = new Rect(rect.x, rect.y + 10f, rect.width, 30f);
        Widgets.Label(headerRect, "TribalTrophies_AdorningCeremony_TaleDialog_Title".Translate());
        Text.Font = GameFont.Small;

        // Instruction
        var instrRect = new Rect(rect.x, headerRect.yMax + 4f, rect.width, 36f);
        GUI.color = Color.gray;
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
        var countColor = draft.Length > MAX_LENGTH - 30 ? Color.yellow : Color.gray;
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

        if (Widgets.ButtonText(new Rect(rect.x, btnY, btnWidth, BUTTON_HEIGHT), "TribalTrophies_AdorningCeremony_TaleDialog_Confirm".Translate()))
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