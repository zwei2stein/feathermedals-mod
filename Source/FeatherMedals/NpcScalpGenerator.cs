using RimWorld;
using Verse;

namespace TribalTrophies;

public class NpcScalpedExtension : DefModExtension
{
    public float chance = 0.05f;
    public TechLevel zeroChanceTechLevel = TechLevel.Spacer;
}

public static class NpcScalpGenerator
{
    public static NpcScalpedExtension Tuning => field ??=
        TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped.GetModExtension<NpcScalpedExtension>() ?? new NpcScalpedExtension();

    public static void OnGearGenerated(Pawn pawn)
    {
        try
        {
            if (!Eligible(pawn))
                return;

            if (Rand.Chance(ChanceFor(pawn)))
                AddScar(pawn);
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate scalped pawn {pawn?.LabelShort}: {ex}", 1948203);
        }
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!TribalTrophiesMod.Settings.GenerateNpcScalped)
            return false;
        if (!NpcGenerationUtility.IsGeneratedNpc(pawn) || pawn.health == null)
            return false;

        return ScalpingUtility.HasScalpToTake(pawn);
    }

    private static float ChanceFor(Pawn pawn)
    {
        return NpcGenerationUtility.TechScaledChance(pawn, Tuning.chance, Tuning.zeroChanceTechLevel);
    }

    private static void AddScar(Pawn pawn)
    {
        var head = ScalpingUtility.HeadOf(pawn);
        var scalped = (Hediff_Scalped)HediffMaker.MakeHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped, pawn, head);

        // scalped while both were adults, same as NPC scalps are taken
        ScalpScarUtility.MakeOld(scalped, pawn, BackstorySlot.Adulthood);

        pawn.health.AddHediff(scalped, head);
    }

}
