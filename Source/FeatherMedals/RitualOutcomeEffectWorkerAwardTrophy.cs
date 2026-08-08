using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class RitualOutcomeEffectWorkerAwardTrophy : RitualOutcomeEffectWorker
{
    [System.ThreadStatic]
    public static bool ApplyingCeremonyAward;

    public RitualOutcomeEffectWorkerAwardTrophy() => InitializeSafety();

    public RitualOutcomeEffectWorkerAwardTrophy(RitualOutcomeEffectDef def) : base(def) => InitializeSafety();

    private void InitializeSafety()
    {
        this.def ??= FeatherMedalDefOf.FeatherMedals_AwardTrophyOutcome;
        if (this.def is { comps: null }) this.def.comps = [];
    }

    private const int MEDALS_DECORATED = 3;
    private const int MEDALS_HONORED = 5;
    private const int MEDALS_EXHALTED = 7;

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
            
        var ext = medal.def.GetModExtension<TrophyExtension>();
        if (ext is not null && ModsConfig.RoyaltyActive && Faction.OfEmpire != null) 
            awardee.royalty.GainFavor(Faction.OfEmpire, ext.honorAwarded);

        if (MedalMod.Settings.TrophyDynamicTraits)
        {
            var medalCount = awardee.apparel.WornApparel.Count(a => a is FeatherMedal);

            // Determine target degree based on medal count
            var targetDegree = medalCount switch
            {
                >= MEDALS_EXHALTED => 2,
                >= MEDALS_HONORED => 1,
                >= MEDALS_DECORATED => 0,
                _ => -1
            };
            if (targetDegree >= 0)
            {
                var existing = awardee.story.traits.GetTrait(FeatherMedalDefOf.FeatherMedals_Decorated);
                if (existing is null)
                {
                    // No decorated trait yet, grant it
                    awardee.story.traits.GainTrait(new Trait(FeatherMedalDefOf.FeatherMedals_Decorated, targetDegree));
                    var label = FeatherMedalDefOf.FeatherMedals_Decorated.DataAtDegree(targetDegree).label.CapitalizeFirst();
                    Messages.Message(
                        "FeatherMedals_TrophyGrantedTrait".Translate(awardee.Named("PAWN"), label.Named("TRAIT")),
                        awardee,
                        MessageTypeDefOf.PositiveEvent
                    );
                }
                else if (targetDegree > existing.Degree)
                {
                    // Already has the trait but at a lower tier, upgrade it
                    awardee.story.traits.RemoveTrait(existing);
                    awardee.story.traits.GainTrait(new Trait(FeatherMedalDefOf.FeatherMedals_Decorated, targetDegree));
                    var label = FeatherMedalDefOf.FeatherMedals_Decorated.DataAtDegree(targetDegree).label.CapitalizeFirst();
                    Messages.Message(
                        "FeatherMedals_TrophyGrantedTrait".Translate(awardee.Named("PAWN"), label.Named("TRAIT")),
                        awardee,
                        MessageTypeDefOf.PositiveEvent
                    );
                }
            }

            if (ext?.removesTraits != null)
            {
                foreach (var entry in ext.removesTraits)
                {
                    if (!Rand.Chance(entry.chance)) continue;

                    // Find the trait on the pawn that matches both def AND degree
                    var existing = awardee.story.traits.allTraits
                        .FirstOrDefault(t => t.def == entry.trait && t.Degree == entry.degree);

                    if (existing == null) continue;

                    awardee.story.traits.RemoveTrait(existing);
                    Messages.Message(
                        "FeatherMedals_TrophyRemovedTrait".Translate(awardee.Named("PAWN"), entry.Label.Named("TRAIT")),
                        awardee,
                        MessageTypeDefOf.PositiveEvent
                    );
                    medal.removedTrait = existing.def;
                    medal.removedTraitDegree = existing.Degree;
                }
            }

            if (ext?.addsTraits != null)
            {
                foreach (var entry in ext.addsTraits)
                {
                    if (!Rand.Chance(entry.chance)) continue;

                    // Skip if pawn already has this exact trait+degree
                    if (awardee.story.traits.allTraits
                        .Any(t => t.def == entry.trait && t.Degree == entry.degree))
                        continue;

                    // Check for conflicts with existing traits
                    var newTrait = new Trait(entry.trait, entry.degree);
                    if (awardee.story.traits.allTraits.Any(t => t.def.ConflictsWith(newTrait)))
                        continue;

                    awardee.story.traits.GainTrait(newTrait);
                    Messages.Message(
                        "FeatherMedals_TrophyAddedTrait".Translate(awardee.Named("PAWN"), entry.Label.Named("TRAIT")),
                        awardee,
                        MessageTypeDefOf.PositiveEvent
                    );
                    medal.addedTrait = newTrait.def;
                    medal.addedTraitDegree = newTrait.Degree;
                }
            }
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