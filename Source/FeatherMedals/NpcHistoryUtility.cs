using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals;

public static class NpcHistoryUtility
{
    public static EventTime RandomPastEvent(Faction placeFaction, params Pawn[] pawns)
    {
        var tick = RandomPastTick(pawns);
        var settlements = Settlements(placeFaction);

        if (settlements.Count == 0)
            return new EventTime(tick, FallbackTile(), null);

        var settlement = settlements.RandomElement();
        return new EventTime(tick, settlement.Tile, settlement);
    }

    public static int RandomPastTick(params Pawn[] pawns)
    {
        var nowAbs = (long)GenTicks.TicksAbs;
        var nowGame = GenTicks.TicksGame;

        // the latest adulthood of the given pawns; a missing pawn does not limit it
        long? adultAbs = null;
        foreach (var pawn in pawns)
        {
            if (pawn?.ageTracker == null)
                continue;
            var becameAdult = pawn.ageTracker.BirthAbsTicks + pawn.ageTracker.AdultMinAgeTicks;
            if (adultAbs == null || becameAdult > adultAbs.Value)
                adultAbs = becameAdult;
        }
        if (adultAbs == null)
            return nowGame;

        var pickedAbs = adultAbs.Value >= nowAbs
            ? nowAbs
            : adultAbs.Value + (long)(Rand.Value * (nowAbs - adultAbs.Value));

        return (int)(nowGame - (nowAbs - pickedAbs));
    }

    public static List<Settlement> Settlements(Faction faction)
    {
        var result = new List<Settlement>();
        if (faction == null || Current.Game == null)
            return result;

        var settlements = Find.WorldObjects.Settlements;
        for (var i = 0; i < settlements.Count; i++)
        {
            if (settlements[i].Faction == faction)
                result.Add(settlements[i]);
        }
        return result;
    }

    public static PlanetTile FallbackTile()
    {
        if (Current.Game == null)
            return PlanetTile.Invalid;

        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        return map != null ? map.Tile : PlanetTile.Invalid;
    }
}