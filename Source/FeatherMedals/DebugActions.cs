using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals;

public class DebugActions
{
    public static class DebugActionsScalpHunt
    {
        [DebugAction("Tribal Trophies", "Take scalp and flee", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TakeScalpAndFlee(Pawn pawn)
        {
            var job = ScalpHuntUtility.TakeScalpJob(pawn);
            if (job == null)
            {
                Messages.Message("No downed colonist this pawn may scalp, or no way off the map.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }
    }
}