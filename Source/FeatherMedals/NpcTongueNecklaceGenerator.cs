using RimWorld;
using Verse;

namespace TribalTrophies;

public class NpcTongueNecklaceExtension : NpcBeltExtension
{
    public float honorableChance = 0.05f;
    public float mercilessChance = 0.2f;
}

// Same rules as scalp belts, rolled on its own: a pawn can get a belt, a necklace, both or neither.
public static class NpcTongueNecklaceGenerator
{
    public static void OnGearGenerated(Pawn pawn)
    {
        try
        {
            if (!Eligible(pawn))
                return;

            NpcBeltGenerationUtility.TryGiveBelt<NpcTongueNecklaceExtension>(pawn,
                tuning => BaseChance(pawn.Ideo, tuning),
                TribalTrophiesDefOf.TribalTrophies_Trophy_TongueCutting_DriedTongue,
                () => NpcVictimUtility.MakeTrophyFromTemporaryVictim(pawn.Faction, pawn, DriedTongue.Create));
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate dried tongue necklace for {pawn?.LabelShort}: {ex}", 7281936);
        }
    }

    private static bool Eligible(Pawn pawn)
    {
        if (!TribalTrophiesMod.Settings.GenerateNpcTongueNecklaces)
            return false;
        if (!NpcBeltGenerationUtility.IsNpcWearer(pawn))
            return false;

        return pawn.Ideo != null;
    }

    private static float BaseChance(Ideo ideo, NpcTongueNecklaceExtension tuning)
    {
        return ScalpingPrecepts.StanceOf(ideo) switch
        {
            ScalpStance.Merciless => tuning.mercilessChance,
            ScalpStance.Honorable => tuning.honorableChance,
            _ => 0f
        };
    }
}
