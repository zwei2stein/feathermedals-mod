using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FeatherMedals;

public static class ScalpHuntUtility
{
    private const int RaidersPerScalp = 4;

    private const int MinEligibleVictims = 2;

    // reused list, main thread only
    private static readonly List<Thing> tmpCandidates = new();

    // scalping raider: no non-humans, physicaly able
    public static bool CanTakeScalps(Pawn hunter)
    {
        return hunter.Spawned
               && !hunter.Downed
               && hunter.RaceProps.Humanlike
               && !hunter.IsSubhuman
               && hunter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
    }
    
    private static void CollectColonistsToScalp(Map map, List<Thing> into)
    {
        into.Clear();
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.Downed
                && pawn.IsFreeNonSlaveColonist
                && ScalpingUtility.HasScalpToTake(pawn))
                into.Add(pawn);
        }
        foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
        {
            if (thing is Corpse corpse
                && corpse.GetRotStage() == RotStage.Fresh
                && corpse.InnerPawn.IsFreeNonSlaveColonist
                && ScalpingUtility.HasScalpToTake(corpse.InnerPawn))
                into.Add(corpse);
        }
    }

    // colonist down or fresh dead who still has a scalp.
    public static bool AnyColonistDownToScalp(Map map)
    {
        CollectColonistsToScalp(map, tmpCandidates);
        var any = tmpCandidates.Count > 0;
        tmpCandidates.Clear();
        return any;
    }

    public static bool CanHunt(Faction faction, Map map)
    {
        if (!TribalTrophiesMod.Settings.ScalpHuntRaids)
            return false;
        if (faction == null || !faction.def.humanlikeFaction || !faction.HostileTo(Faction.OfPlayer))
            return false;
        if (faction.def.techLevel > TribalTrophiesMod.Settings.ScalpHuntMaxTech)
            return false;
        if (ScalpingPrecepts.StanceOf(faction.ideos?.PrimaryIdeo) == ScalpStance.Forbidden)
            return false;
        if (map == null || EligibleVictimCount(map, faction) < MinEligibleVictims)
            return false;
        return true;
    }

    public static int EligibleVictimCount(Map map, Faction faction)
    {
        var allowChildren = ScalpingPrecepts.StanceOf(faction.ideos?.PrimaryIdeo) == ScalpStance.Merciless;
        var count = 0;
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (!pawn.IsFreeNonSlaveColonist)
                continue;
            if (!allowChildren && !pawn.DevelopmentalStage.Adult())
                continue;
            if (!ScalpingUtility.HasScalpToTake(pawn))
                continue;
            count++;
        }
        return count;
    }

    public static int GoalFor(int raiders, int eligibleVictims)
    {
        var wanted = Mathf.CeilToInt(raiders / (float)RaidersPerScalp);
        var cap = eligibleVictims / 4;
        return Mathf.Max(1, Mathf.Min(wanted, cap));
    }

    // KidnapAIUtility.TryFindGoodKidnapVictim.
    // Unlike kidnappers, hunters seek victims anywhere on the map: nearest one this hunter may scalp.
    public static bool TryFindVictim(Pawn hunter, out Thing victim, List<Thing> disallowed = null)
    {
        victim = null;
        if (!CanTakeScalps(hunter)
            || hunter.Faction == null
            || !hunter.Map.reachability.CanReachMapEdge(hunter.Position, TraverseParms.For(hunter, Danger.Some)))
            return false;

        CollectColonistsToScalp(hunter.Map, tmpCandidates);
        var bestDistance = int.MaxValue;
        foreach (var thing in tmpCandidates)
        {
            if (disallowed != null && disallowed.Contains(thing))
                continue;
            // living downed or freshly dead, adult unless the hunter is merciless: all inside CanBeScalped
            if (!ScalpingUtility.PawnOf(thing).Faction.HostileTo(hunter.Faction)
                || !ScalpingUtility.CanBeScalped(thing, hunter)
                || !hunter.CanReserve(thing))
                continue;
            // same rule as vanilla kidnappers: closed doors stop them
            if (!hunter.CanReach(thing, PathEndMode.Touch, Danger.Some, mode: TraverseMode.NoPassClosedDoors))
                continue;
            var distance = hunter.Position.DistanceToSquared(thing.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                victim = thing;
            }
        }
        tmpCandidates.Clear();
        return victim != null;
    }

    public static Job TakeScalpJob(Pawn hunter)
    {
        if (!hunter.Spawned)
            return null;
        // victim first: cheap when there is nothing to take, the exit search is not
        if (!TryFindVictim(hunter, out var victim) || GenAI.InDangerousCombat(hunter))
            return null;
        if (!RCellFinder.TryFindBestExitSpot(hunter, out var exitSpot))
            return null;

        var job = JobMaker.MakeJob(TribalTrophiesDefOf.TribalTrophies_Scalping_TakeScalpAndFlee, victim, exitSpot);
        job.count = 1;
        return job;
    }
    
}