using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

public enum TrophyTraitOperation
{
    Add,
    Remove
}

// one trait this trophy may grant or remove
public readonly struct TrophyTraitChange
{
    public readonly TrophyDynamicTrait Entry;
    public readonly TrophyTraitOperation Operation;

    public TrophyTraitChange(TrophyDynamicTrait entry, TrophyTraitOperation operation)
    {
        Entry = entry;
        Operation = operation;
    }

    public override string ToString()
    {
        return $"{Operation}: {Entry}";
    }
}

public class TrophyExtension : DefModExtension
{
    public List<TrophyDynamicTrait> addsTraits;
    public List<TrophyDynamicTrait> removesTraits;

    public IEnumerable<TrophyTraitChange> TraitChanges()
    {
        if (addsTraits != null)
            foreach (var entry in addsTraits)
                yield return new TrophyTraitChange(entry, TrophyTraitOperation.Add);
        if (removesTraits != null)
            foreach (var entry in removesTraits)
                yield return new TrophyTraitChange(entry, TrophyTraitOperation.Remove);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in CheckList(addsTraits, nameof(addsTraits)))
            yield return error;
        foreach (var error in CheckList(removesTraits, nameof(removesTraits)))
            yield return error;
    }

    private static IEnumerable<string> CheckList(List<TrophyDynamicTrait> list, string listName)
    {
        if (list == null)
            yield break;

        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];

            if (entry == null)
            {
                yield return $"{listName}[{i}] is an empty entry";
                continue;
            }

            if (entry.trait == null)
            {
                yield return $"{listName}[{i}] has no trait (typo, or trait from a mod that is not loaded, MayRequire missing?)";
                continue;
            }

            var degrees = entry.trait.degreeDatas;
            if (degrees == null || !degrees.Any(d => d.degree == entry.degree))
            {
                var available = degrees == null ? "none" : string.Join(", ", degrees.Select(d => d.degree));
                yield return $"{listName}[{i}] trait {entry.trait.defName} has no degree {entry.degree} (available: {available})";
            }

            if (entry.chance <= 0f)
            {
                yield return $"{listName}[{i}] trait {entry.trait.defName} has chance {entry.chance}, must be > 0";
            }
        }
    }
}

public class TrophyDynamicTrait
{
    public TraitDef trait;
    public int degree = 0;
    public float chance = 1.0f;
    
    public string Label
    {
        get
        {
            var data = trait?.DataAtDegree(degree);
            return data?.label ?? trait?.defName ?? "unknown";
        }
    }
    
    public override string ToString()
    {
        return $"{trait?.defName ?? "unknown"} [{Label}], Degree={degree}, Chance={chance:P0}";
    }
}