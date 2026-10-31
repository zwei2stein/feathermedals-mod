using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// preferred trophy to give out
public class NpcTrophyPick
{
    public ThingDef trophy;
    public float chance = 1f;
}

// backstory or apparel guiding trophy generation
public class NpcTrophyGuaranteeExtension : DefModExtension
{
    // guaranteed random feathers, the rolls of all sources add up
    public IntRange count = new IntRange(0, 0);

    // preferred trophy that can take place of RNG trophy
    public List<NpcTrophyPick> trophies;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (count.min < 0 || count.max < count.min)
            yield return $"{nameof(count)} {count} is not a valid range";

        if (trophies == null)
            yield break;

        foreach (var pick in trophies)
        {
            if (pick.trophy == null)
                yield return $"{nameof(trophies)} has an entry without {nameof(NpcTrophyPick.trophy)}";
            else if (!FeatherTrophyUtility.IsFeatherTrophyDef(pick.trophy))
                yield return $"{nameof(trophies)}: {pick.trophy.defName} is not a feather trophy";

            if (pick.chance < 0f || pick.chance > 1f)
                yield return $"{nameof(trophies)}: {nameof(NpcTrophyPick.chance)} {pick.chance} is outside 0..1";
        }
    }
}

public static class NpcTrophyGenerator
{
    public const int LeaderTrophies = 8;

    private const int MaxPassionRolls = 4;
    private const int HighSkill = 10;
    private const int HighAge = 50;
    private const int PickAttemptsPerTrophy = 3;
    private const int MaxCeremonyStage = 3; // Legendary AdorningQuality
    private const int LeaderQualitySteps = 2; // remove awful/poor for leaders
    private const int LegendaryCeremonyQualitySteps = 2; // remove awful/poor for legendary ceremony

    private static List<ThingDef> FeatherTrophyDefs
    {
        get
        {
            if (field != null)
                return field;

            field = new List<ThingDef>();
            var all = DefDatabase<ThingDef>.AllDefsListForReading;
            for (var i = 0; i < all.Count; i++)
            {
                if (FeatherTrophyUtility.IsFeatherTrophyDef(all[i]))
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
            if (HonoringWarriorsUtility.WornTrophies(pawn) > 0)
                return;

            var total = BackstoryGuaranteed(pawn);

            // roll for rest
            if (!pawn.WorkTagIsDisabled(WorkTags.Violent) && Rand.Chance(ExtraTrophiesChance(pawn)))
                total += Rand.RangeInclusive(0, baseRolls + BonusRolls(pawn));

            Adorn(pawn, total, false);
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

            // on top of the trophies the leader already wears
            Adorn(leader, LeaderTrophies, true);
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
        if (!NpcGenerationUtility.IsGeneratedNpc(pawn))
            return false;
        if (pawn.apparel == null || pawn.story == null || pawn.skills == null)
            return false;
        if (!TechAllowed(pawn))
            return false;
        // no adulthood yet, only a childhood that gives trophies counts
        if (pawn.story.Adulthood == null && pawn.story.Childhood?.HasModExtension<NpcTrophyGuaranteeExtension>() != true)
            return false;

        baseRolls = BaseRolls(pawn);
        // highest possible guarantee, so the gate does not depend on a roll
        return baseRolls + MaxGuaranteed(pawn) > 0;
    }

    private static bool TechAllowed(Pawn pawn)
    {
        var factionDef = NpcGenerationUtility.FactionDefFor(pawn);
        if (factionDef == null)
            return false;

        var level = factionDef.techLevel;
        return level >= TribalTrophiesMod.Settings.NpcTrophyMinTech && level <= TribalTrophiesMod.Settings.NpcTrophyMaxTech;
    }

    private static int BaseRolls(Pawn pawn)
    {
        if (pawn.Faction == null)
            return 1;

        return HonoringWarriorsPrecepts.StanceOf(pawn.Ideo) switch
        {
            HonoringStance.Forbidden or HonoringStance.Disapproved => 0,
            HonoringStance.Neutral => 1,
            HonoringStance.Required => 3,
            HonoringStance.Desired => 2,
            _ => 1
        };
    }

    // ideo stance chance to roll for extra trophies, 
    private static float StanceChance(Pawn pawn)
    {
        // Faction == null counts as neutral
        if (pawn.Faction == null)
            return 0.125f;

        return HonoringWarriorsPrecepts.StanceOf(pawn.Ideo) switch
        {
            HonoringStance.Forbidden or HonoringStance.Disapproved => 0f,
            HonoringStance.Neutral => 0.125f,
            HonoringStance.Desired => 0.25f,
            HonoringStance.Required => 0.5f,
            _ => 0.125f
        };
    }

    private static float ExtraTrophiesChance(Pawn pawn)
    {
        var zeroChanceTechLevel = TribalTrophiesMod.Settings.NpcTrophyMaxTech + 1;
        return NpcGenerationUtility.TechScaledChance(pawn, StanceChance(pawn), zeroChanceTechLevel);
    }

    // one roll per combat passion level, and one each for high melee, high shooting and old age
    private static int BonusRolls(Pawn pawn)
    {
        var melee = pawn.skills.GetSkill(SkillDefOf.Melee);
        var shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);

        // None 0, Minor 1, Major 2, so at most 4 rolls
        var rolls = Mathf.Min(MaxPassionRolls, (int)melee.passion + (int)shooting.passion);

        if (melee.Level > HighSkill)
            rolls++;

        if (shooting.Level > HighSkill)
            rolls++;

        if (pawn.ageTracker.AgeBiologicalYearsFloat > HighAge)
            rolls++;

        return rolls;
    }

    // the rolls of all sources add up, see NpcTrophyGuaranteeExtension
    private static int BackstoryGuaranteed(Pawn pawn)
    {
        var guaranteed = 0;
        foreach (var source in GuaranteeSources(pawn))
            guaranteed += source.count.RandomInRange;
        return guaranteed;
    }

    private static int MaxGuaranteed(Pawn pawn)
    {
        var guaranteed = 0;
        foreach (var source in GuaranteeSources(pawn))
            guaranteed += source.count.max;
        return guaranteed;
    }

    // every pick that rolls its chance
    private static IEnumerable<ThingDef> PreferredTrophies(Pawn pawn)
    {
        foreach (var source in GuaranteeSources(pawn))
        {
            if (source.trophies == null)
                continue;
            foreach (var pick in source.trophies)
            {
                if (pick.trophy != null && Rand.Chance(pick.chance))
                    yield return pick.trophy;
            }
        }
    }

    // both backstories and everything worn
    private static IEnumerable<NpcTrophyGuaranteeExtension> GuaranteeSources(Pawn pawn)
    {
        var adulthood = pawn.story.Adulthood?.GetModExtension<NpcTrophyGuaranteeExtension>();
        if (adulthood != null)
            yield return adulthood;

        var childhood = pawn.story.Childhood?.GetModExtension<NpcTrophyGuaranteeExtension>();
        if (childhood != null)
            yield return childhood;

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            var worn = apparel.def.GetModExtension<NpcTrophyGuaranteeExtension>();
            if (worn != null)
                yield return worn;
        }
    }

