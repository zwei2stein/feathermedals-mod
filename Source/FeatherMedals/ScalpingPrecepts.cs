using RimWorld;
using Verse;

namespace TribalTrophies;

public enum ScalpStance
{
    Merciless,
    Honorable,
    Forbidden
}

public static class ScalpingPrecepts
{
    
    public static ScalpStance StanceOf(Pawn pawn)
    {
        var ideo = pawn?.Ideo;
        if (ideo == null)
            return ScalpStance.Honorable;
        return StanceOf(ideo);
    }

    public static ScalpStance StanceOf(Ideo ideo)
    {
        if (ideo == null)
            return ScalpStance.Honorable;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Forbidden))
            return ScalpStance.Forbidden;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Merciless))
            return ScalpStance.Merciless;
        return ScalpStance.Honorable;
    }

    // Non-player humanlike faction in the tech range whose primary ideo does not forbid scalping.
    public static bool FactionPracticesScalping(Faction faction, TechLevel minTech, TechLevel maxTech)
    {
        if (faction == null || faction.IsPlayer || !faction.def.humanlikeFaction)
            return false;
        if (faction.def.techLevel < minTech || faction.def.techLevel > maxTech)
            return false;
        return StanceOf(faction.ideos?.PrimaryIdeo) != ScalpStance.Forbidden;
    }

    public static bool AnyMercilessColonist()
    {
        foreach (var ideo in Faction.OfPlayer.ideos.AllIdeos)
        {
            if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Merciless))
                return true;
        }
        return false;
    }

}