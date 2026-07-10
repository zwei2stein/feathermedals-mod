using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI.Group;

namespace FeatherMedals
{
    [StaticConstructorOnStartup]
    public static class TrophyTextures
    {
        public static readonly Texture2D CitationIcon = ContentFinder<Texture2D>.Get("UI/ButtonWriteTheTale");
        public static readonly Texture2D LockedIcon = ContentFinder<Texture2D>.Get("UI/Locked");
        public static readonly Texture2D UnlockedIcon = ContentFinder<Texture2D>.Get("UI/Unlocked");
        public static readonly Texture2D HonorIcon = ContentFinder<Texture2D>.Get("UI/Icons/RoyalFavor");
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
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "FeatherMedals_Settings_TrophiesRequireCeremony".Translate(),
                ref Settings.TrophiesRequireCeremony,
                 "FeatherMedals_Settings_TrophiesRequireCeremony_Tooltip".Translate()
            );
            listing.CheckboxLabeled(
                 "FeatherMedals_Settings_LockTrophyUponAward".Translate(),
                ref Settings.LockTrophyUponAward,
                 "FeatherMedals_Settings_LockTrophyUponAward_Tooltip".Translate()
            );
            listing.CheckboxLabeled(
                 "FeatherMedals_Settings_PromptForCitationDuringRitual".Translate(),
                ref Settings.PromptForCitationDuringRitual,
                 "FeatherMedals_Settings_PromptForCitationDuringRitual_Tooltip".Translate()
            );
            listing.CheckboxLabeled(
                 "FeatherMedals_Settings_TrophyDynamicTraits".Translate(),
                ref Settings.TrophyDynamicTraits,
                "FeatherMedals_Settings_TrophyDynamicTraits_Tooltip".Translate()
            );

            listing.End();
        }

        private static void DrawDisplayTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                 "FeatherMedals_Settings_ShowTrophyCatalog".Translate(),
                ref Settings.ShowTrophyCatalog,
                "FeatherMedals_Settings_ShowTrophyCatalog_Tooltip".Translate()
            );
            listing.CheckboxLabeled(
                "FeatherMedals_Settings_DrawTrophiesOnPawns".Translate(),
                ref Settings.DrawTrophiesOnPawns,
                "FeatherMedals_Settings_DrawTrophiesOnPawns_Tooltip".Translate()
            );

            listing.Gap(10f);
            listing.GapLine();
            SubHeader(listing, "Worn Medals");

            listing.Label($"Worn size: {Settings.TrophyScale.ToStringPercent()}");
            Settings.TrophyScale = listing.Slider(Settings.TrophyScale, 0.1f, 2.0f);
            listing.Label($"Displayed medals: {Settings.MaxDisplayedTrophies.ToStringCached()}");
            listing.IntAdjuster(ref Settings.MaxDisplayedTrophies, 1);

            listing.End();
        }

        private static void SubHeader(Listing_Standard listing, string text)
        {
            Text.Font = GameFont.Medium;
            listing.Label(text);
            Text.Font = GameFont.Small;
            listing.Gap(4f);
        }
        
    }
    
    public class TrophyFeathersModSettings : ModSettings
    {
        // Default it to true so your intended behavior is the standard
        public bool TrophiesRequireCeremony = true;
        public bool LockTrophyUponAward = true;
        public bool DrawTrophiesOnPawns = true;
        public bool TrophyDynamicTraits = true;
        public bool PromptForCitationDuringRitual = true;
        public float TrophyScale = 0.8f;
        public int MaxDisplayedTrophies = 9;
        public bool ShowTrophyCatalog = true;

        // This method saves and loads the setting
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref TrophiesRequireCeremony, "TrophiesRequireCeremony", true);
            Scribe_Values.Look(ref LockTrophyUponAward, "LockTrophyUponAward", true);
            Scribe_Values.Look(ref DrawTrophiesOnPawns, "DrawTrophiesOnPawns", true);
            Scribe_Values.Look(ref MaxDisplayedTrophies, "MaxDisplayedTrophies", 9);
            Scribe_Values.Look(ref PromptForCitationDuringRitual, "PromptForCitationDuringRitual", true);
            Scribe_Values.Look(ref TrophyDynamicTraits, "TrophyDynamicTraits", true);
            Scribe_Values.Look(ref TrophyScale, "TrophyScale", 0.8f);
            Scribe_Values.Look(ref ShowTrophyCatalog, "ShowTrophyCatalog", true);
        }
    }

    [StaticConstructorOnStartup]
    public static class MedalModInit
    {
        static MedalModInit()
        {
            var harmony = new HarmonyLib.Harmony("FeatherMedals");
            harmony.PatchAll();
            Log.Message("[FeatherMedals] Harmony patches applied successfully.");
        }
    }
    
}