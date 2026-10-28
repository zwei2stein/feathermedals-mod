using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualOutcomeEffectWorkerAdornTrophy : RitualOutcomeEffectWorker
{
    public static bool ApplyingCeremonyAdorning;

    public RitualOutcomeEffectWorkerAdornTrophy()
    {
    }

    public RitualOutcomeEffectWorkerAdornTrophy(RitualOutcomeEffectDef def) : base(def)
    {
    }

    // normal completion reports 1, an interrupted ritual reports how far it got
    private const float MIN_PROGRESS = 0.99f;

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var previous = ApplyingCeremonyAdorning;
        ApplyingCeremonyAdorning = true;
        try { ApplyImpl(progress, totalPresence, jobRitual); }
        finally { ApplyingCeremonyAdorning = previous; }
    }

    private void ApplyImpl(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        if (jobRitual.selectedTarget.Thing is not FeatherTrophy featherTrophy) return;
        var adorned = jobRitual.assignments.FirstAssignedPawn(RitualRole_Adorned.ROLE_ID);
        var presenter = jobRitual.assignments.FirstAssignedPawn("leader");
        if (adorned == null || presenter == null) return;

        if (Prefs.DevMode)
            Log.Message($"[TribalTrophies] Apply progress={progress:F3}");
        
        // Interrupted (raid, leader down...) or participants gone: no adorning, the trophy stays as it was
        if (progress < MIN_PROGRESS)
            return;

        if (featherTrophy.Destroyed)
        {
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_TrophyLost".Translate(adorned.Named("PAWN")),
                adorned,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (adorned.Dead || adorned.Destroyed || presenter.Dead
            || adorned.apparel == null || adorned.story == null)
            return;

        if (!FeatherTrophyUtil.CanWearTrophy(adorned, featherTrophy))
        {
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_CannotWear".Translate(adorned.Named("PAWN"), featherTrophy.TrophyLabel.Named("TROPHY")),
                adorned,
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (presenter.carryTracker?.CarriedThing == featherTrophy)
            presenter.carryTracker.innerContainer.Remove(featherTrophy);

        adorned.apparel.Wear(featherTrophy, false, TribalTrophiesMod.Settings.LockTrophyUponAdorning);

        // Wear failed, drop trophy
        if (!adorned.apparel.Wearing(featherTrophy))
        {
            if (!featherTrophy.Spawned && featherTrophy.holdingOwner == null)
                GenPlace.TryPlaceThing(featherTrophy, adorned.PositionHeld, adorned.MapHeld, ThingPlaceMode.Near);
            Messages.Message(
                "TribalTrophies_AdorningCeremony_Message_CannotWear".Translate(adorned.Named("PAWN"), featherTrophy.TrophyLabel.Named("TROPHY")),
                adorned,
                MessageTypeDefOf.NegativeEvent);
            return;
        }
        
        featherTrophy.adornedBy = new RememberedPawn(presenter);
        featherTrophy.adorned = EventTime.Now(jobRitual.Map);

        TrophyCeremonyLedger.Instance?.Record(featherTrophy.adorned.tick);

        var attendees = AdorningQuality.CountAttendees(totalPresence.Keys, adorned);
        var totalColonists = AdorningQuality.CountPossibleAttendees(jobRitual.Map, adorned);
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

        var memory = (Thought_Memory)ThoughtMaker.MakeThought(TribalTrophiesDefOf.TribalTrophies_Trophy_Feather_Adorning_Thought_AdornedTrophy, stageIndex);
        adorned.needs?.mood?.thoughts.memories.TryGainMemory(memory);

        foreach (var pawn in totalPresence.Keys)
        {
            if (pawn == adorned) continue;
            pawn.needs?.mood?.thoughts.memories.TryGainMemory(TribalTrophiesDefOf.TribalTrophies_Trophy_Feather_Adorning_Thought_Witnessed);
        }

        if (TribalTrophiesMod.Settings.TrophyDynamicTraits)
        {
            RitualOutcomeTraitUtil.UpdateDecoratedTrait(adorned, featherTrophy);
            RitualOutcomeTraitUtil.GiveRandomTrait(adorned, featherTrophy);
        }
        
        var letterLabel = "TribalTrophies_AdorningCeremony_Letter_Label".Translate(featherTrophy.TrophyLabel.Named("TROPHY"));
        var letterText = "TribalTrophies_AdorningCeremony_Letter_Adorned".Translate(
            adorned.Named("PAWN"),
            featherTrophy.TrophyLabel.Named("TROPHY"),
            presenter.Named("PRESENTER"));
        letterText += "\n\n";
        letterText += "TribalTrophies_AdorningCeremony_Letter_Attendance"
            .Translate(adorned.Named("PAWN"), qualityLabel.Named("QUALITY"), attendees.Named("ATTENDEES") );
            
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
            lookTargets: adorned
        );

        TaleRecorder.RecordTale(TribalTrophiesDefOf.TribalTrophies_AdornedTrophyTale, presenter, adorned);

        Find.WindowStack.Add(new Dialog_TrophyAdorned(featherTrophy, adorned));
    }
}