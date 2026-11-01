using RimWorld;

namespace TribalTrophies;

// Class name is in saves.
public class QuestPart_ScalpRequestTargetGoodwillLoss : QuestPart_TrophyRequestTargetGoodwillLoss
{
    protected override HistoryEventDef Reason => TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_AcceptedScalpRequest;

    protected override string TargetLearnedKey => "TribalTrophies_ScalpRequest_Message_TargetLearned";
}
