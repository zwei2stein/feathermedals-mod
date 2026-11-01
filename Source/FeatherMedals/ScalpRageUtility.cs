using Verse;

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
            return TrophyRageUtility.IsAllowedPawn(pawn, target, ext.targetColonists, ext.targetSlaves, ext.targetPrisoners);
        }

        public static bool IsValidTarget(Pawn pawn, Pawn target, MentalStateDef def)
        {
            var ext = def.GetModExtension<ScalpRageExtension>();
            return TrophyRageUtility.IsValidTarget(pawn, target,
                ScalpingUtility.CanBeTargetedInBreak,
                living => ext != null && IsAllowedPawn(pawn, living, ext));
        }

        public static Pawn FindTarget(Pawn pawn, MentalStateDef def)
        {
            return TrophyRageUtility.FindTarget(pawn, candidate => IsValidTarget(pawn, candidate, def));
        }

        // Enabled violence, not trait that forbids murderous rage like Kind
        public static bool CanDoViolentBreak(Pawn pawn)
        {
            return TrophyRageUtility.CanDoViolentBreak(pawn);
        }
    }

}
