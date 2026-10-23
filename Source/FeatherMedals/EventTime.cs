using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class EventTime : IExposable
{
    public int tick;
    public PlanetTile tile = PlanetTile.Invalid;

    private string settlementName;
    private Faction settlementFaction;

    // for loading
    public EventTime()
    {
    }

    public EventTime(int tick, PlanetTile tile, Settlement settlement)
    {
        this.tick = tick;
        this.tile = tile;
        if (settlement != null)
        {
            settlementName = settlement.Name;
            settlementFaction = settlement.Faction;
        }
    }

    public static EventTime Now(Map map) => new(Find.TickManager.TicksGame, map?.Tile ?? PlanetTile.Invalid, map?.Parent as Settlement);

    public int TicksAgo => Find.TickManager.TicksGame - tick;

    public string DateLabel
    {
        get
        {
            var longLat = tile.Valid ? Find.WorldGrid.LongLatOf(tile) : Vector2.zero;
            return GenDate.DateFullStringAt(GenDate.TickGameToAbs(tick), longLat);
        }
    }

    public string SettlementLabel
    {
        get
        {
            if (settlementName.NullOrEmpty())
                return null;
            return settlementFaction != null ? settlementName.Colorize(settlementFaction.Color) : settlementName;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref tick, "tick", 0);
        Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
        Scribe_Values.Look(ref settlementName, "settlementName");
        Scribe_References.Look(ref settlementFaction, "settlementFaction");
    }
}