using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class ScalpRequestUtility
{
    private const int MaxTechDistance = 1;

    // reused list, main thread only
    private static readonly List<Faction> tmpTargets = new();

    public static bool CanAsk(Faction faction)
    {
        if (faction == null || faction.IsPlayer || faction.Hidden || faction.defeated || !faction.def.humanlikeFaction)
            return false;
        if (faction.def.permanentEnemy || !faction.HasGoodwill)
            return false;
        if (faction.def.techLevel > TribalTrophiesMod.Settings.ScalpRequestMaxTech)
            return false;
        if (ScalpingPrecepts.StanceOf(faction.ideos?.PrimaryIdeo) == ScalpStance.Forbidden)
            return false;
        if (HasOpenRequest(faction))
            return false;
        return TryFindTarget(faction, out _);
    }

    public static bool CanBeTarget(Faction target, Faction asker)
    {
        return target != null
               && target != asker
               && !target.IsPlayer
               && !target.Hidden
               && !target.defeated
               && target.def.humanlikeFaction
               && asker.HostileTo(target);
    }

    public static bool TryFindTarget(Faction asker, out Faction target)
    {
        tmpTargets.Clear();
        foreach (var faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (CanBeTarget(faction, asker)
                && Mathf.Abs((int)faction.def.techLevel - (int)asker.def.techLevel) <= MaxTechDistance)
                tmpTargets.Add(faction);
        }
        if (tmpTargets.Count == 0)
        {
            foreach (var faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (CanBeTarget(faction, asker))
                    tmpTargets.Add(faction);
            }
        }
        var found = tmpTargets.TryRandomElement(out target);
        tmpTargets.Clear();
        return found;
    }

    public static bool HasOpenRequest(Faction faction)
    {
        foreach (var quest in Find.QuestManager.QuestsListForReading)
        {
            if (quest.Historical)
                continue;
            foreach (var part in quest.PartsListForReading)
            {
                if (part is QuestPart_InitiateScalpRequest request && request.settlement?.Faction == faction)
                    return true;
            }
        }
        return false;
    }

    public static int Stars(Faction target, int count, int extraStarAboveCount)
    {
        int stars;
        if (target.def.permanentEnemy)
            stars = 1;
        else if (target.HostileTo(Faction.OfPlayer))
            stars = 2;
        else if (target.PlayerRelationKind == FactionRelationKind.Neutral)
            stars = 3;
        else
            stars = 4;

        if (count > extraStarAboveCount)
            stars++;
        return stars;
    }

    public static bool IsScalpOf(Thing thing, Faction target)
    {
        return target != null && thing is Scalp scalp && scalp.Victim?.Faction == target;
    }
}