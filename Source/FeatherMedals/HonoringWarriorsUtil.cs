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
    
    public static void AddNeutralToIdeosMissingIssue()
    {
        foreach (var ideo in Find.IdeoManager.IdeosListForReading)
        {
            if (ideo.classicMode)
                continue;
            if (ideo.PreceptsListForReading.Any(p => p.def.issue == FeatherMedalDefOf.FeatherMedals_HonoringWarriors))
                continue;
            ideo.AddPrecept(PreceptMaker.MakePrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Neutral), true);
        }
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