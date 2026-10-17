using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public static class HonoringWarriorsUtil
{
    private const int CacheTicks = 60;
    
    private static int cacheTick = -CacheTicks;
    private static readonly HashSet<Pawn> eligible = new();
    private static readonly HashSet<Pawn> wearers = new();

    public static bool ForbidsCeremony(Pawn p)
    {
        return p?.Ideo != null && p.Ideo.HasPrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Forbidden);
    }
    
    public static void AddCompatibleStanceToIdeosMissingIssue()
    {
        foreach (var ideo in Find.IdeoManager.IdeosListForReading)
        {
            if (ideo.classicMode)
                continue;
            if (ideo.PreceptsListForReading.Any(p => p.def.issue == FeatherMedalDefOf.FeatherMedals_HonoringWarriors))
                continue;
            var stance = PickClosestToNeutral(ideo);
            if (stance == null)
                continue;
            
            ideo.AddPrecept(PreceptMaker.MakePrecept(stance), true);
            
            if (Prefs.DevMode)
                Log.Message($"[FeatherTrophies] {ideo.name} updated with new precept {stance.defName}.");
        }
    }

    private static PreceptDef PickClosestToNeutral(Ideo ideo)
    {
        var neutralOrder = FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Neutral.displayOrderInIssue;
        PreceptDef best = null;
        var bestDistance = int.MaxValue;
        foreach (var def in DefDatabase<PreceptDef>.AllDefsListForReading)
        {
            if (def.issue != FeatherMedalDefOf.FeatherMedals_HonoringWarriors || ConflictsWithMemes(ideo, def))
                continue;
            var distance = System.Math.Abs(def.displayOrderInIssue - neutralOrder);
            if (distance < bestDistance || (distance == bestDistance && def.displayOrderInIssue > best.displayOrderInIssue))
            {
                best = def;
                bestDistance = distance;
            }
        }
        return best;
    }

    public static bool ConflictsWithMemes(Ideo ideo, PreceptDef def)
    {
        var conflicts = def.conflictingMemes;
        if (conflicts == null)
            return false;
        for (var i = 0; i < conflicts.Count; i++)
        {
            if (ideo.HasMeme(conflicts[i]))
                return true;
        }
        return false;
    }

    public static bool IsEligible(Pawn p)
    {
        return p is { Dead: false }
               && p.RaceProps.Humanlike
               && p.Faction == Faction.OfPlayer
               && !p.IsPrisoner
               && !p.DevelopmentalStage.Juvenile()
               && !p.IsQuestLodger();
    }

    public static int WornTrophies(Pawn p)
    {
        var worn = p?.apparel?.WornApparel;
        if (worn == null)
            return 0;
        var count = 0;
        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is FeatherMedal)
                count++;
        }
        return count;
    }

    public static bool MeetsExpectation(Pawn p, ExpectationDef min)
    {
        if (min == null)
            return true;
        var current = ExpectationsUtility.CurrentExpectationFor(p);
        return current != null && current.order >= min.order;
    }

    public static int OthersWearing(Pawn self)
    {
        Refresh();
        var count = wearers.Count;
        if (wearers.Contains(self))
            count--;
        return count;
    }

    public static int OthersNotWearing(Pawn self)
    {
        Refresh();
        var count = eligible.Count - wearers.Count;
        if (eligible.Contains(self) && !wearers.Contains(self))
            count--;
        return count;
    }
    
    public static void ResetCache()
    {
        cacheTick = -CacheTicks;
        eligible.Clear();
        wearers.Clear();
    }

    private static void Refresh()
    {
        var now = Find.TickManager.TicksGame;
        
        if (now >= cacheTick && now - cacheTick < CacheTicks)
            return;

        cacheTick = now;
        eligible.Clear();
        wearers.Clear();
        foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep)
        {
            if (!IsEligible(p))
                continue;
            eligible.Add(p);
            if (WornTrophies(p) > 0)
                wearers.Add(p);
        }
    }
}