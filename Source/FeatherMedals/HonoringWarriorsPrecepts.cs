using RimWorld;
using Verse;

namespace TribalTrophies;

public enum HonoringStance
{
    Forbidden,
    Disapproved,
    Neutral,
    Desired,
    Required
}

public static class HonoringWarriorsPrecepts
{

    public static HonoringStance StanceOf(Pawn pawn)
    {
        return StanceOf(pawn?.Ideo);
    }

    public static HonoringStance StanceOf(Ideo ideo)
    {
        if (ideo == null)
            return HonoringStance.Neutral;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Forbidden))
            return HonoringStance.Forbidden;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Disapproved))
            return HonoringStance.Disapproved;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Required))
            return HonoringStance.Required;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Desired))
            return HonoringStance.Desired;
        return HonoringStance.Neutral;
    }

}
