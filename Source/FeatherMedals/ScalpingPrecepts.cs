using RimWorld;
using Verse;

namespace FeatherMedals;

public enum ScalpStance
{
    Honourable,
    Merciless,
    Forbidden
}

public static class ScalpingPrecepts
{
    
    public static ScalpStance StanceOf(Pawn pawn)
    {
        var ideo = pawn?.Ideo;
        if (ideo == null)
            return ScalpStance.Honourable;
        return StanceOf(ideo);
    }

    public static ScalpStance StanceOf(Ideo ideo)
    {
        if (ideo == null)
            return ScalpStance.Honourable;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Forbidden))
            return ScalpStance.Forbidden;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Merciless))
            return ScalpStance.Merciless;
        return ScalpStance.Honourable;
    }

    public static bool AnyMercilessColonist()
    {
        foreach (var ideo in Faction.OfPlayer.ideos.AllIdeos)
        {
            if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Merciless))
                return true;
        }
        return false;
    }

}