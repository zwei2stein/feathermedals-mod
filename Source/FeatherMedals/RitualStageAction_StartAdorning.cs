using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualStageAction_StartAdorning : RitualStageAction
{
    public override void Apply(LordJob_Ritual ritual)
    {
        var leader = ritual.PawnWithRole("leader");
        var adorned = ritual.PawnWithRole(RitualRole_Adorned.ROLE_ID);
        if (leader != null && adorned != null)
        {
            if (TribalTrophiesMod.Settings.PromptForTaleDuringCeremony)
                if (ritual.selectedTarget.Thing is FeatherTrophy featherTrophy && !featherTrophy.HasTale)
                    Find.WindowStack.Add(new Dialog_WriteTale(featherTrophy));
                
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_Started".Translate(leader.Named("PRESENTER"), adorned.Named("PAWN")),
                leader, // This makes the message clickable to jump to the leader
                MessageTypeDefOf.PositiveEvent
            );
        }
        else
        {
            Log.Error("[TribalTrophies] RitualStageAction_StartAdorning: Leader or Adorned is null!");
        }
    }

    public override void ExposeData()
    {
    }
}