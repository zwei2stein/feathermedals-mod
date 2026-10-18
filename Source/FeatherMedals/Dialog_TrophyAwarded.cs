using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_TrophyAwarded : Window
{

    private readonly FeatherMedal medal;
    private readonly Pawn awardee;

    private const float CLOSE_BUTTON_AREA = 55f;
    private const float SCROLLBAR_WIDTH = 16f;

    private Vector2 scrollPosition;
    private float scrollViewHeight;

    public Dialog_TrophyAwarded(FeatherMedal medal, Pawn awardee)
    {
        this.medal = medal;
        this.awardee = awardee;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(500f, 560f);

    public override void DoWindowContents(Rect inRect)
    {
        var curY = inRect.y;

        // Medal icon, centered
        var iconRect = new Rect(inRect.x + (inRect.width - 128f) / 2f, curY, 128f, 128f);
        Widgets.ThingIcon(iconRect, medal);
        curY += 138f;

        // Medal name
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        var nameHeight = Text.CalcHeight(medal.MedalLabel, inRect.width);
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, nameHeight), medal.MedalLabel);
        curY += nameHeight + 6f;

        // Awarded to
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        var awardLine = "TribalTrophies_AdorningCeremony_Common_Honoring".Translate(awardee.Named("PAWN"));
        var awardHeight = Text.CalcHeight(awardLine, inRect.width);
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, awardHeight), awardLine);
        curY += awardHeight + 4f;

        // Presented by
        Text.Font = GameFont.Tiny;
        var presenterLine = medal.GetAwardedByLabel();
        var presenterHeight = Text.CalcHeight(presenterLine, inRect.width);
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, presenterHeight), presenterLine);
        curY += presenterHeight + 10f;
        Text.Font = GameFont.Small;
        
        var dateStr = medal.GetAwardedDateLabel();
        if (dateStr != null)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            var dateHeight = Text.CalcHeight(dateStr, inRect.width);
            Widgets.Label(new Rect(inRect.x, curY, inRect.width, dateHeight), dateStr);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            curY += dateHeight + 10f;
        }
        else
        {
            curY += 6f;
        }

        // Citation, stats and trait changes can be long, lets make them scrollable instead of obeflowing.
        var scrollRect = new Rect(inRect.x, curY, inRect.width, inRect.yMax - CLOSE_BUTTON_AREA - curY);
        var viewRect = new Rect(0f, 0f, scrollRect.width - SCROLLBAR_WIDTH, scrollViewHeight);
        Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

        var innerY = 0f;
        if (medal.HasCitation)
            UIHelper.DrawCitation(medal.def, medal.citation, viewRect, ref innerY);
        UIHelper.DrawStatSummary(medal.def, viewRect, ref innerY);
        UIHelper.DrawTraitChangeSummary(medal, viewRect, ref innerY, TextAnchor.MiddleCenter);

        // the content height is only known after drawing, the view uses it next frame
        scrollViewHeight = innerY;
        Widgets.EndScrollView();

        // Reset and close button
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        if (Widgets.ButtonText(new Rect(inRect.x + (inRect.width - 120f) / 2f, inRect.yMax - 45f, 120f, 35f), "TribalTrophies_AdorningCeremony_AdornedDialog_Close".Translate()))
            Close();
    }
}