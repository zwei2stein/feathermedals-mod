using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class EventTime : IExposable
{
    public int tick;
    public PlanetTile tile = PlanetTile.Invalid;

    // for loading
    public EventTime()
    {
    }

    public EventTime(int tick, PlanetTile tile)
    {
        this.tick = tick;
        this.tile = tile;
    }

    public static EventTime Now(Map map) => new(Find.TickManager.TicksGame, map?.Tile ?? PlanetTile.Invalid);

    public int TicksAgo => Find.TickManager.TicksGame - tick;

    public string DateLabel
    {
        get
        {
            var longLat = tile.Valid ? Find.WorldGrid.LongLatOf(tile) : Vector2.zero;
            return GenDate.DateFullStringAt(GenDate.TickGameToAbs(tick), longLat);
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref tick, "tick", 0);
        Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
    }
}