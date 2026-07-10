using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FeatherMedals;

public class JobGiver_AwardSpeech : JobGiver_GiveSpeechFacingTarget
{
    private static readonly AccessTools.FieldRef<InteractionDef, Texture2D> SymbolTexRef =
        AccessTools.FieldRefAccess<InteractionDef, Texture2D>("symbolTex");

    protected override Job TryGiveJob(Pawn pawn)
    {
        LordJob_Ritual lordJob = pawn.GetLord()?.LordJob as LordJob_Ritual;
        if (lordJob == null) return null;
        lordJob.Ritual.outcomeEffect ??= FeatherMedalDefOf.FeatherMedals_AwardTrophyOutcome.GetInstance();
        lordJob.Ritual.outcomeEffect.compDatas ??= new();
        var awardee = lordJob.assignments.FirstAssignedPawn("awardee");
        if (awardee is not { Spawned: true }) return null;
        var targetB = (LocalTargetInfo)awardee;
        var job = JobMaker.MakeJob(JobDefOf.GiveSpeech, (LocalTargetInfo)pawn.Position, targetB);
        job.showSpeechBubbles = true;
        job.speechFaceSpectatorsIfPossible = this.faceSpectatorsIfPossible;
        var interactDef = FeatherMedalDefOf.FeatherMedals_Speech_AwardMedal;
        if (lordJob.selectedTarget.Thing is FeatherMedal medal)
        {
            var medalTexture = medal.def.uiIcon;
            if (medalTexture != null)
                SymbolTexRef(interactDef) = medalTexture;
        }
        job.interaction = interactDef;
        job.speechSoundMale = this.soundDefMale ?? SoundDefOf.Speech_Leader_Male;
        job.speechSoundFemale = this.soundDefFemale ?? SoundDefOf.Speech_Leader_Female;
        return job;
    }
}