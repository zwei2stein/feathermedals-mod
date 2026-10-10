using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace FeatherMedals;

public static class NpcTrophyGenerator
{
    public const int LeaderTrophies = 8;

    private const float AgePerRollStep = 50f;
    private const float ChancePerPassion = 0.1f;
    private const float ChancePerBonus = 0.1f;
    private const int HighSkill = 10;
    private const int HighAge = 50;
    private const int PickAttemptsPerTrophy = 3;

    private static List<ThingDef> medalDefs;

    private static List<ThingDef> MedalDefs
    {
        get
        {
            if (medalDefs != null)
                return medalDefs;

            medalDefs = new List<ThingDef>();
            var all = DefDatabase<ThingDef>.AllDefsListForReading;
            for (var i = 0; i < all.Count; i++)
            {
                if (FeatherMedalUtil.IsMedalDef(all[i]))
                    medalDefs.Add(all[i]);
            }
            return medalDefs;
        }
    }

    public static void OnGearGenerated(Pawn pawn)
    {
        try
        {
            if (!BaseEligible(pawn, out var baseRolls))
                return;
            
            // do not regenerate trophies
            if (HonoringWarriorsUtil.WornTrophies(pawn) > 0)
                return;

            var guaranteed = BackstoryGuaranteed(pawn);
            var total = guaranteed;

            // every guaranteed trophy replaces one roll
            var rolls = Mathf.FloorToInt(baseRolls * (1f + pawn.ageTracker.AgeBiologicalYearsFloat / AgePerRollStep)) - guaranteed;
            
            // roll for rest
            if (rolls > 0 && !pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                var chance = SuccessChance(pawn);
                for (var i = 0; i < rolls; i++)
                {
                    if (Rand.Chance(chance))
                        total++;
                }
            }

            Award(pawn, total);
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[FeatherTrophies] Failed to generate trophies for {pawn?.LabelShort}: {ex}", 1948201);
        }
    }

    // Faction.TryGenerateNewLeader postfix, the leader is only known after generation
    public static void OnLeaderChosen(Pawn leader)
    {
        try
        {
            if (!BaseEligible(leader, out _))
                return;

            Award(leader, LeaderTrophies - HonoringWarriorsUtil.WornTrophies(leader));
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[FeatherTrophies] Failed to generate leader trophies for {leader?.LabelShort}: {ex}", 1948202);
        }
    }

    private static bool BaseEligible(Pawn pawn, out int baseRolls)
    {
        baseRolls = 0;

        if (!MedalMod.Settings.GenerateNpcTrophies)
            return false;
        if (pawn == null || pawn.Dead || pawn.apparel == null || pawn.story == null || pawn.skills == null || pawn.ageTracker == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction is { IsPlayer: true })
            return false;
        if (!TechAllowed(pawn))
            return false;
        if (pawn.story.Adulthood == null && pawn.story.Childhood != FeatherMedalDefOf.FeatherTrophy_Backstory_PraisedChild)
            return false;

        baseRolls = BaseRolls(pawn);
        return baseRolls > 0;
    }

    private static bool TechAllowed(Pawn pawn)
    {
        var factionDef = pawn.Faction?.def ?? pawn.kindDef?.defaultFactionDef;
        if (factionDef == null)
            return false;

        var level = factionDef.techLevel;
        return level >= MedalMod.Settings.NpcTrophyMinTech && level <= MedalMod.Settings.NpcTrophyMaxTech;
    }
    
