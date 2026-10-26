using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals
{

    [DefOf]
    public class TribalTrophiesDefOf
    {
        public static RitualPatternDef TribalTrophies_AdorningCeremony_Pattern;
        
        public static JobDef TribalTrophies_PresentTrophy;
        public static JobDef TribalTrophies_PresentTrophySpeech;
        public static JobDef TribalTrophies_Job_AwaitAdorning;

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

        public static HediffDef TribalTrophies_Scalping_Hediff_Scalped;
        public static ThingDef TribalTrophies_Trophy_Scalping_Scalp;
        public static JobDef TribalTrophies_Scalping_Job_ScalpJob;
        public static JobDef TribalTrophies_Scalping_Job_ScalpInBreakJob;
        public static DesignationDef TribalTrophies_Scalping_Designation_ScalpDesignation;
        public static ThoughtDef TribalTrophies_Scalping_ThoughtSocial_ScalpedMe;
        public static ThoughtDef TribalTrophies_Issue_ScalpTaking_Thought_Merciless_ClaimedRightOfVictor;
        public static ThoughtDef TribalTrophies_Scalping_Thought_IWasScalped;
        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_ScalpedMember;
        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_TookScalp;

        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_Observed_Saw_Scalped;
        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Item_Scalp;
        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Building_Pole;
        public static HistoryEventDef TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Item_Belt;

        public static IssueDef TribalTrophies_Issue_ScalpTaking;
        public static PreceptDef TribalTrophies_Issue_ScalpTaking_Merciless;
        public static PreceptDef TribalTrophies_Issue_ScalpTaking_Honorable;
        public static PreceptDef TribalTrophies_Issue_ScalpTaking_Forbidden;

        public static HairDef Messy;

        public static MentalStateDef MurderousRage;

        public static ThingDef TribalTrophies_Scalping_Building_ScalpPole;
        
        public static JobDef TribalTrophies_Scalping_TakeScalpAndFlee;
        public static DutyDef TribalTrophies_Scalping_ScalpHuntDuty;
        
        public static TaleDef TribalTrophies_Scalping_Tale_TookScalp;
        public static TaleDef TribalTrophies_Scalping_Tale_ScalpedByEnemy;

        // Trophy belts:
        
        public static DesignationDef TribalTrophies_Apparel_Belt_Designation_Disassemble;
        public static JobDef TribalTrophies_Apparel_Belt_Job_Disassemble;
        public static WorkGiverDef TribalTrophies_Apparel_Belt_WorkGiver_Disassemble;
        
        static TribalTrophiesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TribalTrophiesDefOf));
    }
    
    public static class SpeakerRolesUtil
    {
   
        private static List<PreceptDef> speakerRoles;
        private static string speakerRolesLabel;

        private static List<PreceptDef> SpeakerRoles => speakerRoles ??= new List<PreceptDef>
        {
            PreceptDefOf.IdeoRole_Leader,
            PreceptDefOf.IdeoRole_Moralist,
            TribalTrophiesDefOf.IdeoRole_ShootingSpecialist,
            TribalTrophiesDefOf.IdeoRole_MeleeSpecialist,
            TribalTrophiesDefOf.IdeoRole_ResearchSpecialist,
            TribalTrophiesDefOf.IdeoRole_MedicalSpecialist
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