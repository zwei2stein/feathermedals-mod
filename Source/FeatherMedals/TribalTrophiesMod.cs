using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace TribalTrophies
{
    [StaticConstructorOnStartup]
    public static class TrophyTextures
    {
        public static readonly Texture2D TaleIcon = ContentFinder<Texture2D>.Get("UI/ButtonWriteTheTale");
        public static readonly Texture2D LockedIcon = ContentFinder<Texture2D>.Get("UI/Locked");
        public static readonly Texture2D UnlockedIcon = ContentFinder<Texture2D>.Get("UI/Unlocked");
        public static readonly Texture2D DisassembleIcon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct");
    }

    public class TribalTrophiesMod : Mod
    {
        public static TribalTrophiesSettings Settings;

        // Constructor runs when the mod is loaded
        public TribalTrophiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TribalTrophiesSettings>();
        }

        public override string SettingsCategory() => "TribalTrophies_ModName".Translate();

        private enum SettingsTab { Trophies, Scalping, Display }
        private static SettingsTab currentTab = SettingsTab.Trophies;
        private static readonly List<TabRecord> tabBuf = new();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabBarHeight = 32f;
            var contentRect = new Rect(inRect.x, inRect.y + tabBarHeight, inRect.width, inRect.height - tabBarHeight);

            tabBuf.Clear();
            tabBuf.Add(new TabRecord("TribalTrophies_Settings_Trophies_Label".Translate(), () => currentTab = SettingsTab.Trophies, currentTab == SettingsTab.Trophies));
            tabBuf.Add(new TabRecord("TribalTrophies_Settings_Scalping_Label".Translate(), () => currentTab = SettingsTab.Scalping, currentTab == SettingsTab.Scalping));
            tabBuf.Add(new TabRecord("TribalTrophies_Settings_Display_Label".Translate(), () => currentTab = SettingsTab.Display, currentTab == SettingsTab.Display));

            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabBuf);

            var inner = contentRect.ContractedBy(12f);
            switch (currentTab)
            {
                case SettingsTab.Trophies: DrawFeathersTab(inner); break;
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

            listingStandard.Label("TribalTrophies_Settings_Trophies_Ceremony_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                 "TribalTrophies_Settings_Trophies_Ceremony_LockTrophyUponAdorning_Label".Translate(),
                ref Settings.LockTrophyUponAdorning,
                 "TribalTrophies_Settings_Trophies_Ceremony_LockTrophyUponAdorning_Tooltip".Translate()
            );
            listingStandard.CheckboxLabeled(
                 "TribalTrophies_Settings_Trophies_Ceremony_PromptForTaleDuringCeremony_Label".Translate(),
                ref Settings.PromptForTaleDuringCeremony,
                 "TribalTrophies_Settings_Trophies_Ceremony_PromptForTaleDuringCeremony_Tooltip".Translate()
            );
            if (Settings.PromptForTaleDuringCeremony)
            {
                listingStandard.CheckboxLabeled(
                    "TribalTrophies_Settings_Trophies_Ceremony_AutoSuggestTale_Label".Translate(),
                    ref Settings.AutoSuggestTale,
                    "TribalTrophies_Settings_Trophies_Ceremony_AutoSuggestTale_Tooltip".Translate()
                );
            }
            listingStandard.CheckboxLabeled(
                 "TribalTrophies_Settings_Trophies_Ceremony_TrophyDynamicTraits_Label".Translate(),
                ref Settings.TrophyDynamicTraits,
                "TribalTrophies_Settings_Trophies_Ceremony_TrophyDynamicTraits_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();

            listingStandard.Label("TribalTrophies_Settings_Trophies_Npc_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Trophies_Npc_GenerateNpcTrophies_Label".Translate(),
                ref Settings.GenerateNpcTrophies,
                "TribalTrophies_Settings_Trophies_Npc_GenerateNpcTrophies_Tooltip".Translate()
            );

            if (Settings.GenerateNpcTrophies)
            {
                // moving one slider past the other pushes the other along
                Settings.NpcTrophyMinTech = DrawTechLevelSlider(listingStandard,
                    "TribalTrophies_Settings_Trophies_Npc_NpcTrophyMinTech_Label",
                    "TribalTrophies_Settings_Trophies_Npc_NpcTrophyMinTech_Tooltip",
                    Settings.NpcTrophyMinTech);
                if (Settings.NpcTrophyMaxTech < Settings.NpcTrophyMinTech)
                    Settings.NpcTrophyMaxTech = Settings.NpcTrophyMinTech;

                Settings.NpcTrophyMaxTech = DrawTechLevelSlider(listingStandard,
                    "TribalTrophies_Settings_Trophies_Npc_NpcTrophyMaxTech_Label",
                    "TribalTrophies_Settings_Trophies_Npc_NpcTrophyMaxTech_Tooltip",
                    Settings.NpcTrophyMaxTech);
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

            listingStandard.Label("TribalTrophies_Settings_Scalping_Ui_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                 "TribalTrophies_Settings_Scalping_Ui_ShowScalpingGizmo_Label".Translate(),
                ref Settings.ShowScalpingGizmo,
                 "TribalTrophies_Settings_Scalping_Ui_ShowScalpingGizmo_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();

            listingStandard.Label("TribalTrophies_Settings_Scalping_MentalBreaks_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_MentalBreaks_ScalpBreaksTargetChildren_Label".Translate(),
                ref Settings.ScalpBreaksTargetChildren,
                "TribalTrophies_Settings_Scalping_MentalBreaks_ScalpBreaksTargetChildren_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();

            listingStandard.Label("TribalTrophies_Settings_Scalping_Npc_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcScalped_Label".Translate(),
                ref Settings.GenerateNpcScalped,
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcScalped_Tooltip".Translate(
                    NpcScalpGenerator.Tuning.chance.ToStringPercent().Named("BASECHANCE"),
                    NpcScalpGenerator.Tuning.zeroChanceTechLevel.ToStringHuman().Named("ZEROTECH")
                    )
            );

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcScalpBelts_Label".Translate(),
                ref Settings.GenerateNpcScalpBelts,
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcScalpBelts_Tooltip".Translate()
            );

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcSkullBelts_Label".Translate(),
                ref Settings.GenerateNpcSkullBelts,
                "TribalTrophies_Settings_Scalping_Npc_GenerateNpcSkullBelts_Tooltip".Translate()
            );
            
            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_Npc_ScalpPolesInBases_Label".Translate(),
                ref Settings.ScalpPolesInBases,
                "TribalTrophies_Settings_Scalping_Npc_ScalpPolesInBases_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();
            
            listingStandard.Label("TribalTrophies_Settings_Scalping_ScalpHunts_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_ScalpHunts_Enabled_Label".Translate(),
                ref Settings.ScalpHuntRaids,
                "TribalTrophies_Settings_Scalping_ScalpHunts_Enabled_Tooltip".Translate()
            );

            if (Settings.ScalpHuntRaids)
            {
                Settings.ScalpHuntMaxTech = DrawTechLevelSlider(listingStandard,
                    "TribalTrophies_Settings_Scalping_ScalpHunts_MaxTech_Label",
                    "TribalTrophies_Settings_Scalping_ScalpHunts_MaxTech_Tooltip",
                    Settings.ScalpHuntMaxTech);

                listingStandard.CheckboxLabeled(
                    "TribalTrophies_Settings_Scalping_ScalpHunts_DropPods_Label".Translate(),
                    ref Settings.ScalpHuntDropPods,
                    "TribalTrophies_Settings_Scalping_ScalpHunts_DropPods_Tooltip".Translate()
                );
            }

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();
            
            listingStandard.Label("TribalTrophies_Settings_Scalping_ScalpRequests_Label".Translate());
 
            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;
 
            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Scalping_ScalpRequests_Enabled_Label".Translate(),
                ref Settings.ScalpRequestQuests,
                "TribalTrophies_Settings_Scalping_ScalpRequests_Enabled_Tooltip".Translate()
            );
 
            if (Settings.ScalpRequestQuests)
            {
                Settings.ScalpRequestMaxTech = DrawTechLevelSlider(listingStandard,
                    "TribalTrophies_Settings_Scalping_ScalpRequests_MaxTech_Label",
                    "TribalTrophies_Settings_Scalping_ScalpRequests_MaxTech_Tooltip",
                    Settings.ScalpRequestMaxTech);
 
                Settings.ScalpRequestMaxDistance = Mathf.RoundToInt(listingStandard.SliderLabeled(
                    "TribalTrophies_Settings_Scalping_ScalpRequests_MaxDistance_Label".Translate(
                        Settings.ScalpRequestMaxDistance.Named("TILES")),
                    Settings.ScalpRequestMaxDistance, 5f, 100f, 0.5f,
                    "TribalTrophies_Settings_Scalping_ScalpRequests_MaxDistance_Tooltip".Translate()));
            }
 
            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;
 
            listingStandard.GapLine();

            listingStandard.End();
        }


        private static TechLevel DrawTechLevelSlider(Listing_Standard listing, string labelKey, string tooltipKey, TechLevel current)
        {
            var value = listing.SliderLabeled(
                labelKey.Translate(current.ToStringHuman().CapitalizeFirst().Named("LEVEL")),
                (int)current, (int)TechLevel.Animal, (int)TechLevel.Archotech, 0.5f,
                tooltipKey.Translate()
                );
            return (TechLevel)Mathf.RoundToInt(value);
        }

        private static void DrawDisplayTab(Rect rect)
        {
            var listingStandard = new Listing_Standard();

            var gapWidth = 12f;

            listingStandard.Begin(rect);

            listingStandard.GapLine();

            listingStandard.Label("TribalTrophies_Settings_Display_Rendering_Label".Translate());

            listingStandard.Indent(gapWidth);
            listingStandard.ColumnWidth -= gapWidth;

            listingStandard.CheckboxLabeled(
                "TribalTrophies_Settings_Display_Rendering_DrawTrophiesOnPawns_Label".Translate(),
                ref Settings.DrawTrophiesOnPawns,
                "TribalTrophies_Settings_Display_Rendering_DrawTrophiesOnPawns_Tooltip".Translate()
            );

            listingStandard.Outdent(gapWidth);
            listingStandard.ColumnWidth += gapWidth;

            listingStandard.GapLine();

            if (Settings.DrawTrophiesOnPawns)
            {

                listingStandard.Label("TribalTrophies_Settings_Display_Worn_Label".Translate());

                listingStandard.Indent(gapWidth);
                listingStandard.ColumnWidth -= gapWidth;

                Settings.TrophyScale = listingStandard.SliderLabeled(
                    "TribalTrophies_Settings_Display_Worn_TrophyScale_Label".Translate(Settings.TrophyScale.ToStringPercent().Named("SIZE")),
                    Settings.TrophyScale, 0.1f, 2f, 0.5f,
                    "TribalTrophies_Settings_Display_Worn_TrophyScale_Tooltip".Translate());

                Settings.FanOut = listingStandard.SliderLabeled(
                    "TribalTrophies_Settings_Display_Worn_FanOut_Label".Translate(Settings.FanOut.ToStringPercent()
                        .Named("FANOUT")),
                    Settings.FanOut, 0f, 1f, 0.5f,
                    "TribalTrophies_Settings_Display_Worn_FanOut_Tooltip".Translate());

                Settings.VerticalOffset = listingStandard.SliderLabeled(
                    "TribalTrophies_Settings_Display_Worn_VerticalOffset_Label".Translate(Settings.VerticalOffset.ToStringDecimalIfSmall().Named("OFFSET")),
                    Settings.VerticalOffset, -0.2f, 0.2f, 0.5f,
                    "TribalTrophies_Settings_Display_Worn_VerticalOffset_Tooltip".Translate());

                Settings.MaxDisplayedTrophies = Mathf.RoundToInt(listingStandard.SliderLabeled(
                    "TribalTrophies_Settings_Display_Worn_MaxDisplayedTrophies_Label".Translate(
                        Settings.MaxDisplayedTrophies.Named("COUNT")),
                    Settings.MaxDisplayedTrophies, 1f, 20f, 0.5f,
                    "TribalTrophies_Settings_Display_Worn_MaxDisplayedTrophies_Tooltip".Translate()));

                listingStandard.Outdent(gapWidth);
                listingStandard.ColumnWidth += gapWidth;

                listingStandard.GapLine();

            }

            listingStandard.End();
        }

    }

    public class TribalTrophiesSettings : ModSettings
    {
        // Default it to true so your intended behavior is the standard
        public bool LockTrophyUponAdorning = true;
        public bool DrawTrophiesOnPawns = true;
        public bool TrophyDynamicTraits = true;
        public bool PromptForTaleDuringCeremony = true;
        public bool AutoSuggestTale = false;
        public float TrophyScale = 1.3f;
        public float FanOut = 0.35f;  // 0 = all upright, 1 = fully radial
        public float VerticalOffset = 0.1f;
        public int MaxDisplayedTrophies = 9;
        public bool GenerateNpcTrophies = true;
        public TechLevel NpcTrophyMinTech = TechLevel.Animal;
        public TechLevel NpcTrophyMaxTech = TechLevel.Medieval;
        public bool GenerateNpcScalped = true;
        public bool GenerateNpcScalpBelts = true;
        public bool GenerateNpcSkullBelts = true;
        public bool ShowScalpingGizmo = true;
        public bool ScalpBreaksTargetChildren = false;
        public bool ScalpHuntRaids = true;
        public TechLevel ScalpHuntMaxTech = TechLevel.Industrial;
        public bool ScalpHuntDropPods = true;
        public bool ScalpRequestQuests = true;
        public TechLevel ScalpRequestMaxTech = TechLevel.Industrial;
        public int ScalpRequestMaxDistance = 32;

        public bool ScalpPolesInBases = true;

        // This method saves and loads the setting
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LockTrophyUponAdorning, "LockTrophyUponAdorning", true);
            Scribe_Values.Look(ref DrawTrophiesOnPawns, "DrawTrophiesOnPawns", true);
            Scribe_Values.Look(ref MaxDisplayedTrophies, "MaxDisplayedTrophies", 9);
            Scribe_Values.Look(ref PromptForTaleDuringCeremony, "PromptForTaleDuringCeremony", true);
            Scribe_Values.Look(ref AutoSuggestTale, "AutoSuggestTale", false);
            Scribe_Values.Look(ref TrophyDynamicTraits, "TrophyDynamicTraits", true);
            Scribe_Values.Look(ref TrophyScale, "TrophyScale", 1.3f);
            Scribe_Values.Look(ref FanOut, "FanOut", 0.35f);
            Scribe_Values.Look(ref VerticalOffset, "VerticalOffset", 0.1f);
            Scribe_Values.Look(ref GenerateNpcTrophies, "GenerateNpcTrophies", true);
            Scribe_Values.Look(ref NpcTrophyMinTech, "NpcTrophyMinTech", TechLevel.Animal);
            Scribe_Values.Look(ref NpcTrophyMaxTech, "NpcTrophyMaxTech", TechLevel.Medieval);
            Scribe_Values.Look(ref GenerateNpcScalped, "GenerateNpcScalped", true);
            Scribe_Values.Look(ref GenerateNpcScalpBelts, "GenerateNpcScalpBelts", true);
            Scribe_Values.Look(ref GenerateNpcSkullBelts, "GenerateNpcSkullBelts", true);
            Scribe_Values.Look(ref ShowScalpingGizmo, "ShowScalpingGizmo", true);
            Scribe_Values.Look(ref ScalpBreaksTargetChildren, "ScalpBreaksTargetChildren", false);
            Scribe_Values.Look(ref ScalpHuntRaids, "ScalpHuntRaids", true);
            Scribe_Values.Look(ref ScalpHuntMaxTech, "ScalpHuntMaxTech", TechLevel.Industrial);
            Scribe_Values.Look(ref ScalpHuntDropPods, "ScalpHuntDropPods", true);
            Scribe_Values.Look(ref ScalpRequestQuests, "ScalpRequestQuests", true);
            Scribe_Values.Look(ref ScalpRequestMaxTech, "ScalpRequestMaxTech", TechLevel.Industrial);
            Scribe_Values.Look(ref ScalpRequestMaxDistance, "ScalpRequestMaxDistance", 32);
            Scribe_Values.Look(ref ScalpPolesInBases, "ScalpPolesInBases", true);
        }
    }

    [StaticConstructorOnStartup]
    public static class TribalTrophiesModInit
    {
        static TribalTrophiesModInit()
        {
            var harmony = new HarmonyLib.Harmony("TribalTrophies");
            harmony.PatchAll();

            if (Prefs.DevMode)
                Log.Message("[TribalTrophies] Harmony patches applied successfully.");
        }
    }

}