using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals
{

    [DefOf]
    public class FeatherMedalDefOf
    {
        public static RitualPatternDef FeatherMedal_AwardTrophyPattern;
        
        public static JobDef FeatherTrophies_PresentTrophy;
        public static JobDef FeatherTrophies_PresentTrophySpeech;
        public static JobDef FeatherTrophies_AwaitAdorning;

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
        
        public static PreceptDef IdeoRole_ShootingSpecialist;
        public static PreceptDef IdeoRole_MeleeSpecialist;
        public static PreceptDef IdeoRole_ResearchSpecialist;
        public static PreceptDef IdeoRole_MedicalSpecialist;


        //Scapling:

        public static HediffDef FeatherTrophies_Scalping_Scalped;
        public static ThingDef FeatherTrophies_Scalping_Scalp;
        public static JobDef FeatherTrophies_Scalping_ScalpJob;
        public static JobDef FeatherTrophies_Scalping_ScalpInBreakJob;
        public static DesignationDef FeatherTrophies_Scalping_ScalpDesignation;
        public static ThoughtDef FeatherTrophies_Scalping_ScalpedMe;
        public static ThoughtDef FeatherTrophies_Scalping_ClaimedRightOfVictor;
        public static ThoughtDef FeatherTrophies_Scalping_WasScalped;
        public static HistoryEventDef FeatherTrophies_Scalping_ScalpedMember;
        public static HistoryEventDef FeatherTrophies_Scalping_TookScalp;

        public static HistoryEventDef FeatherTrophies_Scalping_Observed_Saw_Scalped;
        public static HistoryEventDef FeatherTrophies_Scalping_Observed_Scalp;
        public static HistoryEventDef FeatherTrophies_Scalping_Observed_ScalpPole;
        public static HistoryEventDef FeatherTrophies_Scalping_Observed_ScalpBelt;

        public static IssueDef FeatherTrophies_ScalpTaking;
        public static PreceptDef FeatherTrophies_ScalpTaking_Merciless;
        public static PreceptDef FeatherTrophies_ScalpTaking_Honourable;
        public static PreceptDef FeatherTrophies_ScalpTaking_Forbidden;

        public static HairDef Messy;

        public static MentalStateDef MurderousRage;

        public static ThingDef FeatherTrophies_ScalpPole;
        
        public static JobDef TribalTrophies_Scalping_TakeScalpAndFlee;
        public static DutyDef TribalTrophies_Scalping_ScalpHuntDuty;
        
        public static TaleDef TribalTrophies_Scalping_TookScalp;
        public static TaleDef TribalTrophies_Scalping_ScalpedByEnemy;

        static FeatherMedalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FeatherMedalDefOf));
    }
    
    public static class SpeakerRolesUtil
    {
   
        private static List<PreceptDef> speakerRoles;
        private static string speakerRolesLabel;

        private static List<PreceptDef> SpeakerRoles => speakerRoles ??= new List<PreceptDef>
        {
            PreceptDefOf.IdeoRole_Leader,
            PreceptDefOf.IdeoRole_Moralist,
            FeatherMedalDefOf.IdeoRole_ShootingSpecialist,
            FeatherMedalDefOf.IdeoRole_MeleeSpecialist,
            FeatherMedalDefOf.IdeoRole_ResearchSpecialist,
            FeatherMedalDefOf.IdeoRole_MedicalSpecialist
        };

        public static string SpeakerRolesLabel => speakerRolesLabel ??=
            string.Join(", ", SpeakerRoles.Select(role => role.LabelCap.Resolve()));

        public static bool IsSpeaker(Pawn pawn)
        {
            var role = pawn.Ideo?.GetRole(pawn);
            if (role == null)
                return false;

            return SpeakerRoles.Contains(role.def);
        }
        
    }

}