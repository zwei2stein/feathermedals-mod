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
            // Pawns without ideo count as Honourable.
            return ScalpStance.Honourable;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Forbidden))
            return ScalpStance.Forbidden;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Merciless))
            return ScalpStance.Merciless;
        return ScalpStance.Honourable;
    }

    public static bool AnyMercilessColonist()
    {
        foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            if (StanceOf(p) == ScalpStance.Merciless)
                return true;
        }
        return false;
    }

    public static void AddStanceToIdeosMissingIssue()
    {
        // Honourable > Merciless > Forbidden. None allowed: Honourable
        foreach (var ideo in Find.IdeoManager.IdeosListForReading)
        {
            if (ideo.classicMode || HasStance(ideo))
                continue;

            var stance = PickStance(ideo);
            ideo.AddPrecept(PreceptMaker.MakePrecept(stance), true);

            if (Prefs.DevMode)
                Log.Message($"[FeatherTrophies] {ideo.name} updated with new precept {stance.defName}.");
        }
    }

    private static bool HasStance(Ideo ideo)
    {
        var precepts = ideo.PreceptsListForReading;
        for (var i = 0; i < precepts.Count; i++)
        {
            if (precepts[i].def.issue == FeatherMedalDefOf.FeatherTrophies_ScalpTaking)
                return true;
        }
        return false;
    }

    private static PreceptDef PickStance(Ideo ideo)
    {
        var order = new[]
        {
            FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Honourable,
            FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Merciless,
            FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Forbidden
        };
        foreach (var def in order)
        {
            if (!HonoringWarriorsUtil.ConflictsWithMemes(ideo, def))
                return def;
        }
        return FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Honourable;
    }
}