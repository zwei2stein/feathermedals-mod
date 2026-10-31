using RimWorld;
using Verse;

namespace TribalTrophies;

public class NpcScalpBeltExtension : NpcBeltExtension
{
    public float honorableChance = 0.05f;
    public float mercilessChance = 0.2f;
}

public static class NpcScalpBeltGenerator
{
    public static void OnGearGenerated(Pawn pawn)
    {
        try
        {
            if (!Eligible(pawn))
                return;

            NpcBeltGenerationUtility.TryGiveBelt<NpcScalpBeltExtension>(pawn,
                tuning => BaseChance(pawn.Ideo, tuning),
                TribalTrophiesDefOf.TribalTrophies_Trophy_Scalping_Scalp,
                () => NpcScalpUtility.MakeScalp(pawn.Faction, pawn));
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate scalp belt for {pawn?.LabelShort}: {ex}", 7281934);
        }
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!TribalTrophiesMod.Settings.GenerateNpcScalpBelts)
            return false;
        if (!NpcBeltGenerationUtility.IsNpcWearer(pawn))
            return false;

        return pawn.Ideo != null;
    }

    private static float BaseChance(Ideo ideo, NpcScalpBeltExtension tuning)
    {
        return ScalpingPrecepts.StanceOf(ideo) switch
        {
            ScalpStance.Merciless => tuning.mercilessChance,
            ScalpStance.Honorable => tuning.honorableChance,
            _ => 0f
        };
    }
}
