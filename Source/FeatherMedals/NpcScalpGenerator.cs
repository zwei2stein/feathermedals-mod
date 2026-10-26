using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class NpcScalpGenerator
{
    private const float MaxChance = 0.05f;

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
        if (!MedalMod.Settings.GenerateNpcScalped)
            return false;
        if (pawn == null || pawn.Dead || pawn.health == null || pawn.ageTracker == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction is { IsPlayer: true })
            return false;
        if (pawn.health.hediffSet.HasHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped))
            return false;

        return ScalpingUtility.HeadOf(pawn) != null;
    }

    private static float ChanceFor(Pawn pawn)
    {
        var factionDef = pawn.Faction?.def ?? pawn.kindDef?.defaultFactionDef;
        if (factionDef == null || factionDef.techLevel == TechLevel.Undefined)
            return 0f;

        var span = (float)(TechLevel.Spacer - TechLevel.Animal);
        var steps = (float)(TechLevel.Spacer - factionDef.techLevel);
        return MaxChance * Mathf.Clamp01(steps / span);
    }

    private static void AddScar(Pawn pawn)
    {
        var head = ScalpingUtility.HeadOf(pawn);
        var scalped = (Hediff_Scalped)HediffMaker.MakeHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped, pawn, head);

        var scalper = PickScalper(pawn);
        if (scalper != null)
            scalped.scalper = new RememberedPawn(scalper);

        // scalped while both were adults, same as NPC scalps are taken
        scalped.BecomeScar();
        scalped.ageTicks = System.Math.Max(1, GenTicks.TicksGame - NpcHistoryUtility.RandomPastTick(scalper, pawn));

        pawn.health.AddHediff(scalped, head);
    }

    private static Pawn PickScalper(Pawn victim)
    {
        var faction = victim.Faction;
        var worldPawns = Current.Game?.World?.worldPawns;
        if (faction == null || worldPawns == null)
            return null;

        var pool = new List<Pawn>();
        var all = worldPawns.AllPawnsAlive;
        for (var i = 0; i < all.Count; i++)
        {
            var other = all[i];
            var otherFaction = other.Faction;
            if (otherFaction == null || otherFaction == faction || otherFaction.IsPlayer || otherFaction.Hidden)
                continue;
            if (!otherFaction.HostileTo(faction))
                continue;
            if (other.RaceProps.Humanlike && other.DevelopmentalStage.Adult())
                pool.Add(other);
        }
        return pool.Count > 0 ? pool.RandomElement() : null;
    }
}