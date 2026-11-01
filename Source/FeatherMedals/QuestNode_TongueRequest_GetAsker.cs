using RimWorld.Planet;

namespace TribalTrophies;

public class QuestNode_TongueRequest_GetAsker : QuestNode_TrophyRequest_GetAsker
{
    protected override bool QuestsEnabled => TribalTrophiesMod.Settings.TongueRequestQuests;

    protected override TrophyRequestComp RequestOf(Settlement settlement) => settlement.GetComponent<TongueRequestComp>();
}

public class QuestNode_TongueRequest_Initiate : QuestNode_TrophyRequest_Initiate
{
    protected override QuestPart_InitiateTrophyRequest MakePart() => new QuestPart_InitiateTongueRequest();
}

public class QuestNode_TongueRequest_TargetGoodwillLoss : QuestNode_TrophyRequest_TargetGoodwillLoss
{
    protected override QuestPart_TrophyRequestTargetGoodwillLoss MakePart() => new QuestPart_TongueRequestTargetGoodwillLoss();
}
