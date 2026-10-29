using RimWorld;
using Verse;

namespace TribalTrophies;

public static class NpcScalpUtility
{
    public static Scalp MakeScalp(Faction scalperFaction, Pawn scalper = null)
    {
        var victim = NpcVictimUtility.GenerateTemporaryVictim(scalperFaction);

        // taken while both were adults, where the victim's people live
        var scalp = Scalp.CreateFromTemporaryVictim(victim, scalper,
            NpcHistoryUtility.RandomPastEvent(victim.Faction, scalper, victim));

        NpcVictimUtility.Discard(victim);
        return scalp;
    }
}