using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FeatherMedals;

public static class RitualOutcomeTraitUtil
{
    private enum TraitOperation
    {
        Add,
        Remove
    }

    private readonly struct Candidate
    {
        public readonly MedalDynamicTrait Entry;
        public readonly TraitOperation Operation;

        public Candidate(MedalDynamicTrait entry, TraitOperation operation)
        {
            Entry = entry;
            Operation = operation;
        }
        
    }
    
    public static void GiveRandomTrait(Pawn pawn, FeatherMedal medal)
    {
        var candidates = BuildCandidates(pawn, medal).ToList();

        // there is chance of not doing anything.
        var totalChance = candidates.Sum(candidate => candidate.Entry.chance);
        if (!Rand.Chance(totalChance))
            return;
        
        var picked = candidates.RandomElementByWeight(candidate => candidate.Entry.chance);
        switch (picked.Operation)
        {
            case TraitOperation.Add:
                Add(pawn, picked, medal);
                break;
            case TraitOperation.Remove:
                Remove(pawn, picked, medal);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static IEnumerable<Candidate> BuildCandidates(Pawn pawn, FeatherMedal medal)
    {
        var ext = medal.def.GetModExtension<TrophyExtension>();
        if (ext == null)
            yield break;
        
        if (ext.removesTraits != null)
        {
            foreach (var entry in ext.removesTraits)
            {
                if (CanRemove(pawn.story.traits, entry))
                    yield return new Candidate(entry, TraitOperation.Remove);
            }
        }

        if (ext.addsTraits != null)
        {
            foreach (var entry in ext.addsTraits)
            {
                if (CanAdd(pawn.story.traits, entry))
                    yield return new Candidate(entry, TraitOperation.Add);
            }
        }
    }

    private static bool CanRemove(TraitSet traits, MedalDynamicTrait entry)
    {
        var existing = traits.allTraits.FirstOrDefault(t => t.def == entry.trait && t.Degree == entry.degree);
        if (existing == null)
            return false;

        if (existing.sourceGene != null || existing.ScenForced || existing.Suppressed)
            return false;

        return true;
    }

    private static bool CanAdd(TraitSet traits, MedalDynamicTrait entry)
    {
        if (traits.HasTrait(entry.trait))
            return false;

        var probe = new Trait(entry.trait, entry.degree);
        return !traits.allTraits.Any(t => t.def.ConflictsWith(probe));
    }

    private static void Remove(Pawn pawn, Candidate picked, FeatherMedal medal)
    {
        var existing =  pawn.story.traits.allTraits.FirstOrDefault(t => t.def == picked.Entry.trait && t.Degree == picked.Entry.degree);
        pawn.story.traits.RemoveTrait(existing);
        
        if (pawn.story.traits.allTraits.Contains(existing))
            return;

        Messages.Message(
            "FeatherMedals_TrophyRemovedTrait".Translate(pawn.Named("PAWN"), picked.Entry.Label.Named("TRAIT")),
            pawn,
            MessageTypeDefOf.PositiveEvent
        );
        medal.removedTrait = existing.def;
        medal.removedTraitDegree = existing.Degree;
    }

    private static void Add(Pawn pawn, Candidate picked, FeatherMedal medal)
    {
        var newTrait = new Trait(picked.Entry.trait, picked.Entry.degree);
        pawn.story.traits.GainTrait(newTrait);
        
        if (!pawn.story.traits.allTraits.Contains(newTrait))
            return;

        Messages.Message(
            "FeatherMedals_TrophyAddedTrait".Translate(pawn.Named("PAWN"), picked.Entry.Label.Named("TRAIT")),
            pawn,
            MessageTypeDefOf.PositiveEvent
        );
        medal.addedTrait = newTrait.def;
        medal.addedTraitDegree = newTrait.Degree;
    }

    private const int MEDALS_DECORATED = 3;
    private const int MEDALS_HONORED = 5;
    private const int MEDALS_EXALTED = 7;
    
    public static void UpdateDecoratedTrait(Pawn pawn)
    {
        var traits = pawn.story?.traits;
        if (traits == null || pawn.apparel == null)
            return;

        var medalCount = pawn.apparel.WornApparel.Count(a => a is FeatherMedal);
        var targetDegree = medalCount switch
        {
            >= MEDALS_EXALTED => 2,
            >= MEDALS_HONORED => 1,
            >= MEDALS_DECORATED => 0,
            _ => -1
        };
        if (targetDegree < 0)
            return;

        var def = FeatherMedalDefOf.FeatherMedals_Decorated;
        var existing = traits.GetTrait(def);
        if (existing != null && existing.Degree >= targetDegree)
            return;
        if (existing != null && (existing.sourceGene != null || existing.ScenForced))
            return;

        if (existing != null)
            traits.RemoveTrait(existing);

        var decorated = new Trait(def, targetDegree);
        traits.GainTrait(decorated);
        
        // we somehow failed to upgrade trait, lets readd original
        if (!traits.allTraits.Contains(decorated))
        {
            if (existing != null)
                traits.GainTrait(existing);
            return;
        }

        Messages.Message(
            "FeatherMedals_TrophyGrantedTrait".Translate(
                pawn.Named("PAWN"),
                def.DataAtDegree(targetDegree).label.CapitalizeFirst().Named("TRAIT")),
            pawn,
            MessageTypeDefOf.PositiveEvent
        );
    }
}