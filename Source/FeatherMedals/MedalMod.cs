using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace FeatherMedals
{
    [StaticConstructorOnStartup]
    public static class TrophyTextures
    {
        public static readonly Texture2D CitationIcon = ContentFinder<Texture2D>.Get("UI/ButtonWriteTheTale");
        public static readonly Texture2D LockedIcon = ContentFinder<Texture2D>.Get("UI/Locked");
        public static readonly Texture2D UnlockedIcon = ContentFinder<Texture2D>.Get("UI/Unlocked");
    }

    public class MedalMod : Mod
    {
        public static TrophyFeathersModSettings Settings;

        // Constructor runs when the mod is loaded
        public MedalMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TrophyFeathersModSettings>();
        }

        public override string SettingsCategory() => "FeatherMedals_ModName".Translate();

        private enum SettingsTab { General, Display }
        private static SettingsTab currentTab = SettingsTab.General;
        private static readonly List<TabRecord> tabBuf = new();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabBarHeight = 32f;
            var contentRect = new Rect(inRect.x, inRect.y + tabBarHeight, inRect.width, inRect.height - tabBarHeight);

            tabBuf.Clear();
            tabBuf.Add(new TabRecord("FeatherMedals_Settings_Section_General".Translate(), () => currentTab = SettingsTab.General, currentTab == SettingsTab.General));
            tabBuf.Add(new TabRecord("FeatherMedals_Settings_Section_Display".Translate(), () => currentTab = SettingsTab.Display, currentTab == SettingsTab.Display));

            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabBuf);

            var inner = contentRect.ContractedBy(12f);
            switch (currentTab)
            {
                case SettingsTab.General: DrawGeneralTab(inner); break;
                case SettingsTab.Display: DrawDisplayTab(inner); break;
            }
        }

        private static void DrawGeneralTab(Rect rect)
        {
            var listingStandard = new Listing_Standard();
            
            var gapWidth = 12f;
            
            listingStandard.Begin(rect);
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_Ceremony_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                 "FeatherMedals_Settings_LockTrophyUponAward".Translate(),
                ref Settings.LockTrophyUponAward,
                 "FeatherMedals_Settings_LockTrophyUponAward_Tooltip".Translate()
            );
            listingStandard.CheckboxLabeled(
                 "FeatherMedals_Settings_PromptForCitationDuringRitual".Translate(),
                ref Settings.PromptForCitationDuringRitual,
                 "FeatherMedals_Settings_PromptForCitationDuringRitual_Tooltip".Translate()
            );
            listingStandard.CheckboxLabeled(
                 "FeatherMedals_Settings_TrophyDynamicTraits".Translate(),
                ref Settings.TrophyDynamicTraits,
                "FeatherMedals_Settings_TrophyDynamicTraits_Tooltip".Translate()
            );
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_Npc_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "FeatherMedals_Settings_GenerateNpcTrophies".Translate(),
                ref Settings.GenerateNpcTrophies,
                "FeatherMedals_Settings_GenerateNpcTrophies_Tooltip".Translate()
            );
            
            if (Settings.GenerateNpcTrophies)
            {
                DrawTechLevelPicker(listingStandard, "FeatherMedals_Settings_NpcMinTech".Translate(), Settings.NpcTrophyMinTech, level =>
                {
                    Settings.NpcTrophyMinTech = level;
                    if (Settings.NpcTrophyMaxTech < level)
                        Settings.NpcTrophyMaxTech = level;
                });
                DrawTechLevelPicker(listingStandard, "FeatherMedals_Settings_NpcMaxTech".Translate(), Settings.NpcTrophyMaxTech, level =>
                {
                    Settings.NpcTrophyMaxTech = level;
                    if (Settings.NpcTrophyMinTech > level)
                        Settings.NpcTrophyMinTech = level;
                });
            }
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.End();
        }

        private static void DrawTechLevelPicker(Listing_Standard listing, string label, TechLevel current, System.Action<TechLevel> apply)
        {
            if (!listing.ButtonTextLabeled(label, current.ToStringHuman().CapitalizeFirst()))
                return;

            var options = new List<FloatMenuOption>();
            foreach (TechLevel level in System.Enum.GetValues(typeof(TechLevel)))
            {
                if (level == TechLevel.Undefined)
                    continue;
                var chosen = level;
                options.Add(new FloatMenuOption(chosen.ToStringHuman().CapitalizeFirst(), () => apply(chosen)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void DrawDisplayTab(Rect rect)
        {
            var listingStandard = new Listing_Standard();
            
            var gapWidth = 12f;
            
            listingStandard.Begin(rect);
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_Draw_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;
            
            listingStandard.CheckboxLabeled(
                "FeatherMedals_Settings_DrawTrophiesOnPawns".Translate(),
                ref Settings.DrawTrophiesOnPawns,
                "FeatherMedals_Settings_DrawTrophiesOnPawns_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_Worn_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;
            
            Settings.TrophyScale = listingStandard.SliderLabeled(
                "FeatherMedals_Settings_WornSize".Translate(Settings.TrophyScale.ToStringPercent().Named("SIZE")),
                Settings.TrophyScale , 0.1f, 2f, 0.5f,
                "FeatherMedals_Settings_WornSize_Tooltip".Translate());
            
            Settings.MaxDisplayedTrophies = Mathf.RoundToInt(listingStandard.SliderLabeled(
                "FeatherMedals_Settings_DisplayedTrophies".Translate(
                    Settings.MaxDisplayedTrophies.Named("COUNT")),
                Settings.MaxDisplayedTrophies, 1f, 20f, 0.5f,
                "FeatherMedals_Settings_DisplayedTrophies_Tooltip".Translate()));
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.End();
        }
        
    }
    
    public class TrophyFeathersModSettings : ModSettings
    {
        // Default it to true so your intended behavior is the standard
        public bool LockTrophyUponAward = true;
        public bool DrawTrophiesOnPawns = true;
        public bool TrophyDynamicTraits = true;
        public bool PromptForCitationDuringRitual = true;
        public float TrophyScale = 0.8f;
        public int MaxDisplayedTrophies = 9;
        public bool GenerateNpcTrophies = true;
        public TechLevel NpcTrophyMinTech = TechLevel.Animal;
        public TechLevel NpcTrophyMaxTech = TechLevel.Medieval;

        // This method saves and loads the setting
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LockTrophyUponAward, "LockTrophyUponAward", true);
            Scribe_Values.Look(ref DrawTrophiesOnPawns, "DrawTrophiesOnPawns", true);
            Scribe_Values.Look(ref MaxDisplayedTrophies, "MaxDisplayedTrophies", 9);
            Scribe_Values.Look(ref PromptForCitationDuringRitual, "PromptForCitationDuringRitual", true);
            Scribe_Values.Look(ref TrophyDynamicTraits, "TrophyDynamicTraits", true);
            Scribe_Values.Look(ref TrophyScale, "TrophyScale", 0.8f);
            Scribe_Values.Look(ref GenerateNpcTrophies, "GenerateNpcTrophies", true);
            Scribe_Values.Look(ref NpcTrophyMinTech, "NpcTrophyMinTech", TechLevel.Animal);
            Scribe_Values.Look(ref NpcTrophyMaxTech, "NpcTrophyMaxTech", TechLevel.Medieval);
        }
    }

    [StaticConstructorOnStartup]
    public static class MedalModInit
    {
        static MedalModInit()
        {
            var harmony = new HarmonyLib.Harmony("FeatherTrophies");
            harmony.PatchAll();
            Log.Message("[FeatherTrophies] Harmony patches applied successfully.");
        }
    }
    
}