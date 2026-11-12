using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;

namespace TribalTrophies;

public static class TaleUtility
{
    
    public static string Generate(TrophyKindDef kind, Faction faction, Pawn pawn = null, PlanetTile tile = default)
    {
        var rulePack = kind.taleOfHonorRulePack;
        if (rulePack == null)
            return null;

        var request = default(GrammarRequest);
        request.Includes.Add(rulePack);
        request.Rules.Add(new Rule_String("faction_name", FactionName(kind, faction)));
        request.Rules.Add(new Rule_String("settlement_name", SettlementName(faction, tile)));
        request.Rules.Add(new Rule_String("pawn_name", PawnName(kind, pawn)));
        request.Rules.Add(new Rule_String("pawn_adj", PawnPossessive(pawn)));
        request.Rules.Add(new Rule_String("community", kind.communityLabel));
        request.Rules.Add(new Rule_String("honoree", kind.honoreeLabel));
        request.Rules.Add(new Rule_String("honoree_indefinite", kind.HonoreeIndefinite));
        return GrammarResolver.Resolve("r_root", request, rulePack.defName).CapitalizeFirst();
    }

    // Tale dialog: player faction, colony on the map being looked at
    public static string GenerateForPlayer(TrophyKindDef kind)
    {
        var map = Find.CurrentMap;
        return Generate(kind, Faction.OfPlayer, null, map != null ? map.Tile : PlanetTile.Invalid);
    }

    private static string FactionName(TrophyKindDef kind, Faction faction)
    {
        var name = faction?.Name;
        return name.NullOrEmpty() ? kind.communityLabel.CapitalizeFirst() : name;
    }

    private static string SettlementName(Faction faction, PlanetTile tile)
    {
        if (faction != null && tile.Valid && Current.Game != null)
        {
            var settlement = Find.WorldObjects.SettlementAt(tile);
            if (settlement != null && settlement.Faction == faction && !settlement.Name.NullOrEmpty())
                return settlement.Name;
        }
        return "TribalTrophies_AdorningCeremony_Misc_TaleDefaultSettlement".Translate().ToString();
    }

    private static string PawnName(TrophyKindDef kind, Pawn pawn)
    {
        var name = pawn?.Name?.ToStringShort;
        return name.NullOrEmpty() ? kind.honoreeLabel.CapitalizeFirst() : name;
    }

    private static string PawnPossessive(Pawn pawn)
    {
        if (pawn == null || pawn.gender == Gender.None)
            return "TribalTrophies_AdorningCeremony_Misc_TaleDefaultPossessive".Translate().ToString();
        return pawn.gender.GetPossessive();
    }
}