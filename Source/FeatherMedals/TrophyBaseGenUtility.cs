using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

// How many trophy buildings a settlement gets, and how full.
public class SettlementSpawnExtension : DefModExtension
{
    public IntRange countRange = new(1, 2);
    public IntRange trophiesRange = new(0, 10);
    public TechLevel maxTechLevel = TechLevel.Medieval;
}

// Shared by trophy buildings placed in NPC settlements (scalp poles, tongue baskets).
public static class TrophyBaseGenUtility
{
    // Free cells where the building can stand, indoors or outdoors.
    public static List<IntVec3> FreeSpots(CellRect rect, Map map, ThingDef def)
    {
        var spots = new List<IntVec3>();
        foreach (var cell in rect)
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetDoor(map) != null)
                continue;
            if (cell.GetFirstBuilding(map) != null || cell.GetFirstItem(map) != null || cell.GetFirstPawn(map) != null)
                continue;
            if (!GenConstruct.CanBuildOnTerrain(def, cell, map, Rot4.North))
                continue;
            spots.Add(cell);
        }
        return spots;
    }

    public static IntVec3 FarthestFromUsed(List<IntVec3> spots, List<IntVec3> usedSpots)
    {
        return spots.MaxBy(spot =>
        {
            var nearest = float.PositiveInfinity;
            foreach (var used in usedSpots)
            {
                var distance = used.DistanceTo(spot);
                if (distance < nearest)
                    nearest = distance;
            }
            return nearest;
        });
    }

    // An adult of the faction whose ideo allows taking trophies; null when there is none on the map.
    public static Pawn RandomTaker(Map map, Faction faction)
    {
        return map.mapPawns.SpawnedPawnsInFaction(faction)
            .Where(pawn => pawn.RaceProps.Humanlike
                           && pawn.DevelopmentalStage.Adult()
                           && ScalpingPrecepts.StanceOf(pawn) != ScalpStance.Forbidden)
            .RandomElementWithFallback();
    }
}
