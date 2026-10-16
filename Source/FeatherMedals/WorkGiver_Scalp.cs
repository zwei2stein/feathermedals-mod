using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals
{
    // One class, three WorkGiverDefs (Doctor, Cooking, Handling): any of them can do the job.
    public class WorkGiver_Scalp : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // HasJobOnThing can remove designations while this is happening.
            foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation).ToList())
            {
                yield return designation.target.Thing;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ScalpingUtility.CanBeScalpedByAnyone(t))
            {
                // Target no longer qualifies, drop designation.
                t.Map?.designationManager.TryRemoveDesignationOn(t, FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation);
                return false;
            }
            if (ScalpingPrecepts.StanceOf(pawn) == ScalpStance.Forbidden)
            {
                JobFailReason.Is("FeatherTrophies_Scalping_IdeoForbids".Translate());
                return false;
            }
            if (!ScalpingUtility.CanBeScalped(t, pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(t, ignoreOtherReservations: forced))
            {
                return false;
            }
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                JobFailReason.Is("IsIncapableOfViolenceShort".Translate((NamedArgument)(Thing)pawn));
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpJob, t);
        }
    }
}