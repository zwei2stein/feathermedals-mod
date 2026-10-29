using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TribalTrophies;

public class RaidStrategyWorker_ScalpHunt : RaidStrategyWorker_ImmediateAttack
{
    public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
    {
        if (!base.CanUseWith(parms, groupKind))
            return false;
        return ScalpHuntUtility.CanHunt(parms.faction, parms.target as Map);
    }

    protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidId)
    {
        // forced dev raid fix
        if (!parms.faction.HostileTo(Faction.OfPlayer) || !parms.attackTargets.NullOrEmpty())
            return base.MakeLordJob(parms, map, pawns, raidId);

        var tracker = ScalpHuntTracker.For(map);
        if (tracker.Get(raidId) == null)
        {
            var eligible = ScalpHuntUtility.EligibleVictimCount(map, parms.faction);
            var goal = ScalpHuntUtility.GoalFor(parms.pawnCount, eligible);
            tracker.Register(raidId, goal);

            if (Prefs.DevMode)
                Log.Message($"[TribalTrophies] Scalp hunt {raidId} by {parms.faction.Name}: {parms.pawnCount} raiders, {eligible} eligible colonists, goal {goal} scalps.");
        }

        return new LordJob_ScalpHunt(parms.faction, raidId);
    }
}

public class RaidStrategyWorker_ScalpHuntDropPods : RaidStrategyWorker_ScalpHunt
{
    public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
    {
        return TribalTrophiesMod.Settings.ScalpHuntDropPods && base.CanUseWith(parms, groupKind);
    }
}