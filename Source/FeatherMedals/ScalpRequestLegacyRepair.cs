using HarmonyLib;
using RimWorld;
using Verse;

namespace TribalTrophies;

// TODO: remove in Q1 2027
// 1.0.0 saved the scalp request in a wrong way, where, we are fixin it
[HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
public static class ScalpRequestLegacyRepair
{
    public static void Postfix()
    {
        foreach (var quest in Find.QuestManager.QuestsListForReading)
        {
            if (quest.State != QuestState.Ongoing)
                continue;

            foreach (var part in quest.PartsListForReading)
            {
                if (part is QuestPart_InitiateScalpRequest request)
                    Repair(quest, request);
            }
        }
    }

    private static void Repair(Quest quest, QuestPart_InitiateScalpRequest request)
    {
        if (request.inSignal != quest.InitiateSignal)
            return;

        var settlement = request.settlement;
        if (settlement == null || settlement.Destroyed)
            return;

        var comp = settlement.GetComponent<ScalpRequestComp>();
        if (comp == null || comp.ActiveRequest)
            return;

        // Time is up already: the quest timeout ends it.
        var expiration = quest.acceptanceTick + request.requestDuration;
        if (expiration <= Find.TickManager.TicksGame)
            return;

        // delivered count was saved under its own name and loaded fine
        comp.targetFaction = request.targetFaction;
        comp.requestCount = request.requestedCount;
        comp.expiration = expiration;

        Log.Message($"[TribalTrophies] Restored scalp request of {settlement.Label} from quest {quest.name}.");
    }
}
