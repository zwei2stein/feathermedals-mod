using RimWorld.Planet;

namespace TribalTrophies;

public class QuestNode_ScalpRequest_GetAsker : QuestNode_TrophyRequest_GetAsker
{
    protected override bool QuestsEnabled => TribalTrophiesMod.Settings.ScalpRequestQuests;

    protected override TrophyRequestComp RequestOf(Settlement settlement) => settlement.GetComponent<ScalpRequestComp>();
}

public class QuestNode_ScalpRequest_Initiate : QuestNode_TrophyRequest_Initiate
{
    protected override QuestPart_InitiateTrophyRequest MakePart() => new QuestPart_InitiateScalpRequest();
}

public class QuestNode_ScalpRequest_TargetGoodwillLoss : QuestNode_TrophyRequest_TargetGoodwillLoss
{
    protected override QuestPart_TrophyRequestTargetGoodwillLoss MakePart() => new QuestPart_ScalpRequestTargetGoodwillLoss();
}
