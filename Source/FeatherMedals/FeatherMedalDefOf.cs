using RimWorld;
using Verse;

namespace FeatherMedals;

[DefOf]
public class FeatherMedalDefOf
{
    public static RitualPatternDef FeatherMedal_AwardTrophyPattern;

    public static PreceptDef FeatherMedals_MedalCeremonyPrecept;
    
    public static ThoughtDef FeatherMedals_AwardedTrophy_Thought;
    public static ThoughtDef FeatherMedals_WitnessedTrophyCeremony_Thought;

    public static TraitDef FeatherMedals_Decorated;

    public static TaleDef FeatherMedals_AdornedTrophyTale;
    
    public static InteractionDef FeatherMedals_Speech_AwardMedal;
    
    public static PreceptDef FeatherMedals_HonoringWarriors_Neutral;
    public static PreceptDef FeatherMedals_HonoringWarriors_Forbidden;
    public static PreceptDef FeatherMedals_HonoringWarriors_Disapproved;
    public static PreceptDef FeatherMedals_HonoringWarriors_Desired;
    public static PreceptDef FeatherMedals_HonoringWarriors_Required;

    public static IssueDef FeatherMedals_HonoringWarriors;

    public static RulePackDef FeatherMedals_NpcCitation;

    public static BackstoryDef FeatherTrophy_Backstory_DisgracedElder;
    public static BackstoryDef FeatherTrophy_Backstory_HonoredElder;
    public static BackstoryDef FeatherTrophy_Backstory_FeatheredWarrior;
    public static BackstoryDef FeatherTrophy_Backstory_PraisedWarrior;
    public static BackstoryDef FeatherTrophy_Backstory_PraisedChild;
    
    static FeatherMedalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (FeatherMedalDefOf));
}