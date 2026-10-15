using RimWorld;
using Verse;
using Verse.Grammar;

namespace FeatherMedals;

public static class CitationUtil
{
    
    public static string Generate(Faction faction)
    {
        var rulePack = FeatherMedalDefOf.FeatherMedals_NpcCitation;
        if (rulePack == null)
            return null;

        var request = new GrammarRequest();
        request.Includes.Add(rulePack);
        
        request.Rules.Add(new Rule_String("faction_name", faction?.Name ?? string.Empty));
        
        return GrammarResolver.Resolve("r_root", request, "FeatherMedals_NpcCitation").CapitalizeFirst();
    }
}