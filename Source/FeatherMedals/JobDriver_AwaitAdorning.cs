using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FeatherMedals
{

    public class JobGiver_AwaitAdorning : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.GetLord()?.LordJob is not LordJob_Ritual ritual)
                return null;

            var standCell = pawn.mindState.duty?.focus ?? LocalTargetInfo.Invalid;
            if (!standCell.IsValid)
                return null;

            var presenter = ritual.assignments.FirstAssignedPawn("leader");
            if (presenter == null)
                return null;

            if (!pawn.CanReach(standCell, PathEndMode.OnCell, Danger.Deadly))
                return null;

            return JobMaker.MakeJob(FeatherMedalDefOf.TribalTrophies_Job_AwaitAdorning, standCell, presenter);
        }
    }
    
    public class JobDriver_AwaitAdorning : JobDriver
    {
        private const TargetIndex StandInd = TargetIndex.A;
        private const TargetIndex PresenterInd = TargetIndex.B;

        private Pawn Presenter => job.GetTarget(PresenterInd).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.GetLord()?.LordJob is not LordJob_Ritual);

            yield return Toils_Goto.GotoCell(StandInd, PathEndMode.OnCell);
            yield return Toils_General.Do(MarkArrived);
            yield return StandToil();
        }
        
        private void MarkArrived()
        {
            if (pawn.GetLord()?.LordJob is LordJob_Ritual ritual)
                ritual.AddTagForPawn(pawn, StageEndTrigger_RolesArrived.ArrivalTag);
        }

        private void FacePresenter()
        {
            var presenter = Presenter;
            if (presenter is { Spawned: true })
                pawn.rotationTracker.FaceTarget(presenter);
        }

        private Toil StandToil()
        {
            var toil = ToilMaker.MakeToil(nameof(StandToil));
            toil.tickIntervalAction = _ => FacePresenter();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.handlingFacing = true;
            return toil;
        }
    }

}