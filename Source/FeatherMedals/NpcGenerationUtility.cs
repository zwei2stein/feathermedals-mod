using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public static class NpcGenerationUtility
{
    
    public static bool IsGeneratedNpc(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || pawn.ageTracker == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        return pawn.Faction is not { IsPlayer: true };
    }

    public static FactionDef FactionDefFor(Pawn pawn)
    {
        return pawn.Faction?.def ?? pawn.kindDef?.defaultFactionDef;
    }

    public static float TechScaledChance(Pawn pawn, float baseChance, TechLevel zeroChanceTechLevel)
    {
        var factionDef = FactionDefFor(pawn);
        if (factionDef == null || factionDef.techLevel == TechLevel.Undefined)
            return 0f;

        var span = (float)(zeroChanceTechLevel - TechLevel.Animal);
        var steps = (float)(zeroChanceTechLevel - factionDef.techLevel);
        return baseChance * Mathf.Clamp01(steps / span);
    }
}
