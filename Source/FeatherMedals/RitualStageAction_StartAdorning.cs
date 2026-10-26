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
            if (TribalTrophiesMod.Settings.PromptForCitationDuringRitual)
                if (ritual.selectedTarget.Thing is FeatherTrophy featherTrophy && !featherTrophy.HasCitation)
                    Find.WindowStack.Add(new Dialog_WriteCitation(featherTrophy));
                
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_Started".Translate(leader.Named("LEADER"), awardee.Named("PAWN")),
                leader, // This makes the message clickable to jump to the leader
                MessageTypeDefOf.PositiveEvent
            );
        }
        else
        {
            Log.Error("[TribalTrophies] RitualStageAction_StartAdorning: Leader or Awardee is null!");
        }
    }

    public override void ExposeData()
    {
    }
}