using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

public static class RitualOutcomeTraitUtility
{
    // Trait chance multiplier (awful x0.5, legendary x2)
    private const float TrophyTraitChanceStep = 0.25f;

    public static float GetTrophyTraitChanceMultiplier(Thing trophy)
    {
        return trophy.TryGetQuality(out var quality)
            ? 1f + ((int)quality - (int)QualityCategory.Normal) * TrophyTraitChanceStep
            : 1f;
    }
    
    public static void GiveRandomTrait(Pawn pawn, FeatherTrophy trophy)
    {
        var candidates = BuildCandidates(pawn, trophy).ToList();

        // there is chance of not doing anything. Quality scales the total, weights between candidates stay the same.
        var qualityMultiplier = GetTrophyTraitChanceMultiplier(trophy);
        var totalChance = candidates.Sum(candidate => candidate.Entry.chance) * qualityMultiplier;

        if (Prefs.DevMode)
        {
            Log.Message($"[TribalTrophies] GiveRandomTrait.totalChance={totalChance:P0} (quality x{qualityMultiplier:F2}), candidates={candidates.Count}");
            candidates.ForEach(
                candidate => Log.Message($"[TribalTrophies] GiveRandomTrait.candidate={candidate}")
            );
        }

        if (!Rand.Chance(totalChance))
            return;
        
        var picked = candidates.RandomElementByWeight(candidate => candidate.Entry.chance);
        switch (picked.Operation)
        {
            case TrophyTraitOperation.Add:
                Add(pawn, picked, trophy);
                break;
            case TrophyTraitOperation.Remove:
                Remove(pawn, picked, trophy);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static IEnumerable<TrophyTraitChange> BuildCandidates(Pawn pawn, FeatherTrophy trophy)
    {
        var ext = trophy.def.GetModExtension<TrophyExtension>();
        if (ext == null)
            yield break;

        foreach (var change in ext.TraitChanges())
        {
            var possible = change.Operation == TrophyTraitOperation.Add
                ? CanAdd(pawn.story.traits, change.Entry)
                : CanRemove(pawn.story.traits, change.Entry);
            if (possible)
                yield return change;
        }
    }

    private static bool CanRemove(TraitSet traits, TrophyDynamicTrait entry)
    {
        var existing = traits.allTraits.FirstOrDefault(t => t.def == entry.trait && t.Degree == entry.degree);
        if (existing == null)
            return false;

        if (existing.sourceGene != null || existing.ScenForced || existing.Suppressed)
            return false;

        return true;
    }

    private static bool CanAdd(TraitSet traits, TrophyDynamicTrait entry)
    {
        if (traits.HasTrait(entry.trait))
            return false;

        var probe = new Trait(entry.trait, entry.degree);
        return !traits.allTraits.Any(t => t.def.ConflictsWith(probe));
    }

    private static void Remove(Pawn pawn, TrophyTraitChange picked, FeatherTrophy trophy)
    {
        var existing =  pawn.story.traits.allTraits.FirstOrDefault(t => t.def == picked.Entry.trait && t.Degree == picked.Entry.degree);
        pawn.story.traits.RemoveTrait(existing);
        
        if (pawn.story.traits.allTraits.Contains(existing))
            return;
        
        trophy.removedTrait = existing.def;
        trophy.removedTraitDegree = existing.Degree;

        Messages.Message(
            trophy.GetRemovedTraitLabel(),
            pawn,
            MessageTypeDefOf.PositiveEvent
        );

    }

    private static void Add(Pawn pawn, TrophyTraitChange picked, FeatherTrophy trophy)
    {
        var newTrait = new Trait(picked.Entry.trait, picked.Entry.degree);
        pawn.story.traits.GainTrait(newTrait);
        
        if (!pawn.story.traits.allTraits.Contains(newTrait))
            return;
        
        trophy.addedTrait = newTrait.def;
        trophy.addedTraitDegree = newTrait.Degree;

        Messages.Message(
            trophy.GetAddedTraitLabel(),
            pawn,
            MessageTypeDefOf.PositiveEvent
        );
    }

    private const int TROPHIES_DECORATED = 3;
    private const int TROPHIES_HONORED = 5;
    private const int TROPHIES_EXALTED = 7;
    
    public static void UpdateDecoratedTrait(Pawn pawn, FeatherTrophy trophy)
    {
        var traits = pawn.story?.traits;
        if (traits == null || pawn.apparel == null)
            return;

        var trophyCount = pawn.apparel.WornApparel.Count(a => a is FeatherTrophy);
        var targetDegree = trophyCount switch
        {
            >= TROPHIES_EXALTED => 2,
            >= TROPHIES_HONORED => 1,
            >= TROPHIES_DECORATED => 0,
            _ => -1
        };
        if (targetDegree < 0)
            return;

        var def = TribalTrophiesDefOf.TribalTrophies_Decorated;
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

        trophy.decoratedDegree = targetDegree;
        
        if (pawn.Faction is { IsPlayer: true })
            Messages.Message(
                trophy.GetDecoratedTraitLabel(),
                pawn,
                MessageTypeDefOf.PositiveEvent);
    }
}