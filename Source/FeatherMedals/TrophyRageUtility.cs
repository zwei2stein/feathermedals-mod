using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TribalTrophies;

// Rules shared by mental breaks that end in taking a body part trophy (scalping, tongue cutting).
public static class TrophyRageUtility
{
    // Which of the raging pawn's own people it may go for.
    public static bool IsAllowedPawn(Pawn pawn, Pawn target, bool targetColonists, bool targetSlaves, bool targetPrisoners)
    {
        if (target.IsPrisoner)
            return targetPrisoners && target.HostFaction == pawn.Faction;

        if (target.IsSlave)
            return targetSlaves && target.Faction == pawn.Faction;

        return targetColonists && target.Faction == pawn.Faction;
    }

    // canBeTargeted: the break's own rule for the body; isAllowedPawn: who among the living it may go for
    public static bool IsValidTarget(Pawn pawn, Pawn target, Func<Thing, bool> canBeTargeted, Func<Pawn, bool> isAllowedPawn)
    {
        if (target == null || target == pawn)
            return false;

        Thing thing = target.Dead ? target.Corpse : target;
        if (thing == null || !thing.Spawned || thing.Map != pawn.Map)
            return false;

        if (!canBeTargeted(thing))
            return false;

        if (!target.Dead)
        {
            if (!isAllowedPawn(target))
                return false;

            if (target.InAggroMentalState)
                return false;

            if (target.CurJob != null && target.CurJob.exitMapOnArrival)
                return false;
        }

        // Someone else is already on it (another raging pawn): look elsewhere.
        if (!pawn.CanReserve(thing))
            return false;

        return pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly);
    }

    public static Pawn FindTarget(Pawn pawn, Func<Pawn, bool> isValidTarget)
    {
        if (!pawn.Spawned)
            return null;

        var candidates = new List<Pawn>();
        foreach (var spawnedPawn in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (isValidTarget(spawnedPawn))
                candidates.Add(spawnedPawn);
        }

        return candidates.Count == 0 ? null : candidates.RandomElement();
    }

    // Enabled violence, not trait that forbids murderous rage like Kind
    public static bool CanDoViolentBreak(Pawn pawn)
    {
        if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            return false;

        if (pawn.story == null)
            return true;

        foreach (var trait in pawn.story.traits.allTraits)
        {
            if (trait.Suppressed)
                continue;

            var disallowed = trait.CurrentData.disallowedMentalStates;
            if (disallowed != null && disallowed.Contains(TribalTrophiesDefOf.MurderousRage))
                return false;
        }

        return true;
    }
}
