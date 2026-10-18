using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;

namespace FeatherMedals;

public static class CitationUtil
{
    
    public static string Generate(Faction faction, Pawn pawn = null, PlanetTile tile = default)
    {
        var rulePack = FeatherMedalDefOf.FeatherMedals_NpcCitation;
        if (rulePack == null)
            return null;

        var request = default(GrammarRequest);
        request.Includes.Add(rulePack);
        request.Rules.Add(new Rule_String("faction_name", FactionName(faction)));
        request.Rules.Add(new Rule_String("settlement_name", SettlementName(faction, tile)));
        request.Rules.Add(new Rule_String("pawn_name", PawnName(pawn)));
        request.Rules.Add(new Rule_String("pawn_adj", PawnPossessive(pawn)));
        return GrammarResolver.Resolve("r_root", request, "FeatherMedals_NpcCitation").CapitalizeFirst();
    }

    // Citation dialog: player faction, colony on the map being looked at
    public static string GenerateForPlayer()
    {
        var map = Find.CurrentMap;
        return Generate(Faction.OfPlayer, null, map != null ? map.Tile : PlanetTile.Invalid);
    }

    private static string FactionName(Faction faction)
    {
        var name = faction?.Name;
        return name.NullOrEmpty() ? "TribalTrophies_AdorningCeremony_Misc_TaleDefaultFaction".Translate().ToString() : name;
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

    private static string PawnName(Pawn pawn)
    {
        var name = pawn?.Name?.ToStringShort;
        return name.NullOrEmpty() ? "TribalTrophies_AdorningCeremony_Misc_TaleDefaultName".Translate().ToString() : name;
    }

    private static string PawnPossessive(Pawn pawn)
    {
        if (pawn == null || pawn.gender == Gender.None)
            return "TribalTrophies_AdorningCeremony_Misc_TaleDefaultPossessive".Translate().ToString();
        return pawn.gender.GetPossessive();
    }
}