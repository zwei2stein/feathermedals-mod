using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    [StaticConstructorOnStartup]
    public static class InjectThophyTab
    {
        static InjectThophyTab()
        {
            var tabType = typeof(ITab_PawnTrophies);
            var tabInstance = InspectTabManager.GetSharedInstance(tabType);
            var humanDef = ThingDef.Named("Human");
            var corpseDef = ThingDef.Named("Corpse_Human");
            InjectIntoDef(humanDef, tabType, tabInstance);
            InjectIntoDef(corpseDef, tabType, tabInstance);
        }

        private static void InjectIntoDef(ThingDef def, Type tabType, InspectTabBase tabInstance)
        {
            if (def == null) return;
            if (def.inspectorTabs == null)
                def.inspectorTabs = new List<Type>();
            if (!def.inspectorTabs.Contains(tabType))
                def.inspectorTabs.Add(tabType);
            if (def.inspectorTabsResolved == null)
                def.inspectorTabsResolved = new List<InspectTabBase>();
            if (!def.inspectorTabsResolved.Contains(tabInstance))
                def.inspectorTabsResolved.Add(tabInstance);
        }
    }
    
    public class ITab_PawnTrophies : ITab
    {
        private Vector2 _scrollPosition;
        private const float MEDAL_ROW_HEIGHT = 90f;
        private const float ICON_SIZE = 80f;
        private const float PADDING = 10f;
        private const float TAB_WIDTH = 400f;
        private const float TAB_HEIGHT = 480f;

        public ITab_PawnTrophies()
        {
            labelKey = "FeatherMedals_TrophiesTab";
            size = new(TAB_WIDTH, TAB_HEIGHT);
        }

        public override bool IsVisible => HasTrophies(SelPawnForGear);

        private Pawn SelPawnForGear
        {
            get
            {
                return SelThing switch
                {
                    Pawn p => p,
                    Corpse corpse => corpse.InnerPawn,
                    _ => null
                };
            }
        }
        
        private bool HasAwardInfo(FeatherMedal medal) => medal.awardedBy != null || medal.awardedTick >= 0;

        private string GetAwardInfo(FeatherMedal medal)
        {
            var sb = new StringBuilder();
            if (medal.awardedBy != null)
            {
                sb.Append(medal.GetAwardedByLabel());
            }
            if (medal.awardedTick >= 0)
            {
                if (sb.Length > 0) sb.Append(" on ");
                sb.Append(GenDate.DateFullStringAt(
                    GenDate.TickGameToAbs(medal.awardedTick),
                    Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile)
                ));
            }
            return sb.ToString();
        }

        private bool HasTrophies(Pawn pawn)
        {
            if (pawn?.apparel == null) return false;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel is FeatherMedal)
                    return true;
            }
            return false;
        }

        protected override void FillTab()
        {
            var pawn = SelPawnForGear;
            if (pawn == null) return;

            var outerRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(PADDING);

            var headerRect = new Rect(outerRect.x, outerRect.y, outerRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "FeatherMedals_TrophiesTab".Translate());
            Text.Font = GameFont.Small;

            if (!HasTrophies(pawn))
            {
                var emptyRect = new Rect(outerRect.x, headerRect.yMax + PADDING, outerRect.width, 40f);
                GUI.color = Color.gray;
                Widgets.Label(emptyRect, "FeatherMedals_NoTrophies".Translate());
                GUI.color = Color.white;
                return;
            }

            var listRect = new Rect(outerRect.x, headerRect.yMax + PADDING, outerRect.width, outerRect.height - 30f - PADDING);
            var viewWidth = listRect.width - 16f; // scrollbar

            var totalHeight = 0f;
            foreach (var apparel in pawn.apparel.WornApparel)
                if (apparel is FeatherMedal medal)
                    totalHeight += GetRowHeight(medal, viewWidth) + PADDING;
            var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

            Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);

            var curY = 0f;
            foreach (var apparel in pawn.apparel.WornApparel)
                if (apparel is FeatherMedal medal)
                    DrawMedalRow(viewWidth, ref curY, medal);

            Widgets.EndScrollView();
        }

        private float GetRowHeight(FeatherMedal medal, float width)
        {
            var textWidth = GetTextWidth(width);
            var height = 4f;

            Text.Font = GameFont.Small;
            height += Text.CalcHeight(medal.MedalLabel, textWidth) + 2f;

            Text.Font = GameFont.Tiny;
            if (!medal.citation.NullOrEmpty())
            {
                height += Text.CalcHeight($"\"{medal.citation}\"", textWidth) + 2f;
            }
            else
            {
                height += 20f + 2f;
            }

            var statText = UIHelper.GetStatSummary(medal.def);
            if (!statText.NullOrEmpty())
            {
                height += Text.CalcHeight(statText, textWidth);
            }
            
            if (HasAwardInfo(medal))
            {
                height += Text.CalcHeight(GetAwardInfo(medal), textWidth) + 2f;
            }
            
            var ext = medal.def.GetModExtension<ThrophyExtension>();
            var honor = ext?.honorAwarded ?? 0;
            if (honor > 0)
                height += Text.CalcHeight("0", textWidth) + 2f;

            Text.Font = GameFont.Small;
            height += 8f;
            return Mathf.Max(MEDAL_ROW_HEIGHT, height);
        }
        
        private float GetTextWidth(float availableWidth)
        {
            return availableWidth - ICON_SIZE - (PADDING * 3) - LOCK_BTN_SIZE - PADDING;
        }

        private const float LOCK_BTN_SIZE = 24f;

        private void DrawMedalRow(float width, ref float curY, FeatherMedal medal)
        {
            var rowHeight = GetRowHeight(medal, width);
            var rowRect = new Rect(0f, curY, width, rowHeight);

            if (Mouse.IsOver(rowRect))
                Widgets.DrawHighlight(rowRect);

            // Lock toggle
            var lockRect = new Rect(
                rowRect.xMax - LOCK_BTN_SIZE - PADDING,
                rowRect.y + 4f,
                LOCK_BTN_SIZE,
                LOCK_BTN_SIZE
            );
            var lockIcon = medal.isLocked ?  TrophyTextures.LockedIcon : TrophyTextures.UnlockedIcon;
            var lockTip = medal.isLocked
                ? "FeatherMedals_TrophyLocked".Translate()
                : "FeatherMedals_TrophyUnlocked".Translate();

            GUI.color = Color.gray;
            if (Widgets.ButtonImage(lockRect, lockIcon, GUI.color))
                medal.isLocked = !medal.isLocked;
            GUI.color = Color.white;
            TooltipHandler.TipRegion(lockRect, lockTip);

            // Icon
            var iconRect = new Rect(rowRect.x + PADDING, rowRect.y + (rowHeight - ICON_SIZE) / 2f, ICON_SIZE, ICON_SIZE);
            Widgets.ThingIcon(iconRect, medal);

            var textX = iconRect.xMax + PADDING;
            var textWidth = GetTextWidth(width);

            // Medal name
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            var nameHeight = Text.CalcHeight(medal.MedalLabel, textWidth);
            var nameRect = new Rect(textX, rowRect.y + 4f, textWidth, nameHeight);
            Widgets.Label(nameRect, medal.MedalLabel);

            // Citation
            Text.Font = GameFont.Tiny;
            float descBottom = nameRect.yMax;
            if (!medal.citation.NullOrEmpty())
            {
                UIHelper.DrawCitation(medal.def, medal.citation, nameRect, ref descBottom);
            }
            else
            {
                var descRect = new Rect(textX, nameRect.yMax + 2f, textWidth, 20f);
                GUI.color = Color.gray;
                Widgets.Label(descRect, (medal.def.description ?? "").Truncate(descRect.width));
                GUI.color = Color.white;
                descBottom = descRect.yMax;
            }
            
            var statsBottom = descBottom;
            var ext = medal.def.GetModExtension<ThrophyExtension>();
            var honor = ext?.honorAwarded ?? 0;
            if (ModsConfig.RoyaltyActive && honor > 0)
            {
                var rect = new Rect(textX, descBottom + 4f, nameRect.xMax, 14f);
                UIHelper.DrawHonorSummary(medal.def, rect, ref statsBottom);
            }

            // Stat bonuses summary
            var statText = UIHelper.GetStatSummary(medal.def);
            if (!statText.NullOrEmpty())
            {
                Text.Font = GameFont.Tiny;
                var statHeight = Text.CalcHeight(statText, textWidth);
                var statsRect = new Rect(textX, statsBottom + 2f, textWidth, statHeight);
                GUI.color = new Color(0.5f, 0.8f, 0.5f);
                Widgets.Label(statsRect, statText);
                GUI.color = Color.white;
                statsBottom = statsRect.yMax;
            }
            
            // Award info
            if (HasAwardInfo(medal))
            {
                Text.Font = GameFont.Tiny;
                var awardText = GetAwardInfo(medal);
                var awardHeight = Text.CalcHeight(awardText, textWidth);
                var awardRect = new Rect(textX, statsBottom + 2f, textWidth, awardHeight);
                GUI.color = Color.gray;
                Widgets.Label(awardRect, awardText);
                GUI.color = Color.white;
            }
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            curY += rowHeight + PADDING;
        }
        
    }
}