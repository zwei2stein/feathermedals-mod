using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TribalTrophies;

public class JobDriver_CutTongue : JobDriver
{
    private Thing Target => job.targetA.Thing;

    private bool CanCutTarget()
    {
        return TongueCuttingUtility.CanBeCut(Target, pawn);
    }

    private int DurationTicks()
    {
        var tuning = TongueCuttingUtility.Tuning;
        float speed = pawn.GetStatValue(StatDefOf.MedicalTendSpeed);
        if (speed <= 0f)
            return tuning.maxDurationTicks;

        return Mathf.Clamp(Mathf.RoundToInt(tuning.maxDurationTicks / speed), tuning.minDurationTicks, tuning.maxDurationTicks);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);

        var walk = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        // can change eligibility while in progress
        walk.FailOn(() => !CanCutTarget());
        yield return walk;

        // Non-hostile target: target goes hostile.
        yield return Toils_General.Do(() =>
        {
            if (Target is Pawn victim && TrophyBodyPartUtility.WouldAngerFaction(victim))
            {
                TongueCuttingUtility.Provoke(victim);
                victim.Map?.designationManager.TryRemoveDesignationOn(victim, TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation);
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
        });

        var work = Toils_General.WaitWith(TargetIndex.A, DurationTicks(), true, false, false, TargetIndex.A);

        // can change eligibility while in progress
        work.FailOn(() => !CanCutTarget());

        work.WithEffect(EffecterDefOf.Surgery, TargetIndex.A);
        yield return work;

        yield return Toils_General.Do(() =>
        {
            Thing target = Target;
            Map map = target.Map;
            TongueCuttingUtility.DoCutTongue(pawn, target);
            pawn.skills?.Learn(SkillDefOf.Medicine, TongueCuttingUtility.Tuning.medicineXp);
            map?.designationManager.TryRemoveDesignationOn(target, TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation);
        });
    }
}
