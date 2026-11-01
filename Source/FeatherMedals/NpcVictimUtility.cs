using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TribalTrophies;

// A made-up victim for trophies NPCs already own; discard it once its details are taken.
public static class NpcVictimUtility
{
    // a made-up pawn is being generated right now; nested ones would make up more and more pawns
    public static bool IsGeneratingTemporary => generatingDepth > 0;

    private static int generatingDepth;

    public static Pawn GenerateTemporaryVictim(Faction attackerFaction)
    {
        var victimFaction = PickVictimFaction(attackerFaction);

        generatingDepth++;
        try
        {
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
        finally
        {
            generatingDepth--;
        }
    }

    // just a factional enemy
    public static Pawn GenerateTemporaryPerpetrator(Faction victimFaction)
    {
        return GenerateTemporaryVictim(victimFaction);
    }

    public static void Discard(Pawn victim)
    {
        Find.WorldPawns.PassToWorld(victim, PawnDiscardDecideMode.Discard);
    }

    // A body part trophy from a made-up victim of the taker's enemies; the trophy keeps only the victim's details.
    public static T MakeTrophyFromTemporaryVictim<T>(Faction takerFaction, Pawn taker, Func<Pawn, Pawn, EventTime, T> create)
        where T : TrophyBodyPart
    {
        var victim = GenerateTemporaryVictim(takerFaction);

        // taken while both were adults, where the victim's people live
        var trophy = create(victim, taker, NpcHistoryUtility.RandomPastEvent(victim.Faction, taker, victim));
        trophy.Victim.DiscardPawn();

        Discard(victim);
        return trophy;
    }

    // vanilla gibbet cage (SymbolResolver_TerrorBuildings)
    private static Faction PickVictimFaction(Faction attackerFaction)
    {
        return Find.FactionManager.GetFactions(allowNonHumanlike: false)
            .Where(faction => !faction.IsPlayer && faction.HostileTo(attackerFaction))
            .RandomElementWithFallback(attackerFaction);
    }
}