using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies
{
    public class JobGiver_TakeScalp : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            return ScalpHuntUtility.TakeScalpJob(pawn);
        }
    }

    public class JobDriver_TakeScalp : JobDriver_Scalp
    {
        private const TargetIndex VictimInd = TargetIndex.A;
        private const TargetIndex ExitCellInd = TargetIndex.B;
        private const TargetIndex ScalpInd = TargetIndex.C;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var walk = Toils_Goto.GotoThing(VictimInd, PathEndMode.Touch);
            walk.FailOnDespawnedOrNull(VictimInd);
            walk.FailOn(() => !CanScalpTarget());
            yield return walk;

            var work = Toils_General.WaitWith(VictimInd, DurationTicks(), true, false, false, VictimInd);
            work.FailOnDespawnedOrNull(VictimInd);
            work.FailOn(() => !CanScalpTarget());
            work.WithEffect(EffecterDefOf.Surgery, VictimInd);
            work.AddPreInitAction(() => Messages.Message(
                "TribalTrophies_ScalpHunt_Message_Scalping".Translate(
                    pawn.Named("SCALPER"),
                    ScalpingUtility.PawnOf(Target).Named("VICTIM")),
                Target, MessageTypeDefOf.ThreatBig));
            yield return work;

            yield return Toils_General.Do(() =>
            {
                var victim = Target;
                var victimPawn = ScalpingUtility.PawnOf(victim);
                var scalp = ScalpingUtility.DoScalp(pawn, victim);
                pawn.skills?.Learn(SkillDefOf.Medicine, MedicineXp);
                if (scalp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                job.SetTarget(ScalpInd, scalp);
                // target is scalper
                Messages.Message("TribalTrophies_ScalpHunt_Message_TookScalp".Translate(
                        pawn.Named("SCALPER"),
                        victimPawn.Named("VICTIM")),
                    pawn, MessageTypeDefOf.ThreatSmall);
            });

            yield return Toils_Haul.StartCarryThing(ScalpInd);

            var flee = Toils_Goto.GotoCell(ExitCellInd, PathEndMode.OnCell);
            flee.tickAction = () =>
            {
                if (Map.exitMapGrid.IsExitCell(pawn.Position))
                    pawn.ExitMap(true, CellRect.WholeMap(Map).GetClosestEdge(pawn.Position));
            };
            yield return flee;

            var leave = ToilMaker.MakeToil(nameof(MakeNewToils));
            leave.initAction = () =>
            {
                if (pawn.Position.OnEdge(pawn.Map) || pawn.Map.exitMapGrid.IsExitCell(pawn.Position))
                    pawn.ExitMap(true, CellRect.WholeMap(Map).GetClosestEdge(pawn.Position));
            };
            leave.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return leave;
        }
    }

}