    private static void Adorn(Pawn pawn, int count, bool leader)
    {
        if (count <= 0)
            return;

        var pool = BuildPresenterPool(pawn);
        var fallbackPresenter = FallbackPresenter(pawn);

        // lets the Wear gate and HasPartsToWear patch through, same as a ceremony
        var previous = FeatherTrophyUtility.ApplyingCeremonyAdorning;
        FeatherTrophyUtility.ApplyingCeremonyAdorning = true;
        try
        {
            var defs = new List<ThingDef>();
            foreach (var def in FeatherTrophyDefs)
            {
                if (ApparelUtility.HasPartsToWear(pawn, def))
                    defs.Add(def);
            }
            if (defs.Count == 0)
                return;

            var made = new List<FeatherTrophy>();

            // preferred feathers first (leaders get a second pass)
            var preferred = PreferredTrophies(pawn).ToList();
            // each worn copy settles one owed pick
            foreach (var worn in pawn.apparel.WornApparel)
                preferred.Remove(worn.def);
            preferred.Shuffle();
            foreach (var def in preferred)
            {
                if (made.Count >= count)
                    break;
                if (!defs.Contains(def))
                    continue;

                var featherTrophy = CreateFeatherTrophy(pawn, def, pool, fallbackPresenter, leader);
                if (featherTrophy != null)
                    made.Add(featherTrophy);
            }

            var attempts = count * PickAttemptsPerTrophy;
            while (made.Count < count && attempts-- > 0)
            {
                var featherTrophy = CreateFeatherTrophy(pawn, defs.RandomElement(), pool, fallbackPresenter, leader);
                if (featherTrophy != null)
                    made.Add(featherTrophy);
            }

            // oldest first, so the newest one is the last to be worn and the one that grants the Decorated trait
            made.Sort((a, b) => a.adorned.tick.CompareTo(b.adorned.tick));

            FeatherTrophy newest = null;
            foreach (var featherTrophy in made)
            {
                pawn.apparel.Wear(featherTrophy, false, true);
                if (pawn.apparel.Wearing(featherTrophy))
                {
                    featherTrophy.SetAdornedPawn(pawn);
                    newest = featherTrophy;
                }
                else if (!featherTrophy.Destroyed)
                    featherTrophy.Destroy();
            }

            if (TribalTrophiesMod.Settings.TrophyDynamicTraits && newest != null)
                RitualOutcomeTraitUtility.UpdateDecoratedTrait(pawn, newest);
        }
        finally
        {
            FeatherTrophyUtility.ApplyingCeremonyAdorning = previous;
        }
    }

    private static FeatherTrophy CreateFeatherTrophy(Pawn pawn, ThingDef def, List<Pawn> pool, Pawn fallbackPresenter, bool leader)
    {
        var featherTrophy = (FeatherTrophy)ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
        if (!featherTrophy.PawnCanWear(pawn, true))
        {
            featherTrophy.Destroy();
            return null;
        }

        featherTrophy.adorned = NpcHistoryUtility.RandomPastEvent(pawn.Faction, pawn);
        var presenter = pool.Count > 0 ? pool.RandomElement() : fallbackPresenter;
        featherTrophy.adornedBy = presenter != null ? new RememberedPawn(presenter) : null;
        featherTrophy.ceremonyQuality = Rand.RangeInclusive(0, MaxCeremonyStage);
        SetTrophyQuality(featherTrophy, pawn, leader);
        featherTrophy.tale = TaleUtility.Generate(pawn.Faction, pawn, featherTrophy.adorned.tile);
        return featherTrophy;
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