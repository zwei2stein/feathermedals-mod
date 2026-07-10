using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualStageAction_StartAdorning : RitualStageAction
{
    public override void Apply(LordJob_Ritual ritual)
    {
        var leader = ritual.PawnWithRole("leader");
        var awardee = ritual.PawnWithRole("awardee");
        if (leader != null && awardee != null)
        {
            if (MedalMod.Settings.PromptForCitationDuringRitual)
                if (ritual.selectedTarget.Thing is FeatherMedal medal && medal.citation.NullOrEmpty())
                    Find.WindowStack.Add(new Dialog_WriteCitation(medal));
                
            Messages.Message(
                "FeatherMedals_Ceremony_Started".Translate(leader.Named("LEADER"), awardee.Named("PAWN")),
                leader, // This makes the message clickable to jump to the leader
                MessageTypeDefOf.PositiveEvent
            );
        }
        else
        {
            Log.Error("[FeatherMedals] RitualStageAction_StartBestowal: Leader or Awardee is null!");
        }
    }

    public override void ExposeData()
    {
    }
}