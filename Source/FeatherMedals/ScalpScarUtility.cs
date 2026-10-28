using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public static class ScalpScarUtility
{
    // An old scalping: healed into a scar, by somebody who was an adult back then.
    // A real world pawn when one fits, so the player can meet them; otherwise a made-up one.
    public static void MakeOld(Hediff_Scalped scar, Pawn victim, BackstorySlot when)
    {
        var momentAbs = NpcHistoryUtility.RandomPastAbsTickIn(victim, when);

        var scalper = PickScalper(victim, momentAbs);
        if (scalper != null)
        {
            scar.scalper = new RememberedPawn(scalper);
        }
        else
        {
            var madeUp = MakeUpScalper(victim);
            if (madeUp != null)
            {
                scar.scalper = new RememberedPawn(madeUp);
                scar.scalper.DiscardPawn();
                NpcVictimUtility.Discard(madeUp);
            }
        }

        scar.BecomeScar();
        scar.ageTicks = System.Math.Max(1, (int)((long)GenTicks.TicksAbs - momentAbs));
    }

    private static Pawn PickScalper(Pawn victim, long momentAbs)
    {
        var faction = victim.Faction;
        var worldPawns = Current.Game?.World?.worldPawns;
        if (faction == null || worldPawns == null)
            return null;

        var pool = new List<Pawn>();
        var all = worldPawns.AllPawnsAlive;
        for (var i = 0; i < all.Count; i++)
        {
            var other = all[i];
            var otherFaction = other.Faction;
            if (otherFaction == null || otherFaction == faction || otherFaction.IsPlayer || otherFaction.Hidden)
                continue;
            if (!otherFaction.HostileTo(faction))
                continue;
            if (!other.RaceProps.Humanlike || !other.DevelopmentalStage.Adult())
                continue;
            // already an adult when it happened
            if (!NpcHistoryUtility.WasAdultAt(other, momentAbs))
                continue;
            pool.Add(other);
        }
        return pool.Count > 0 ? pool.RandomElement() : null;
    }

    private static Pawn MakeUpScalper(Pawn victim)
    {
        // no enemies to pick from, or already making up a pawn
        if (victim.Faction == null || NpcVictimUtility.IsGeneratingTemporary)
            return null;

        return NpcVictimUtility.GenerateTemporaryPerpetrator(victim.Faction);
    }
}