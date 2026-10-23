using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals;

public static class NpcHistoryUtility
{
    
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

        var tick = (int)(nowGame - (nowAbs - pickedAbs));
        // -1 means "no date" on a trophy
        return tick == -1 ? -2 : tick;
    }

    public static PlanetTile RandomSettlementTile(Faction faction)
    {
        var tiles = SettlementTiles(faction);
        return tiles.Count > 0 ? tiles.RandomElement() : FallbackTile();
    }

    public static List<PlanetTile> SettlementTiles(Faction faction)
    {
        var tiles = new List<PlanetTile>();
        if (faction == null || Current.Game == null)
            return tiles;

        var settlements = Find.WorldObjects.Settlements;
        for (var i = 0; i < settlements.Count; i++)
        {
            if (settlements[i].Faction == faction)
                tiles.Add(settlements[i].Tile);
        }
        return tiles;
    }

    public static PlanetTile FallbackTile()
    {
        if (Current.Game == null)
            return PlanetTile.Invalid;

        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        return map != null ? map.Tile : PlanetTile.Invalid;
    }
}