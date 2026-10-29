using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TribalTrophies;

public static class HonoringWarriorsUtility
{
    private const int CacheTicks = 60;
    
    private static int cacheTick = -CacheTicks;
    private static readonly HashSet<Pawn> eligible = new();
    private static readonly HashSet<Pawn> wearers = new();

    public static bool ForbidsCeremony(Pawn p)
    {
        return p?.Ideo != null && p.Ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Forbidden);
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
            if (worn[i] is FeatherTrophy)
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