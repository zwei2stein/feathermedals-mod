using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FeatherMedals;

public class JobGiver_PresentTrophy : ThinkNode_JobGiver
{
    protected bool speech;
    protected SoundDef soundDefMale;
    protected SoundDef soundDefFemale;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var copy = (JobGiver_PresentTrophy)base.DeepCopy(resolve);
        copy.speech = speech;
        copy.soundDefMale = soundDefMale;
        copy.soundDefFemale = soundDefFemale;
        return copy;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.GetLord()?.LordJob is not LordJob_Ritual ritual)
            return null;
        if (ritual.selectedTarget.Thing is not FeatherMedal medal || medal.Destroyed)
            return null;

        var standCell = pawn.mindState.duty?.focus ?? LocalTargetInfo.Invalid;
        if (!standCell.IsValid)
            return null;

        var awardee = ritual.assignments.FirstAssignedPawn("awardee");
        if (awardee == null)
            return null;

        // not in hands yet: it must lie where we can get it
        var holding = pawn.carryTracker.CarriedThing == medal;
        if (!holding && (!medal.Spawned || !pawn.CanReserveAndReach(medal, PathEndMode.ClosestTouch, Danger.Deadly)))
            return null;

        var def = speech
            ? TribalTrophiesDefOf.TribalTrophies_PresentTrophySpeech
            : TribalTrophiesDefOf.TribalTrophies_PresentTrophy;

        var job = JobMaker.MakeJob(def, medal, standCell, awardee);
        job.count = 1;
        if (speech)
        {
            job.showSpeechBubbles = true;
            job.interaction = TribalTrophiesDefOf.TribalTrophies_Speech_AdornTrophy;
            job.speechSoundMale = soundDefMale ?? SoundDefOf.Speech_Leader_Male;
            job.speechSoundFemale = soundDefFemale ?? SoundDefOf.Speech_Leader_Female;
        }
        return job;
    }
}