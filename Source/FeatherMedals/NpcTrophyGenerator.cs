using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

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
    private const int MaxCeremonyStage = 3; // Legendary AdorningQuality 
    private const int LeaderQualitySteps = 2; // remove awful/poor for leaders
    private const int LegendaryCeremonyQualitySteps = 2; // remove awful/poor for legendary ceremony

    private static List<ThingDef> MedalDefs
    {
        get
        {
            if (field != null)
                return field;

            field = new List<ThingDef>();
            var all = DefDatabase<ThingDef>.AllDefsListForReading;
            for (var i = 0; i < all.Count; i++)
            {
                if (FeatherTrophyUtil.IsMedalDef(all[i]))
                    field.Add(all[i]);
            }
            return field;
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

            Award(pawn, total, false);
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate trophies for {pawn?.LabelShort}: {ex}", 1948201);
        }
    }

    // Faction.TryGenerateNewLeader postfix, the leader is only known after generation
    public static void OnLeaderChosen(Pawn leader)
    {
        try
        {
            if (!BaseEligible(leader, out _))
                return;

            Award(leader, LeaderTrophies - HonoringWarriorsUtil.WornTrophies(leader), true);
        }
        catch (System.Exception ex)
        {
            Log.ErrorOnce($"[TribalTrophies] Failed to generate leader trophies for {leader?.LabelShort}: {ex}", 1948202);
        }
    }

    private static bool BaseEligible(Pawn pawn, out int baseRolls)
    {
        baseRolls = 0;

        if (!TribalTrophiesMod.Settings.GenerateNpcTrophies)
            return false;
        if (pawn == null || pawn.Dead || pawn.apparel == null || pawn.story == null || pawn.skills == null || pawn.ageTracker == null)
            return false;
        if (!pawn.RaceProps.Humanlike || !pawn.DevelopmentalStage.Adult())
            return false;
        if (pawn.Faction is { IsPlayer: true })
            return false;
        if (!TechAllowed(pawn))
            return false;
        if (pawn.story.Adulthood == null && pawn.story.Childhood != TribalTrophiesDefOf.TribalTrophies_Backstory_PraisedChild)
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
        return level >= TribalTrophiesMod.Settings.NpcTrophyMinTech && level <= TribalTrophiesMod.Settings.NpcTrophyMaxTech;
    }
    
    private static int BaseRolls(Pawn pawn)
    {
        if (pawn.Faction == null)
            return 1;

        var ideo = pawn.Ideo;
        if (ideo == null)
            return 1;

        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Forbidden)
            || ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Disapproved))
            return 0;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Required))
            return 3;
        if (ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_HonoringWarriors_Desired))
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
        if (backstory == TribalTrophiesDefOf.TribalTrophies_Backstory_HonoredElder
            || backstory == TribalTrophiesDefOf.TribalTrophies_Backstory_DisgracedElder)
            return 3;
        if (backstory == TribalTrophiesDefOf.TribalTrophies_Backstory_FeatheredWarrior)
            return 2;
        if (backstory == TribalTrophiesDefOf.TribalTrophies_Backstory_PraisedWarrior
            || backstory == TribalTrophiesDefOf.TribalTrophies_Backstory_PraisedChild)
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

    private static void Award(Pawn pawn, int count, bool leader)
    {
        if (count <= 0)
            return;

        var pool = BuildPresenterPool(pawn);
        var fallbackPresenter = FallbackPresenter(pawn);

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

            var made = new List<FeatherTrophy>();
            var attempts = count * PickAttemptsPerTrophy;
            while (made.Count < count && attempts-- > 0)
            {
                var medal = CreateMedal(pawn, defs, pool, fallbackPresenter, leader);
                if (medal != null)
                    made.Add(medal);
            }

            // oldest first, so the newest one is the last to be worn and the one that grants the Decorated trait
            made.Sort((a, b) => a.awarded.tick.CompareTo(b.awarded.tick));

            FeatherTrophy newest = null;
            foreach (var medal in made)
            {
                pawn.apparel.Wear(medal, false, true);
                if (pawn.apparel.Wearing(medal))
                    newest = medal;
                else if (!medal.Destroyed)
                    medal.Destroy();
            }

            if (TribalTrophiesMod.Settings.TrophyDynamicTraits && newest != null)
                RitualOutcomeTraitUtil.UpdateDecoratedTrait(pawn, newest);
        }
        finally
        {
            RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward = previous;
        }
    }

    private static FeatherTrophy CreateMedal(Pawn pawn, List<ThingDef> defs, List<Pawn> pool, Pawn fallbackPresenter, bool leader)
    {
        var def = defs.RandomElement();
        var medal = (FeatherTrophy)ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
        if (!medal.PawnCanWear(pawn, true))
        {
            medal.Destroy();
            return null;
        }

        medal.awarded = NpcHistoryUtility.RandomPastEvent(pawn.Faction, pawn);
        var presenter = pool.Count > 0 ? pool.RandomElement() : fallbackPresenter;
        medal.awardedBy = presenter != null ? new RememberedPawn(presenter) : null;
        medal.ceremonyQuality = Rand.RangeInclusive(0, MaxCeremonyStage);
        SetTrophyQuality(medal, pawn, leader);
        medal.citation = CitationUtil.Generate(pawn.Faction, pawn, medal.awarded.tile);
        return medal;
    }

    private static void SetTrophyQuality(FeatherTrophy trophy, Pawn pawn, bool leader)
    {
        var comp = trophy.TryGetComp<CompQuality>();
        if (comp == null)
            return;

        var quality = QualityUtility.GenerateQualityGeneratingPawn(pawn.kindDef, trophy.def);

        var steps = 0;
        if (leader)
            steps += LeaderQualitySteps;
        if (trophy.ceremonyQuality >= MaxCeremonyStage)
            steps += LegendaryCeremonyQualitySteps;

        var boosted = Mathf.Clamp((int)quality + steps, (int)QualityCategory.Awful, (int)QualityCategory.Legendary);
        comp.SetQuality((QualityCategory)boosted, ArtGenerationContext.Outsider);
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
}