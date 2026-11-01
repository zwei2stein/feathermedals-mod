using RimWorld.Planet;

namespace TribalTrophies;

// Class name is in saves.
public class QuestPart_InitiateScalpRequest : QuestPart_InitiateTrophyRequest
{
    protected override TrophyRequestComp RequestOf(Settlement settlement) => settlement.GetComponent<ScalpRequestComp>();
}
