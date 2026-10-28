using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals;

// A made-up victim for trophies NPCs already own; discard it once its details are taken.
public static class NpcVictimUtility
{
    public static Pawn GenerateTemporaryVictim(Faction attackerFaction)
    {
        var victimFaction = PickVictimFaction(attackerFaction);

        // vanilla decides how the victim looked: xenotype from the faction's profile, skin from genes, hair from styles
        return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            victimFaction.def.basicMemberKind ?? PawnKindDefOf.Slave,
            victimFaction,
            PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            //fix recursion of generating worn scalp/skull, which can then also generate pawns that might need to generate worn scalps/skulls ...
            forceNoGear: true,
            developmentalStages: DevelopmentalStage.Adult));
    }

    public static void Discard(Pawn victim)
    {
        Find.WorldPawns.PassToWorld(victim, PawnDiscardDecideMode.Discard);
    }

    // vanilla gibbet cage (SymbolResolver_TerrorBuildings)
    private static Faction PickVictimFaction(Faction attackerFaction)
    {
        return Find.FactionManager.GetFactions(allowNonHumanlike: false)
            .Where(faction => !faction.IsPlayer && faction.HostileTo(attackerFaction))
            .RandomElementWithFallback(attackerFaction);
    }
}