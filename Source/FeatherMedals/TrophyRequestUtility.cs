using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Rules shared by quests where a faction asks for body part trophies of its enemies (scalps, dried tongues).
public static class TrophyRequestUtility
{
    private const int MaxTechDistance = 1;

    // reused list, main thread only
    private static readonly List<Faction> tmpTargets = new();

    public static bool CanAsk(Faction faction)
    {
        if (!ScalpingPrecepts.FactionPracticesScalping(faction, TechLevel.Undefined, TribalTrophiesMod.Settings.ScalpRequestMaxTech))
            return false;
        if (faction.Hidden || faction.defeated)
            return false;
        if (faction.def.permanentEnemy || !faction.HasGoodwill)
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

    // One open request per faction, whatever trophy it asks for.
    public static bool HasOpenRequest(Faction faction)
    {
        foreach (var quest in Find.QuestManager.QuestsListForReading)
        {
            if (quest.Historical)
                continue;
            foreach (var part in quest.PartsListForReading)
            {
                if (part is QuestPart_InitiateTrophyRequest request && request.settlement?.Faction == faction)
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

    public static bool IsTrophyOf<T>(Thing thing, Faction target) where T : TrophyBodyPart
    {
        return target != null && thing is T trophy && trophy.Victim?.Faction == target;
    }

    public static IEnumerable<TrophyRequestComp> RequestsOf(Settlement settlement)
    {
        foreach (var comp in settlement.AllComps)
        {
            if (comp is TrophyRequestComp request)
                yield return request;
        }
    }

    // Thing goes to a request of the settlement, not counted as a gift.
    public static bool AnyRequestWants(Settlement settlement, Thing thing)
    {
        foreach (var request in RequestsOf(settlement))
        {
            if (request.Wants(thing))
                return true;
        }
        return false;
    }
}
