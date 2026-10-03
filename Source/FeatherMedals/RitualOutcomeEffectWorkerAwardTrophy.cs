using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualOutcomeEffectWorkerAwardTrophy : RitualOutcomeEffectWorker
{
    [System.ThreadStatic]
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
        ApplyingCeremonyAward = true;
        try { ApplyImpl(progress, totalPresence, jobRitual); }
        finally { ApplyingCeremonyAward = false; }
    }

    private void ApplyImpl(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        if (jobRitual.selectedTarget.Thing is not FeatherMedal medal) return;
        var awardee = jobRitual.assignments.FirstAssignedPawn("awardee");
        var presenter = jobRitual.assignments.FirstAssignedPawn("leader");
        if (awardee == null || presenter == null) return;

        if (Prefs.DevMode)
            Log.Message($"[FeatherMedals] Apply progress={progress:F3}");
        
        // Interrupted (raid, leader down...) or participants gone: no award, the medal stays as it was
        if (progress < MIN_PROGRESS || medal.Destroyed
            || awardee.Dead || awardee.Destroyed || presenter.Dead
            || awardee.apparel == null || awardee.story == null)
            return;

        if (medal.Spawned) medal.DeSpawn();
        awardee.apparel.Wear(medal, false, false);
        medal.isLocked = MedalMod.Settings.LockTrophyUponAward;
        medal.awardedBy = presenter;
        medal.awardedTick = Find.TickManager.TicksGame;

        var attendees = totalPresence.Count;
        var totalColonists = jobRitual.Map.mapPawns.FreeColonistsSpawnedCount;
        var roomImpressiveness = AdorningQuality.GetRoomImpressiveness(jobRitual.selectedTarget);
        var hasCitation = !medal.citation.NullOrEmpty();

        var qualityScore = AdorningQuality.GetQualityScore(
            attendees, totalColonists, roomImpressiveness, hasCitation);
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
            RitualOutcomeTraitUtil.UpdateDecoratedTrait(awardee);
            RitualOutcomeTraitUtil.GiveRandomTrait(awardee, medal);
        }

        var medalName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            GenLabel.ThingLabel(medal.def, medal.Stuff, 1));

        var letterLabel = "FeatherMedals_TakeOfHonorLetterLabel".Translate(medalName);
        var letterText = "FeatherMedals_TakeOfHonorLetterLine1".Translate(
            awardee.Named("PAWN"), medalName.Named("MEDAL"), presenter.Named("PRESENTER"));
        letterText += "\n\n";
        letterText += "FeatherMedals_TakeOfHonorLetterLine2"
            .Translate(awardee.Named("PAWN"), qualityLabel.Named("QUALITY"), attendees.Named("ATTENDEES") );
            
        if (!medal.citation.NullOrEmpty())
        {
            letterText += "\n\n";
            letterText += "FeatherMedals_TakeOfHonorLetter".Translate(medal.citation);
        }

        Find.LetterStack.ReceiveLetter(
            label: letterLabel,
            text: letterText,
            LetterDefOf.PositiveEvent,
            lookTargets: awardee
        );

        TaleRecorder.RecordTale(FeatherMedalDefOf.FeatherMedals_AdornedTrophyTale, presenter, awardee);

        Find.WindowStack.Add(new Dialog_TrophyAwarded(medal, awardee, presenter));
    }
}