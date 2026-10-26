using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FeatherMedals;

public class JobDriver_PresentTrophy : JobDriver
{
    private static readonly AccessTools.FieldRef<InteractionDef, Texture2D> SymbolTexRef =
        AccessTools.FieldRefAccess<InteractionDef, Texture2D>("symbolTex");
    
    private const TargetIndex TrophyInd = TargetIndex.A;
    private const TargetIndex StandInd = TargetIndex.B;
    private const TargetIndex AwardeeInd = TargetIndex.C;

    //vanilla speech
    private const int SocialInteractionInterval = 480;

    private int ticksTillSocialInteraction = 60;

    private FeatherTrophy Trophy => job.GetTarget(TrophyInd).Thing as FeatherTrophy;
    
    private Pawn Awardee => job.GetTarget(AwardeeInd).Pawn;
    
    private bool Speech => job.def == TribalTrophiesDefOf.TribalTrophies_PresentTrophySpeech;
    
    private bool HoldingTrophy => Trophy != null && pawn.carryTracker.CarriedThing == Trophy;
    
    private bool InCeremony => pawn.GetLord()?.LordJob is LordJob_Ritual ritual && ritual.selectedTarget.Thing == Trophy;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (HoldingTrophy)
            return true;
        
        return pawn.Reserve(job.GetTarget(TrophyInd), job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => Trophy == null || Trophy.Destroyed);
        this.FailOn(() => !HoldingTrophy && !Trophy.Spawned);
        this.FailOn(() => !InCeremony);
        AddFinishAction(_ => DropTrophyIfCeremonyOver());

        var gotoStand = Toils_Goto.GotoCell(StandInd, PathEndMode.OnCell);

        // already in hands from the previous stage
        yield return Toils_Jump.JumpIf(gotoStand, () => HoldingTrophy);
        yield return Toils_General.Do(DropOtherCarriedThing);
        yield return Toils_Goto.GotoThing(TrophyInd, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TrophyInd);
        yield return gotoStand;
        yield return Toils_General.Do(MarkArrived);
        yield return Speech ? SpeechToil() : HoldToil();
    }
    
    private void DropTrophyIfCeremonyOver()
    {
        if (HoldingTrophy && !InCeremony)
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
    }

    private void DropOtherCarriedThing()
    {
        var carried = pawn.carryTracker.CarriedThing;
        if (carried != null && carried != Trophy)
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
    }

    private void MarkArrived()
    {
        if (pawn.GetLord()?.LordJob is LordJob_Ritual ritual)
            ritual.AddTagForPawn(pawn, StageEndTrigger_RolesArrived.ArrivalTag);
    }

    private void FaceAwardee()
    {
        var awardee = Awardee;
        if (awardee is { Spawned: true })
            pawn.rotationTracker.FaceTarget(awardee);
    }

    private Toil HoldToil()
    {
        var toil = ToilMaker.MakeToil(nameof(HoldToil));
        toil.FailOn(() => !HoldingTrophy);
        toil.tickIntervalAction = _ => FaceAwardee();
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.handlingFacing = true;
        return toil;
    }

    private Toil SpeechToil()
    {
        var toil = ToilMaker.MakeToil(nameof(SpeechToil));
        toil.FailOn(() => !HoldingTrophy);
        toil.tickIntervalAction = delta =>
        {
            pawn.GainComfortFromCellIfPossible(delta);
            pawn.skills?.Learn(SkillDefOf.Social, 0.3f * delta);
            if (ticksTillSocialInteraction <= 0)
            {
                if (job.showSpeechBubbles)
                    MoteMaker.MakeSpeechBubble(pawn, JobDriver_GiveSpeech.moteIcon);
                if (job.interaction != null && pawn.GetLord()?.LordJob is LordJob_Ritual ritual)
                    ImitateWithTrophySymbol(ritual);
                ticksTillSocialInteraction = SocialInteractionInterval;
            }
            FaceAwardee();
            ticksTillSocialInteraction -= delta;
        };
        toil.PlaySustainerOrSound(() => pawn.gender != Gender.Female ? job.speechSoundMale : job.speechSoundFemale,
            pawn.story.VoicePitchFactor);
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.handlingFacing = true;
        return toil;
    }
    
    private void ImitateWithTrophySymbol(LordJob_Ritual ritual)
    {
        var interaction = job.interaction;
        var original = SymbolTexRef(interaction);
        var trophyIcon = Trophy.def.uiIcon;
        if (trophyIcon != null)
            SymbolTexRef(interaction) = trophyIcon;
        try
        {
            SocialInteractionUtility.ImitateSocialInteractionWithManyPawns(pawn, ritual.lord.ownedPawns, interaction);
        }
        finally
        {
            SymbolTexRef(interaction) = original;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksTillSocialInteraction, "ticksTillSocialInteraction", 60);
    }
}