    private static int BaseRolls(Pawn pawn)
    {
        if (pawn.Faction == null)
            return 1;

        var ideo = pawn.Ideo;
        if (ideo == null)
            return 1;

        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Forbidden)
            || ideo.HasPrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Disapproved))
            return 0;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Required))
            return 3;
        if (ideo.HasPrecept(FeatherMedalDefOf.FeatherMedals_HonoringWarriors_Desired))
            return 2;
        return 1;
    }

    private static int BackstoryGuaranteed(Pawn pawn)
    {
        return Mathf.Max(GuaranteedFor(pawn.story.Adulthood), GuaranteedFor(pawn.story.Childhood));
    }

    private static int GuaranteedFor(BackstoryDef backstory)
    {
        if (backstory == null)
            return 0;
        if (backstory == FeatherMedalDefOf.FeatherTrophy_Backstory_HonoredElder
            || backstory == FeatherMedalDefOf.FeatherTrophy_Backstory_DisgracedElder)
            return 3;
        if (backstory == FeatherMedalDefOf.FeatherTrophy_Backstory_FeatheredWarrior)
            return 2;
        if (backstory == FeatherMedalDefOf.FeatherTrophy_Backstory_PraisedWarrior
            || backstory == FeatherMedalDefOf.FeatherTrophy_Backstory_PraisedChild)
            return 1;
        return 0;
    }

    private static float SuccessChance(Pawn pawn)
    {
        var melee = pawn.skills.GetSkill(SkillDefOf.Melee);
        var shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);

        // None 0, Minor 1, Major 2, so at most 4 steps
        var chance = ChancePerPassion * Mathf.Min(4, (int)melee.passion + (int)shooting.passion);
        
        if (melee.Level > HighSkill)
            chance += ChancePerBonus;
        
        if (shooting.Level > HighSkill)
            chance += ChancePerBonus;
        
        if (pawn.ageTracker.AgeBiologicalYearsFloat > HighAge)
            chance += ChancePerBonus;
        
        return chance;
    }

    private static void Award(Pawn pawn, int count)
    {
        if (count <= 0)
            return;

        var pool = BuildPresenterPool(pawn);
        var fallbackPresenter = FallbackPresenter(pawn);
        var settlementTiles = SettlementTiles(pawn.Faction);
        var fallbackTile = FallbackTile();

        // lets the Wear gate and HasPartsToWear patch through, same as a ceremony
        var previous = RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward;
        RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = true;
        try
        {
            var defs = new List<ThingDef>();
            foreach (var def in MedalDefs)
            {
                if (ApparelUtility.HasPartsToWear(pawn, def))
                    defs.Add(def);
            }
            if (defs.Count == 0)
                return;

            var made = new List<FeatherMedal>();
            var attempts = count * PickAttemptsPerTrophy;
            while (made.Count < count && attempts-- > 0)
            {
                var medal = CreateMedal(pawn, defs, pool, fallbackPresenter, settlementTiles, fallbackTile);
                if (medal != null)
                    made.Add(medal);
            }

            // oldest first, so the newest one is the last to be worn and the one that grants the Decorated trait
            made.Sort((a, b) => a.awardedTick.CompareTo(b.awardedTick));

            FeatherMedal newest = null;
            foreach (var medal in made)
            {
                pawn.apparel.Wear(medal, false, true);
                if (pawn.apparel.Wearing(medal))
                    newest = medal;
                else if (!medal.Destroyed)
                    medal.Destroy();
            }

            if (newest != null)
                RitualOutcomeTraitUtil.UpdateDecoratedTrait(pawn, newest);
        }
        finally
        {
            RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = previous;
        }
    }

    private static FeatherMedal CreateMedal(Pawn pawn, List<ThingDef> defs, List<Pawn> pool, Pawn fallbackPresenter,
        List<PlanetTile> settlementTiles, PlanetTile fallbackTile)
    {
        var def = defs.RandomElement();
        var medal = (FeatherMedal)ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
        if (!medal.PawnCanWear(pawn, true))
        {
            medal.Destroy();
            return null;
        }

        medal.awardedTick = PickAwardedTick(pawn);
        medal.awardedTile = settlementTiles.Count > 0 ? settlementTiles.RandomElement() : fallbackTile;
        medal.awardedBy = pool.Count > 0 ? pool.RandomElement() : fallbackPresenter;
        medal.ceremonyQuality = Rand.RangeInclusive(0, 3);
        medal.citation = GenerateCitation(pawn);
        return medal;
    }

    // random moment between the pawn becoming an adult and now, game ticks may be negative
    private static int PickAwardedTick(Pawn pawn)
    {
        var nowAbs = (long)GenTicks.TicksAbs;
        var nowGame = GenTicks.TicksGame;
        var adultAbs = pawn.ageTracker.BirthAbsTicks + pawn.ageTracker.AdultMinAgeTicks;

        var pickedAbs = adultAbs >= nowAbs
            ? nowAbs
            : adultAbs + (long)(Rand.Value * (nowAbs - adultAbs));

        var tick = (int)(nowGame - (nowAbs - pickedAbs));
        // -1 means "no date" on a trophy
        return tick == -1 ? -2 : tick;
    }

    private static List<Pawn> BuildPresenterPool(Pawn pawn)
    {
        var pool = new List<Pawn>();
        var faction = pawn.Faction;
        var worldPawns = Current.Game?.World?.worldPawns;
        if (faction == null || worldPawns == null)
            return pool;

        var all = worldPawns.AllPawnsAlive;
        for (var i = 0; i < all.Count; i++)
        {
            var other = all[i];
            if (other != pawn && other.Faction == faction && other.RaceProps.Humanlike && other.DevelopmentalStage.Adult())
                pool.Add(other);
        }
        return pool;
    }

    private static Pawn FallbackPresenter(Pawn pawn)
    {
        var leader = pawn.Faction?.leader;
        return leader != null && leader != pawn && !leader.Dead ? leader : null;
    }

    private static List<PlanetTile> SettlementTiles(Faction faction)
    {
        var tiles = new List<PlanetTile>();
        if (faction == null || Current.Game == null)
            return tiles;

        var settlements = Find.WorldObjects.Settlements;
        for (var i = 0; i < settlements.Count; i++)
        {
            if (settlements[i].Faction == faction)
                tiles.Add(settlements[i].Tile);
        }
        return tiles;
    }

    private static PlanetTile FallbackTile()
    {
        if (Current.Game == null)
            return PlanetTile.Invalid;

        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        return map != null ? map.Tile : PlanetTile.Invalid;
    }

    // placeholder text for now, later wiring is XML only
    private static string GenerateCitation(Pawn pawn)
    {
        var rulePack = FeatherMedalDefOf.FeatherMedals_NpcCitation;
        if (rulePack == null)
            return null;

        var request = default(GrammarRequest);
        request.Includes.Add(rulePack);
        request.Rules.Add(new Rule_String("faction_name", pawn.Faction?.Name ?? string.Empty));
        return GrammarResolver.Resolve("r_root", request, "FeatherMedals_NpcCitation");
    }
}