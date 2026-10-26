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
        public static RitualPatternDef TribalTrophies_AdorningCeremony_Pattern;
        
        public static JobDef TribalTrophies_PresentTrophy;
        public static JobDef TribalTrophies_PresentTrophySpeech;
        public static JobDef TribalTrophies_AwaitAdorning;

        public static PreceptDef TribalTrophies_AdorningCeremonyPrecept;

        public static ThoughtDef TribalTrophies_Trophy_Feather_Adorning_Thought_AdornedTrophy;
        public static ThoughtDef TribalTrophies_Trophy_Feather_Adorning_Thought_Witnessed;

        public static TraitDef TribalTrophies_Decorated;

        public static TaleDef TribalTrophies_AdornedTrophyTale;

        public static InteractionDef TribalTrophies_Speech_AdornTrophy;

        public static PreceptDef TribalTrophies_Issue_HonoringWarriors_Neutral;
        public static PreceptDef TribalTrophies_Issue_HonoringWarriors_Forbidden;
        public static PreceptDef TribalTrophies_Issue_HonoringWarriors_Disapproved;
        public static PreceptDef TribalTrophies_Issue_HonoringWarriors_Desired;
        public static PreceptDef TribalTrophies_Issue_HonoringWarriors_Required;

        public static IssueDef TribalTrophies_Issue_HonoringWarriors;

        public static RulePackDef TribalTrophies_Trophy_Feather_Adorning_NpcCitation;

        public static BackstoryDef TribalTrophies_Backstory_DisgracedElder;
        public static BackstoryDef TribalTrophies_Backstory_HonoredElder;
        public static BackstoryDef TribalTrophies_Backstory_FeatheredWarrior;
        public static BackstoryDef TribalTrophies_Backstory_PraisedWarrior;
        public static BackstoryDef TribalTrophies_Backstory_PraisedChild;
        
        public static PreceptDef IdeoRole_ShootingSpecialist;
        public static PreceptDef IdeoRole_MeleeSpecialist;
        public static PreceptDef IdeoRole_ResearchSpecialist;
        public static PreceptDef IdeoRole_MedicalSpecialist;

        public static PreceptDef Skullspike_Desired;

        //Scapling:

        public static HediffDef TribalTrophies_Scalping_Scalped;
        public static ThingDef TribalTrophies_Trophy_Scalping_Scalp;
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
        public static PreceptDef FeatherTrophies_ScalpTaking_Honorable;
        public static PreceptDef FeatherTrophies_ScalpTaking_Forbidden;

        public static HairDef Messy;

        public static MentalStateDef MurderousRage;

        public static ThingDef FeatherTrophies_ScalpPole;
        
        public static JobDef TribalTrophies_Scalping_TakeScalpAndFlee;
        public static DutyDef TribalTrophies_Scalping_ScalpHuntDuty;
        
        public static TaleDef TribalTrophies_Scalping_TookScalp;
        public static TaleDef TribalTrophies_Scalping_ScalpedByEnemy;

        // Trophy belts:
        
        public static DesignationDef TribalTrophies_Apparel_Belt_Designation_Disassemble;
        public static JobDef TribalTrophies_Apparel_Belt_Job_Disassemble;
        public static WorkGiverDef TribalTrophies_Apparel_Belt_WorkGiver_Disassemble;
        
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