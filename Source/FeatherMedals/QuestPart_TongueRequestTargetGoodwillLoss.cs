using RimWorld;

namespace TribalTrophies;

// Class name is in saves.
public class QuestPart_TongueRequestTargetGoodwillLoss : QuestPart_TrophyRequestTargetGoodwillLoss
{
    protected override HistoryEventDef Reason => TribalTrophiesDefOf.TribalTrophies_TongueCutting_HistoryEvent_AcceptedTongueRequest;

    protected override string TargetLearnedKey => "TribalTrophies_TongueRequest_Message_TargetLearned";
}
