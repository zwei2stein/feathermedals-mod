using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals
{
    public class JobDriver_Scalp : JobDriver
    {
        private Thing Target => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            
            var walk = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            
            // can change eligibility while in progress
            walk.FailOn(() => !ScalpingUtility.CanBeScalped(Target));
            yield return walk;

            // Non-hostile target: caught in the act, target defends itself and the attempt fails.
            yield return Toils_General.Do(() =>
            {
                if (Target is Pawn victim && ScalpingUtility.WouldAngerFaction(victim))
                {
                    ScalpingUtility.Provoke(pawn, victim);
                    victim.Map?.designationManager.TryRemoveDesignationOn(victim, FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation);
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            });

            var work = Toils_General.WaitWith(TargetIndex.A, ScalpingTuning.DurationTicks, true, false, false, TargetIndex.A);
            
            // can change eligibility while in progress
            work.FailOn(() => !ScalpingUtility.CanBeScalped(Target));
            
            work.WithEffect(EffecterDefOf.Surgery, TargetIndex.A);
            yield return work;

            yield return Toils_General.Do(() =>
            {
                Thing target = Target;
                Map map = target.Map;
                ScalpingUtility.DoScalp(pawn, target);
                map?.designationManager.TryRemoveDesignationOn(target, FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation);
            });
        }
    }
}