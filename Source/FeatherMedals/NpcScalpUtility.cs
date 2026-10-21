using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals;

public static class NpcScalpUtility
{
    public static Scalp MakeScalp(Faction scalperFaction, Pawn scalper = null)
    {
        var victimFaction = PickVictimFaction(scalperFaction);

        // vanilla decides how the victim looked: xenotype from the faction's profile, skin from genes, hair from styles
        var victim = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            victimFaction.def.basicMemberKind ?? PawnKindDefOf.Slave,
            victimFaction,
            PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            developmentalStages: DevelopmentalStage.Adult));

        var scalp = Scalp.CreateFromTemporaryVictim(victim, scalper);
        
        Find.WorldPawns.PassToWorld(victim, PawnDiscardDecideMode.Discard);
        return scalp;
    }

    // vanilla gibbet cage (SymbolResolver_TerrorBuildings)
    private static Faction PickVictimFaction(Faction scalperFaction)
    {
        return Find.FactionManager.GetFactions(allowNonHumanlike: false)
            .Where(faction => !faction.IsPlayer && faction.HostileTo(scalperFaction))
            .RandomElementWithFallback(scalperFaction);
    }
}