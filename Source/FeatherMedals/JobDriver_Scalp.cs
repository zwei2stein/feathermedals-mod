using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TribalTrophies
{
    public class JobDriver_ScalpInBreak : JobDriver_Scalp
    {
        protected override bool InBreak => true;
    }
    
    public class JobDriver_Scalp : JobDriver
    {
        private const int MinDurationTicks = 50;
        private const int MaxDurationTicks = 300;

        protected const float MedicineXp = 100f;

        private protected Thing Target => job.targetA.Thing;

        // Mental break breaks ideology rules.
        protected virtual bool InBreak => false;

        protected bool CanScalpTarget()
        {
            return InBreak ? ScalpingUtility.CanBeScalpedInBreak(Target) : ScalpingUtility.CanBeScalped(Target, pawn);
        }

        protected int DurationTicks()
        {
            float speed = pawn.GetStatValue(StatDefOf.MedicalTendSpeed);
            if (speed <= 0f)
                return MaxDurationTicks;

            return Mathf.Clamp(Mathf.RoundToInt(MaxDurationTicks / speed), MinDurationTicks, MaxDurationTicks);
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
            walk.FailOn(() => !CanScalpTarget());
            yield return walk;

            // Non-hostile target: targets goes hostile.
            yield return Toils_General.Do(() =>
            {
                if (Target is Pawn victim && ScalpingUtility.WouldAngerFaction(victim))
                {
                    ScalpingUtility.Provoke( victim);
                    victim.Map?.designationManager.TryRemoveDesignationOn(victim, TribalTrophiesDefOf.TribalTrophies_Scalping_Designation_ScalpDesignation);
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            });

            var work = Toils_General.WaitWith(TargetIndex.A, DurationTicks(), true, false, false, TargetIndex.A);
            
            // can change eligibility while in progress
            work.FailOn(() => !CanScalpTarget());
            
            work.WithEffect(EffecterDefOf.Surgery, TargetIndex.A);
            yield return work;

            yield return Toils_General.Do(() =>
            {
                Thing target = Target;
                Map map = target.Map;
                ScalpingUtility.DoScalp(pawn, target, InBreak);
                if (InBreak)
                    (pawn.MentalState as MentalState_ScalpRage)?.Notify_Scalped();
                pawn.skills?.Learn(SkillDefOf.Medicine, MedicineXp);
                map?.designationManager.TryRemoveDesignationOn(target, TribalTrophiesDefOf.TribalTrophies_Scalping_Designation_ScalpDesignation);
            });
        }
    }
}