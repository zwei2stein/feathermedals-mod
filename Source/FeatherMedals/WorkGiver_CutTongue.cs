using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

// One class, three WorkGiverDefs (Doctor, Cooking, Handling): any of them can do the job.
public class WorkGiver_CutTongue : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        // HasJobOnThing can remove designations while this is happening.
        foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation).ToList())
        {
            yield return designation.target.Thing;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!TongueCuttingUtility.CanBeCutByAnyone(t))
        {
            // Target no longer qualifies, drop designation.
            t.Map?.designationManager.TryRemoveDesignationOn(t, TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation);
            return false;
        }
        if (ScalpingPrecepts.StanceOf(pawn) == ScalpStance.Forbidden)
        {
            JobFailReason.Is("IdeoligionForbids".Translate());
            return false;
        }
        if (!TongueCuttingUtility.CanBeCut(t, pawn))
            return false;
        if (!pawn.CanReserve(t, ignoreOtherReservations: forced))
            return false;
        if (pawn.WorkTagIsDisabled(WorkTags.Violent))
        {
            JobFailReason.Is("IsIncapableOfViolenceShort".Translate((NamedArgument)(Thing)pawn));
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(TribalTrophiesDefOf.TribalTrophies_TongueCutting_Job_CutTongueJob, t);
    }
}
