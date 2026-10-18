using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualOutcomeEffectWorkerAwardTrophy : RitualOutcomeEffectWorker
{
    public static bool ApplyingCeremonyAward;

    public RitualOutcomeEffectWorkerAwardTrophy()
    {
    }

    public RitualOutcomeEffectWorkerAwardTrophy(RitualOutcomeEffectDef def) : base(def)
    {
    }

    // normal completion reports 1, an interrupted ritual reports how far it got
    private const float MIN_PROGRESS = 0.99f;

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var previous = ApplyingCeremonyAward;
        ApplyingCeremonyAward = true;
        try { ApplyImpl(progress, totalPresence, jobRitual); }
        finally { ApplyingCeremonyAward = previous; }
    }

    private void ApplyImpl(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        if (jobRitual.selectedTarget.Thing is not FeatherMedal medal) return;
        var awardee = jobRitual.assignments.FirstAssignedPawn("awardee");
        var presenter = jobRitual.assignments.FirstAssignedPawn("leader");
        if (awardee == null || presenter == null) return;

        if (Prefs.DevMode)
            Log.Message($"[FeatherTrophies] Apply progress={progress:F3}");
        
        // Interrupted (raid, leader down...) or participants gone: no award, the medal stays as it was
        if (progress < MIN_PROGRESS)
            return;

        if (medal.Destroyed)
        {
            Messages.Message(
                "FeatherMedals_Ceremony_Failed_TrophyLost".Translate(awardee.Named("PAWN")),
                awardee,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (awardee.Dead || awardee.Destroyed || presenter.Dead
            || awardee.apparel == null || awardee.story == null)
            return;

        if (!FeatherMedalUtil.CanWearTrophy(awardee, medal))
        {
            Messages.Message(
                "FeatherMedals_Ceremony_Failed_CannotWear".Translate(awardee.Named("PAWN"), medal.MedalLabel.Named("TROPHY")),
                awardee,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        awardee.apparel.Wear(medal, false, MedalMod.Settings.LockTrophyUponAward);
        medal.awardedBy = new RememberedPawn(presenter);
        medal.awardedTick = Find.TickManager.TicksGame;
        medal.awardedTile = jobRitual.Map.Tile;
        
        TrophyCeremonyLedger.Instance?.Record(Find.TickManager.TicksGame);

        var attendees = AdorningQuality.CountAttendees(totalPresence.Keys, awardee);
        var totalColonists = AdorningQuality.CountPossibleAttendees(jobRitual.Map, awardee);
        var roomImpressiveness = AdorningQuality.GetRoomImpressiveness(jobRitual.selectedTarget);
        var trophyQualityBonus = AdorningQuality.GetTrophyQualityBonus(medal);
        
        var qualityScore = AdorningQuality.GetQualityScore(
            attendees,
            totalColonists,
            roomImpressiveness,
            medal.HasCitation,
            trophyQualityBonus);
        
        var stageIndex = AdorningQuality.GetStageIndex(qualityScore);
        var qualityLabel = AdorningQuality.GetQualityLabel(stageIndex);

        medal.ceremonyQuality = stageIndex;

        var awardedThought = FeatherMedalDefOf.FeatherMedals_AwardedTrophy_Thought;
        var memory = (Thought_Memory)ThoughtMaker.MakeThought(awardedThought, stageIndex);
        awardee.needs?.mood?.thoughts.memories.TryGainMemory(memory);

        var spectatorThought = FeatherMedalDefOf.FeatherMedals_WitnessedTrophyCeremony_Thought;
        if (spectatorThought != null)
        {
            foreach (var pawn in totalPresence.Keys)
            {
                if (pawn == awardee) continue;
                pawn.needs?.mood?.thoughts.memories.TryGainMemory(spectatorThought);
            }
        }

        if (MedalMod.Settings.TrophyDynamicTraits)
        {
            RitualOutcomeTraitUtil.UpdateDecoratedTrait(awardee, medal);
            RitualOutcomeTraitUtil.GiveRandomTrait(awardee, medal);
        }
        
        var letterLabel = "FeatherMedals_TakeOfHonorLetterLabel".Translate(medal.MedalLabel);
        var letterText = "FeatherMedals_TakeOfHonorLetterLine1".Translate(
            awardee.Named("PAWN"), medal.MedalLabel.Named("MEDAL"), presenter.Named("PRESENTER"));
        letterText += "\n\n";
        letterText += "FeatherMedals_TakeOfHonorLetterLine2"
            .Translate(awardee.Named("PAWN"), qualityLabel.Named("QUALITY"), attendees.Named("ATTENDEES") );
            
        if (medal.HasCitation)
        {
            letterText += "\n\n";
            letterText += "FeatherMedals_TakeOfHonorLetter".Translate(medal.citation);
        }
        
        var traitText = medal.GetTraitChangeText();
        if (!traitText.NullOrEmpty())
        {
            letterText += "\n\n";
            letterText += traitText;
        }

        Find.LetterStack.ReceiveLetter(
            label: letterLabel,
            text: letterText,
            LetterDefOf.PositiveEvent,
            lookTargets: awardee
        );

        TaleRecorder.RecordTale(FeatherMedalDefOf.FeatherMedals_AdornedTrophyTale, presenter, awardee);

        Find.WindowStack.Add(new Dialog_TrophyAwarded(medal, awardee));
    }
}