using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies
{
    public class TongueRageExtension : DefModExtension
    {
        public bool targetColonists;
        public bool targetSlaves;
        public bool targetPrisoners;
    }

    public static class TongueRageUtility
    {
        public static bool IsValidTarget(Pawn pawn, Pawn target, MentalStateDef def)
        {
            var ext = def.GetModExtension<TongueRageExtension>();
            return TrophyRageUtility.IsValidTarget(pawn, target,
                TongueCuttingUtility.CanBeTargetedInBreak,
                living => ext != null && TrophyRageUtility.IsAllowedPawn(pawn, living, ext.targetColonists, ext.targetSlaves, ext.targetPrisoners));
        }

        public static Pawn FindTarget(Pawn pawn, MentalStateDef def)
        {
            return TrophyRageUtility.FindTarget(pawn, candidate => IsValidTarget(pawn, candidate, def));
        }
    }

    public class MentalStateWorker_TongueRage : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            return base.StateCanOccur(pawn)
                   && TrophyRageUtility.CanDoViolentBreak(pawn)
                   && TongueRageUtility.FindTarget(pawn, def) != null;
        }
    }

    // One tongue, then the break is over.
    public class MentalState_TongueRage : MentalState
    {
        public Pawn target;

        private bool cut;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
            Scribe_Values.Look(ref cut, "cut");
        }

        public override void PreStart()
        {
            base.PreStart();
            TryFindNewTarget();
        }

        public void Notify_Cut()
        {
            cut = true;
        }

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            if (pawn.MentalState != this)
                return;

            if (cut)
            {
                RecoverFromState();
                return;
            }

            if (pawn.IsHashIntervalTick(120, delta) && !TongueRageUtility.IsValidTarget(pawn, target, def))
            {
                // Corpse gone, tongue cut out by someone else, unreachable, whatever: start over.
                if (!TryFindNewTarget())
                    RecoverFromState();
            }
        }

        public bool TryFindNewTarget()
        {
            target = TongueRageUtility.FindTarget(pawn, def);
            return target != null;
        }

        public override TaggedString GetBeginLetterText()
        {
            if (target == null || def.beginLetter.NullOrEmpty())
                return "";

            return def.beginLetter.Formatted(
                pawn.NameShortColored, target.NameShortColored, pawn.Named("PAWN"), target.Named("TARGET")
            ).AdjustedFor(pawn).Resolve().CapitalizeFirst();
        }
    }

    public class JobGiver_TongueRage : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var state = pawn.MentalState as MentalState_TongueRage;
            var victim = state?.target;
            if (victim == null)
                return null;

            Thing thing = victim.Dead ? victim.Corpse : victim;
            if (thing == null || !thing.Spawned)
                return null;

            // Another pawn took the target, pick again.
            if (!pawn.CanReserve(thing))
            {
                if (!state.TryFindNewTarget())
                    return null;

                victim = state.target;
                thing = victim.Dead ? victim.Corpse : victim;
                if (thing == null || !thing.Spawned)
                    return null;
            }

            if (TongueCuttingUtility.CanBeCutInBreak(thing))
            {
                var cutTongue = JobMaker.MakeJob(TribalTrophiesDefOf.TribalTrophies_TongueCutting_Job_CutTongueInBreakJob, thing);
                cutTongue.canBashDoors = true;
                return cutTongue;
            }

            if (victim.Dead)
                return null;

            // a target that has to be downed first (not the case for prisoners)
            var attack = JobMaker.MakeJob(JobDefOf.AttackMelee, thing);
            attack.canBashDoors = true;
            attack.killIncappedTarget = false;
            return attack;
        }
    }
}
