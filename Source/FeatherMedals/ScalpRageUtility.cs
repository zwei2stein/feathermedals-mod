using Verse;
using Verse.AI;

namespace TribalTrophies
{
    public class ScalpRageExtension : DefModExtension
    {
        public bool targetColonists;
        public bool targetSlaves;
        public bool targetPrisoners;
    }

    public static class ScalpRageUtility
    {

        private static bool IsAllowedPawn(Pawn pawn, Pawn target, ScalpRageExtension ext)
        {
            if (target.IsPrisoner)
                return ext.targetPrisoners && target.HostFaction == pawn.Faction;

            if (target.IsSlave)
                return ext.targetSlaves && target.Faction == pawn.Faction;

            return ext.targetColonists && target.Faction == pawn.Faction;
        }

        public static bool IsValidTarget(Pawn pawn, Pawn target, MentalStateDef def)
        {
            if (target == null || target == pawn)
                return false;

            Thing thing = target.Dead ? target.Corpse : target;
            if (thing == null || !thing.Spawned || thing.Map != pawn.Map)
                return false;

            if (!ScalpingUtility.CanBeTargetedInBreak(thing))
                return false;

            if (!target.Dead)
            {
                var ext = def.GetModExtension<ScalpRageExtension>();
                if (ext == null || !IsAllowedPawn(pawn, target, ext))
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

        public static Pawn FindTarget(Pawn pawn, MentalStateDef def)
        {
            if (!pawn.Spawned)
                return null;

            var candidates = new System.Collections.Generic.List<Pawn>();
            foreach (var spawnedPawn in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (IsValidTarget(pawn, spawnedPawn, def))
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

}