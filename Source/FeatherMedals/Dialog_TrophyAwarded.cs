using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_TrophyAwarded : Window
{

    private readonly FeatherMedal medal;
    private readonly Pawn awardee;

    private Vector2 scrollPosition;

    public Dialog_TrophyAwarded(FeatherMedal medal, Pawn awardee)
    {
        this.medal = medal;
        this.awardee = awardee;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => TrophyUIStyle.AwardedDialogSize;

    public override void DoWindowContents(Rect inRect)
    {
        // Title
        var titleRect = new Rect(inRect.x, inRect.y, inRect.width, TrophyUIStyle.HeaderHeight);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(titleRect, "TribalTrophies_AdorningCeremony_AwardedDialog_Title".Translate(awardee.Named("PAWN")));
        Text.Font = GameFont.Small;

        // The trophy just adorned, with who it honors and how the ceremony went; scroll instead of overflowing
        var buttonTop = inRect.yMax - TrophyUIStyle.ButtonHeight;
        var scrollTop = titleRect.yMax + TrophyUIStyle.TabPadding;
        var scrollRect = new Rect(inRect.x, scrollTop, inRect.width, buttonTop - TrophyUIStyle.TabPadding - scrollTop);
        var row = new FeatherTrophyRecordDrawer(medal, verbose: true);
        var viewWidth = scrollRect.width - TrophyUIStyle.ScrollbarWidth;
        var viewRect = new Rect(0f, 0f, viewWidth, row.Height(viewWidth));
        Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
        row.Draw(viewRect);
        Widgets.EndScrollView();

        // Reset and close button
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        var buttonRect = new Rect(inRect.x + (inRect.width - TrophyUIStyle.ButtonWidth) / 2f, buttonTop, TrophyUIStyle.ButtonWidth, TrophyUIStyle.ButtonHeight);
        if (Widgets.ButtonText(buttonRect, "CloseButton".Translate()))
            Close();
    }
}