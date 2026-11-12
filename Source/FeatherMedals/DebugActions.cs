using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

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

    public static class DebugActionsEmbroideries
    {
        private const int TrophyCount = 10;

        [DebugAction("Tribal Trophies", "Adorn with 10 random embroideries", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AdornWithRandomEmbroideries(Pawn pawn)
        {
            NpcTrophyGenerator.AdornForDebug(pawn, TribalTrophiesDefOf.TribalTrophies_TrophyKind_Embroidery, TrophyCount);
        }
        
        [DebugAction("Tribal Trophies", "Adorn with 10 random feathers", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AdornWithRandomFeathers(Pawn pawn)
        {
            NpcTrophyGenerator.AdornForDebug(pawn, TribalTrophiesDefOf.TribalTrophies_TrophyKind_Feather, TrophyCount);
        }
    }
}