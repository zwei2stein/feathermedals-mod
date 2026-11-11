using Verse;

namespace TribalTrophies;

// TODO: remove in Q2 2027
// Mod settings saved before display and NPC settings were split into feathers and embroideries: the old keys were feather settings.
public static class SettingsLegacyKeys
{
    public static void LoadRenamed(TribalTrophiesSettings settings)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars)
            return;

        LoadRenamed(ref settings.DrawFeatherTrophiesOnPawns, "DrawFeatherTrophiesOnPawns", "DrawTrophiesOnPawns");
        LoadRenamed(ref settings.MaxDisplayedTrophiesFeathers, "MaxDisplayedTrophiesFeathers", "MaxDisplayedTrophies");
        LoadRenamed(ref settings.TrophyScaleFeather, "TrophyScaleFeather", "TrophyScale");
        LoadRenamed(ref settings.GenerateNpcFeatherTrophies, "GenerateNpcFeatherTrophies", "GenerateNpcTrophies");
        LoadRenamed(ref settings.NpcFeatherTrophyMinTech, "NpcFeatherTrophyMinTech", "NpcTrophyMinTech");
        LoadRenamed(ref settings.NpcFeatherTrophyMaxTech, "NpcFeatherTrophyMaxTech", "NpcTrophyMaxTech");
    }

    // old key only when the new one is not saved yet; saving writes the new key, so this runs once
    private static void LoadRenamed<T>(ref T value, string key, string oldKey)
    {
        if (Scribe.loader.curXmlParent?[key] != null)
            return;

        Scribe_Values.Look(ref value, oldKey, value);
    }
}
