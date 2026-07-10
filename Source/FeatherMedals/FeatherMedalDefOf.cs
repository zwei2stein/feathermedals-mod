using RimWorld;

namespace FeatherMedals;

[DefOf]
public class FeatherMedalDefOf
{
    public static RitualPatternDef FeatherMedal_AwardTrophyPattern;

    public static PreceptDef FeatherMedals_MedalCeremonyPrecept;

    public static RitualOutcomeEffectDef FeatherMedals_AwardTrophyOutcome;
    
    public static ThoughtDef FeatherMedals_AwardedTrophy_Thought;
    public static ThoughtDef FeatherMedals_WitnessedTrophyCeremony_Thought;

    public static TraitDef FeatherMedals_Decorated;

    public static TaleDef FeatherMedals_AdornedTrophyTale;
    
    public static InteractionDef FeatherMedals_Speech_AwardMedal;
    
    static FeatherMedalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (FeatherMedalDefOf));
}