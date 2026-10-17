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

        private enum SettingsTab { Feathers, Scalping, Display }
        private static SettingsTab currentTab = SettingsTab.Feathers;
        private static readonly List<TabRecord> tabBuf = new();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabBarHeight = 32f;
            var contentRect = new Rect(inRect.x, inRect.y + tabBarHeight, inRect.width, inRect.height - tabBarHeight);

            tabBuf.Clear();
            tabBuf.Add(new TabRecord("FeatherMedals_Settings_Section_Feathers".Translate(), () => currentTab = SettingsTab.Feathers, currentTab == SettingsTab.Feathers));
            tabBuf.Add(new TabRecord("FeatherMedals_Settings_Section_Scalping".Translate(), () => currentTab = SettingsTab.Scalping, currentTab == SettingsTab.Scalping));
            tabBuf.Add(new TabRecord("FeatherMedals_Settings_Section_Display".Translate(), () => currentTab = SettingsTab.Display, currentTab == SettingsTab.Display));
            
            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabBuf);

            var inner = contentRect.ContractedBy(12f);
            switch (currentTab)
            {
                case SettingsTab.Feathers: DrawFeathersTab(inner); break;
                case SettingsTab.Scalping: DrawScalpingTab(inner); break;
                case SettingsTab.Display: DrawDisplayTab(inner); break;
            }
        }

        private static void DrawFeathersTab(Rect rect)
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
                 "FeatherMedals_Settings_AutoGenerateCitation".Translate(),
                ref Settings.AutoGenerateCitation,
                 "FeatherMedals_Settings_AutoGenerateCitation_Tooltip".Translate()
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
                // moving one slider past the other pushes the other along
                Settings.NpcTrophyMinTech = DrawTechLevelSlider(listingStandard, "FeatherMedals_Settings_NpcMinTech", Settings.NpcTrophyMinTech);
                if (Settings.NpcTrophyMaxTech < Settings.NpcTrophyMinTech)
                    Settings.NpcTrophyMaxTech = Settings.NpcTrophyMinTech;

                Settings.NpcTrophyMaxTech = DrawTechLevelSlider(listingStandard, "FeatherMedals_Settings_NpcMaxTech", Settings.NpcTrophyMaxTech);
                if (Settings.NpcTrophyMinTech > Settings.NpcTrophyMaxTech)
                    Settings.NpcTrophyMinTech = Settings.NpcTrophyMaxTech;
            }
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.End();
        }

        private static void DrawScalpingTab(Rect rect)
        {
            var listingStandard = new Listing_Standard();
            
            var gapWidth = 12f;
            
            listingStandard.Begin(rect);
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_ScalpUI_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                 "FeatherTrophies_Settings_ShowScalpinGizmo".Translate(),
                ref Settings.ShowScalpingGizmo,
                 "FeatherTrophies_Settings_ShowScalpinGizmo_Tooltip".Translate()
            );
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.Label("FeatherMedals_Settings_Block_Npc_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;
            
            listingStandard.CheckboxLabeled(
                "FeatherTrophies_Settings_GenerateScalpedNpcs".Translate(),
                ref Settings.GenerateNpcScalped,
                "FeatherTrophies_Settings_GenerateScalpedNpcs_Tooltip".Translate()
            );
            
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
            
            listingStandard.GapLine();

            listingStandard.End();
        }

        
        private static TechLevel DrawTechLevelSlider(Listing_Standard listing, string labelKey, TechLevel current)
        {
            var value = listing.SliderLabeled(
                labelKey.Translate(current.ToStringHuman().CapitalizeFirst().Named("LEVEL")),
                (int)current, (int)TechLevel.Animal, (int)TechLevel.Archotech, 0.5f, 
                "FeatherMedals_Settings_NpcTech_Tooltip".Translate()
                );
            return (TechLevel)Mathf.RoundToInt(value);
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

            if (Settings.DrawTrophiesOnPawns)
            {

                listingStandard.Label("FeatherMedals_Settings_Block_Worn_Label".Translate());

                listingStandard.Indent(gapWidth);
                listingStandard.ColumnWidth -= gapWidth;

                Settings.TrophyScale = listingStandard.SliderLabeled(
                    "FeatherMedals_Settings_WornSize".Translate(Settings.TrophyScale.ToStringPercent().Named("SIZE")),
                    Settings.TrophyScale, 0.1f, 2f, 0.5f,
                    "FeatherMedals_Settings_WornSize_Tooltip".Translate());

                Settings.VerticalBias = listingStandard.SliderLabeled(
                    "FeatherMedals_Settings_VerticalBias".Translate(Settings.VerticalBias.ToStringPercent()
                        .Named("BIAS")),
                    Settings.VerticalBias, 0f, 1f, 0.5f,
                    "FeatherMedals_Settings_VerticalBias_Tooltip".Translate());

                Settings.CenterZ = listingStandard.SliderLabeled(
                    "FeatherMedals_Settings_CenterZ".Translate(Settings.CenterZ.ToStringDecimalIfSmall().Named("Z")),
                    Settings.CenterZ, -0.2f, 0.2f, 0.5f,
                    "FeatherMedals_Settings_CenterZ_Tooltip".Translate());

                Settings.MaxDisplayedTrophies = Mathf.RoundToInt(listingStandard.SliderLabeled(
                    "FeatherMedals_Settings_DisplayedTrophies".Translate(
                        Settings.MaxDisplayedTrophies.Named("COUNT")),
                    Settings.MaxDisplayedTrophies, 1f, 20f, 0.5f,
                    "FeatherMedals_Settings_DisplayedTrophies_Tooltip".Translate()));

                listingStandard.Outdent(gapWidth);
                listingStandard.ColumnWidth += gapWidth;

                listingStandard.GapLine();

            }

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
        public bool AutoGenerateCitation = false;
        public float TrophyScale = 1.3f;
        public float VerticalBias = 0.35f;  // 0 = all upright, 1 = fully radial
        public float CenterZ = 0.1f;
        public int MaxDisplayedTrophies = 9;
        public bool GenerateNpcTrophies = true;
        public TechLevel NpcTrophyMinTech = TechLevel.Animal;
        public TechLevel NpcTrophyMaxTech = TechLevel.Medieval;
        public bool GenerateNpcScalped = true;
        public bool ShowScalpingGizmo = true;

        // This method saves and loads the setting
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LockTrophyUponAward, "LockTrophyUponAward", true);
            Scribe_Values.Look(ref DrawTrophiesOnPawns, "DrawTrophiesOnPawns", true);
            Scribe_Values.Look(ref MaxDisplayedTrophies, "MaxDisplayedTrophies", 9);
            Scribe_Values.Look(ref PromptForCitationDuringRitual, "PromptForCitationDuringRitual", true);
            Scribe_Values.Look(ref AutoGenerateCitation, "AutoGenerateCitation", false);
            Scribe_Values.Look(ref TrophyDynamicTraits, "TrophyDynamicTraits", true);
            Scribe_Values.Look(ref TrophyScale, "TrophyScale", 1.3f);
            Scribe_Values.Look(ref VerticalBias, "VerticalBias", 0.35f);
            Scribe_Values.Look(ref CenterZ, "CenterZ", 0.1f);
            Scribe_Values.Look(ref GenerateNpcTrophies, "GenerateNpcTrophies", true);
            Scribe_Values.Look(ref NpcTrophyMinTech, "NpcTrophyMinTech", TechLevel.Animal);
            Scribe_Values.Look(ref NpcTrophyMaxTech, "NpcTrophyMaxTech", TechLevel.Medieval);
            Scribe_Values.Look(ref GenerateNpcScalped, "GenerateNpcScalped", true);
            Scribe_Values.Look(ref ShowScalpingGizmo, "ShowScalpingGizmo", true);
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