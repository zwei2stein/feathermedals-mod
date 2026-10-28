using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class BackstoryHediffExtension : DefModExtension
{
    public List<BackstoryHediff> hediffs = new();

    public override IEnumerable<string> ConfigErrors()
    {
        for (var i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i]?.def == null)
                yield return $"hediffs[{i}] has no def (typo, or hediff from a mod that is not loaded, MayRequire missing?)";
        }
    }
}

// same fields as vanilla StartingHediff, plus the part
public class BackstoryHediff
{
    public HediffDef def;
    public BodyPartDef part; // null: whole body
    public float? severity;
    public float? chance;
}

public interface IBackstoryHediff
{
    void PreAddFromBackstory(Pawn pawn, BackstoryDef backstory);
}

public static class BackstoryHediffUtility
{
    public static void Apply(Pawn pawn)
    {
        if (pawn.story == null || pawn.health == null)
            return;

        ApplyFrom(pawn, pawn.story.Childhood);
        ApplyFrom(pawn, pawn.story.Adulthood);
    }

    private static void ApplyFrom(Pawn pawn, BackstoryDef backstory)
    {
        var extension = backstory?.GetModExtension<BackstoryHediffExtension>();
        if (extension == null)
            return;

        foreach (var entry in extension.hediffs)
        {
            if (entry?.def == null)
                continue;
            if (entry.chance.HasValue && !Rand.Chance(entry.chance.Value))
                continue;
            if (pawn.health.hediffSet.HasHediff(entry.def))
                continue;

            // a wanted part the pawn does not have: nothing to put it on
            BodyPartRecord part = null;
            if (entry.part != null)
            {
                part = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def == entry.part);
                if (part == null)
                    continue;
            }

            var hediff = HediffMaker.MakeHediff(entry.def, pawn, part);
            if (entry.severity.HasValue)
                hediff.Severity = entry.severity.Value;
            (hediff as IBackstoryHediff)?.PreAddFromBackstory(pawn, backstory);
            pawn.health.AddHediff(hediff, part);
        }
    }
}

[HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
public static class Patch_PawnGenerator_GenerateInitialHediffs
{
    public static void Postfix(Pawn pawn)
    {
        try
        {
            BackstoryHediffUtility.Apply(pawn);
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to add backstory hediffs to {pawn?.LabelShort}: {ex}", 1948204);
        }
    }
}