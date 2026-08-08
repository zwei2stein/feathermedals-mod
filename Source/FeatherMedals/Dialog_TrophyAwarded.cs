using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_TrophyAwarded : Window
{

    private readonly FeatherMedal medal;
    private readonly Pawn awardee;
    private readonly Pawn presenter;

    public Dialog_TrophyAwarded(FeatherMedal medal, Pawn awardee, Pawn presenter)
    {
        this.medal = medal;
        this.awardee = awardee;
        this.presenter = presenter;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(500f, 500f);

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
        var awardLine = "FeatherMedals_Inspector_AwardedTo".Translate(awardee.Named("PAWN"));
        var awardHeight = Text.CalcHeight(awardLine, inRect.width);
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, awardHeight), awardLine);
        curY += awardHeight + 4f;

        // Presented by
        Text.Font = GameFont.Tiny;
        var presenterLine = "FeatherMedals_Inspector_PresentedBy".Translate(presenter.Named("PAWN"));
        var presenterHeight = Text.CalcHeight(presenterLine, inRect.width);
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, presenterHeight), presenterLine);
        curY += presenterHeight + 10f;
        Text.Font = GameFont.Small;
        
        if (medal.awardedTick >= 0)
        {
            var tile = awardee.Map?.Tile ?? Find.CurrentMap?.Tile ?? 0;
            var dateStr = GenDate.DateFullStringAt(
                GenDate.TickGameToAbs(medal.awardedTick),
                Find.WorldGrid.LongLatOf(tile));
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

        // Citation
        if (!medal.citation.NullOrEmpty())
        {
            UIHelper.DrawCitation(medal.def, medal.citation, inRect, ref curY);
        }
        
        
        var ext = medal.def.GetModExtension<TrophyExtension>();
        var honor = ext?.honorAwarded ?? 0;
        if (ModsConfig.RoyaltyActive && honor > 0)
        {
            UIHelper.DrawHonorSummary(medal.def, inRect, ref curY);
        }
        
        // Stat bonuses
        UIHelper.DrawStatSummary(medal.def, inRect, ref curY);
        
        UIHelper.DrawTraitChangeSummary(medal, inRect, ref curY);

        // Reset and close button
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        if (Widgets.ButtonText(new Rect(inRect.x + (inRect.width - 120f) / 2f, inRect.yMax - 45f, 120f, 35f), "FeatherMedals_CloseButton".Translate()))
            Close();
    }
}