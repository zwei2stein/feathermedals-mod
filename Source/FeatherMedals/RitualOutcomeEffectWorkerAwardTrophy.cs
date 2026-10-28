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
        if (jobRitual.selectedTarget.Thing is not FeatherTrophy featherTrophy) return;
        var awardee = jobRitual.assignments.FirstAssignedPawn("awardee");
        var presenter = jobRitual.assignments.FirstAssignedPawn("leader");
        if (awardee == null || presenter == null) return;

        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] Apply progress={progress:F3}");
        
        // Interrupted (raid, leader down...) or participants gone: no award, the trophy stays as it was
        if (progress < MIN_PROGRESS)
            return;

        if (featherTrophy.Destroyed)
        {
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_TrophyLost".Translate(awardee.Named("PAWN")),
                awardee,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (awardee.Dead || awardee.Destroyed || presenter.Dead
            || awardee.apparel == null || awardee.story == null)
            return;

        if (!FeatherTrophyUtil.CanWearTrophy(awardee, featherTrophy))
        {
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_CannotWear".Translate(awardee.Named("PAWN"), featherTrophy.TrophyLabel.Named("TROPHY")),
                awardee,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (presenter.carryTracker?.CarriedThing == featherTrophy)
            presenter.carryTracker.innerContainer.Remove(featherTrophy);

        awardee.apparel.Wear(featherTrophy, false, TribalTrophiesMod.Settings.LockTrophyUponAdorning);

        // Wear failed, drop trophy
        if (!awardee.apparel.Wearing(featherTrophy))
        {
            if (!featherTrophy.Spawned && featherTrophy.holdingOwner == null)
                GenPlace.TryPlaceThing(featherTrophy, awardee.PositionHeld, awardee.MapHeld, ThingPlaceMode.Near);
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_CannotWear".Translate(awardee.Named("PAWN"), featherTrophy.TrophyLabel.Named("TROPHY")),
                awardee,
                MessageTypeDefOf.NegativeEvent);
            return;
        }
        
        featherTrophy.adornedBy = new RememberedPawn(presenter);
        featherTrophy.adorned = EventTime.Now(jobRitual.Map);

        TrophyCeremonyLedger.Instance?.Record(featherTrophy.adorned.tick);

        var attendees = AdorningQuality.CountAttendees(totalPresence.Keys, awardee);
        var totalColonists = AdorningQuality.CountPossibleAttendees(jobRitual.Map, awardee);
        var roomImpressiveness = AdorningQuality.GetRoomImpressiveness(jobRitual.selectedTarget);
        var trophyQualityBonus = AdorningQuality.GetTrophyQualityBonus(featherTrophy);
        
        var qualityScore = AdorningQuality.GetQualityScore(
            attendees,
            totalColonists,
            roomImpressiveness,
            featherTrophy.HasTale,
            trophyQualityBonus);
        
        var stageIndex = AdorningQuality.GetStageIndex(qualityScore);
        var qualityLabel = AdorningQuality.GetQualityLabel(stageIndex);

        featherTrophy.ceremonyQuality = stageIndex;

        var awardedThought = TribalTrophiesDefOf.TribalTrophies_Trophy_Feather_Adorning_Thought_AdornedTrophy;
        var memory = (Thought_Memory)ThoughtMaker.MakeThought(awardedThought, stageIndex);
        awardee.needs?.mood?.thoughts.memories.TryGainMemory(memory);

        var spectatorThought = TribalTrophiesDefOf.TribalTrophies_Trophy_Feather_Adorning_Thought_Witnessed;
        if (spectatorThought != null)
        {
            foreach (var pawn in totalPresence.Keys)
            {
                if (pawn == awardee) continue;
                pawn.needs?.mood?.thoughts.memories.TryGainMemory(spectatorThought);
            }
        }

        if (TribalTrophiesMod.Settings.TrophyDynamicTraits)
        {
            RitualOutcomeTraitUtil.UpdateDecoratedTrait(awardee, featherTrophy);
            RitualOutcomeTraitUtil.GiveRandomTrait(awardee, featherTrophy);
        }
        
        var letterLabel = "TribalTrophies_AdorningCeremony_Letter_Label".Translate(featherTrophy.TrophyLabel.Named("TROPHY"));
        var letterText = "TribalTrophies_AdorningCeremony_Letter_Adorned".Translate(
            awardee.Named("PAWN"),
            featherTrophy.TrophyLabel.Named("TROPHY"),
            presenter.Named("PRESENTER"));
        letterText += "\n\n";
        letterText += "TribalTrophies_AdorningCeremony_Letter_Attendance"
            .Translate(awardee.Named("PAWN"), qualityLabel.Named("QUALITY"), attendees.Named("ATTENDEES") );
            
        if (featherTrophy.HasTale)
        {
            letterText += "\n\n";
            letterText += "TribalTrophies_AdorningCeremony_Letter_Tale".Translate(featherTrophy.tale);
        }
        
        var traitText = featherTrophy.GetTraitChangeText();
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

        TaleRecorder.RecordTale(TribalTrophiesDefOf.TribalTrophies_AdornedTrophyTale, presenter, awardee);

        Find.WindowStack.Add(new Dialog_TrophyAwarded(featherTrophy, awardee));
    }
}