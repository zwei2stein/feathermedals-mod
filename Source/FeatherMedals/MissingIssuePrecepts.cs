using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TribalTrophies;

public static class MissingIssuePrecepts
{
    
    public static void AddCompatibleStanceToIdeosMissingHonoringWarriors()
    {
        Fill(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors, new List<PreceptDef>
        {
            TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Neutral,
            TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Desired,
            TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Disapproved,
            TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Required,
            TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Forbidden
        });
    }
    
    public static void AddCompatibleStanceToIdeosMissingScalpTaking()
    {
        Fill(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking, new List<PreceptDef>
        {
            TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Honorable,
            TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Merciless,
            TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Forbidden
        });
    }
    
    public static void Fill(IssueDef issue, List<PreceptDef> playerOrder)
    {
        foreach (var ideo in Find.IdeoManager.IdeosListForReading)
        {
            if (ideo.classicMode || HasIssue(ideo, issue))
                continue;

            var precept = Pick(ideo, issue, playerOrder);
            ideo.AddPrecept(PreceptMaker.MakePrecept(precept), true);

            if (Prefs.DevMode)
                Log.Message($"[TribalTrophies] {ideo.name} updated with new precept {precept.defName}.");
        }
    }

    private static PreceptDef Pick(Ideo ideo, IssueDef issue, List<PreceptDef> playerOrder)
    {
        if (IsPlayerIdeo(ideo))
            return FirstAllowedOrFirst(ideo, playerOrder);
        // nothing rolled: same pick as for the player
        return PickLikeGeneration(ideo, issue) ?? FirstAllowedOrFirst(ideo, playerOrder);
    }

    private static bool HasIssue(Ideo ideo, IssueDef issue)
    {
        var precepts = ideo.PreceptsListForReading;
        for (var i = 0; i < precepts.Count; i++)
        {
            if (precepts[i].def.issue == issue)
                return true;
        }
        return false;
    }

    private static bool IsPlayerIdeo(Ideo ideo) => Faction.OfPlayer.ideos.Has(ideo);

    // Vanilla rule for what an ideo may hold: meme conflicts, faction's disallowed precepts, no high impact for classic factions.
    private static bool Allowed(Ideo ideo, PreceptDef def)
    {
        return ideo.foundation.CanAddForFaction(def, FactionDefUsing(ideo), null, true);
    }

    // First precept of the list the ideo can hold. None can: the first one anyway, the issue must be filled.
    private static PreceptDef FirstAllowedOrFirst(Ideo ideo, List<PreceptDef> order)
    {
        foreach (var def in order)
        {
            if (Allowed(ideo, def))
                return def;
        }
        return order[0];
    }

    // Same two passes as vanilla IdeoFoundation.RandomizePrecepts: precepts tied to the ideo's memes first,
    // then any allowed one, weighted by defaultSelectionWeight.
    private static PreceptDef PickLikeGeneration(Ideo ideo, IssueDef issue)
    {
        var allowed = new List<PreceptDef>();
        var associated = new List<PreceptDef>();
        foreach (var def in DefDatabase<PreceptDef>.AllDefsListForReading)
        {
            if (def.issue != issue || !Allowed(ideo, def))
                continue;
            allowed.Add(def);
            if (def.associatedMemes.Any(meme => ideo.memes.Contains(meme)))
                associated.Add(def);
        }

        if (associated.TryRandomElementByWeight(def => def.defaultSelectionWeight, out var fromMemes))
            return fromMemes;
        if (allowed.TryRandomElementByWeight(def => def.defaultSelectionWeight, out var any))
            return any;
        return null;
    }

    private static FactionDef FactionDefUsing(Ideo ideo)
    {
        foreach (var faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (faction.ideos?.PrimaryIdeo == ideo)
                return faction.def;
        }
        return null;
    }
}