using RimWorld;
using Verse;

namespace TribalTrophies;

public enum TrophyRule
{
    None,
    Honorable,
    Merciless
}

public static class TrophyRuleUtility
{
    public static TrophyRule RuleFor(Pawn pawn, Pawn taker)
    {
        var stance = ScalpingPrecepts.StanceOf(taker);
        
        if (stance == ScalpStance.Forbidden)
            return TrophyRule.None;

        if (TrophyBodyPartUtility.IsNotEvilToTakeFrom(pawn, taker.Faction ?? Faction.OfPlayer))
            return TrophyRule.Honorable;
        
        if (stance == ScalpStance.Merciless)
            return TrophyRule.Merciless;

        return TrophyRule.None;
    }
    
    public static TrophyRule RuleForColony(Pawn pawn)
    {
        if (TrophyBodyPartUtility.IsNotEvilToTakeFrom(pawn, Faction.OfPlayer))
            return TrophyRule.Honorable;

        if (ScalpingPrecepts.AnyMercilessColonist())
            return TrophyRule.Merciless;

        return TrophyRule.None;
    }

    public static string LabelSuffix(TrophyRule rule)
    {
        return rule switch
        {
            TrophyRule.Honorable => " (" + TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Honorable.label + ")",
            TrophyRule.Merciless => " (" + TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Merciless.label + ")",
            _ => ""
        };
    }

    public static string Description(TrophyRule rule)
    {
        return rule switch
        {
            TrophyRule.Honorable => TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Honorable.LabelCap.Resolve() + ": "
                                    + "TribalTrophies_TrophyBodyPart_Rule_Honorable_Desc".Translate().Resolve(),
            TrophyRule.Merciless => TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Merciless.LabelCap.Resolve() + ": "
                                    + "TribalTrophies_TrophyBodyPart_Rule_Merciless_Desc".Translate().Resolve(),
            _ => ""
        };
    }
}
