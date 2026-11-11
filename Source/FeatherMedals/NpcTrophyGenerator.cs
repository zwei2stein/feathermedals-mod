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

// backstory or apparel guiding trophy generation, one per trophy kind
public class NpcTrophyGuaranteeExtension : DefModExtension
{
    // trophy kind this guarantees
    public TrophyKindDef kind;

    // guaranteed random trophies, the rolls of all sources add up
    public IntRange count = new IntRange(0, 0);

    // preferred trophy that can take place of RNG trophy
    public List<NpcTrophyPick> trophies;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (kind == null)
            yield return $"{nameof(kind)} is missing";

        if (count.min < 0 || count.max < count.min)
            yield return $"{nameof(count)} {count} is not a valid range";

        if (trophies == null)
            yield break;

        foreach (var pick in trophies)
        {
            if (pick.trophy == null)
                yield return $"{nameof(trophies)} has an entry without {nameof(NpcTrophyPick.trophy)}";
            else if (!AwardedTrophyUtility.IsAwardedTrophyDef(pick.trophy))
                yield return $"{nameof(trophies)}: {pick.trophy.defName} is not an awarded trophy";
            else if (kind != null && AwardedTrophyUtility.KindOf(pick.trophy) != kind)
                yield return $"{nameof(trophies)}: {pick.trophy.defName} is not {kind.defName}";

            if (pick.chance < 0f || pick.chance > 1f)
                yield return $"{nameof(trophies)}: {nameof(NpcTrophyPick.chance)} {pick.chance} is outside 0..1";
        }
    }
}

public static class NpcTrophyGenerator
{
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
                if (AwardedTrophyUtility.IsAwardedTrophyDef(all[i]))
                    field.Add(all[i]);
            }
            return field;
        }
    }

    // each kind on its own: own settings, chances, counts and leader trophies
    public static void OnGearGenerated(Pawn pawn)
    {
        foreach (var kind in DefDatabase<TrophyKindDef>.AllDefsListForReading)
        {
            try
            {
                if (!BaseEligible(pawn, kind, out var baseRolls))
                    continue;

                // do not regenerate trophies
                if (AwardedTrophyUtility.WornCount(pawn, kind) > 0)
                    continue;

                var total = BackstoryGuaranteed(pawn, kind);

                // roll for rest
                if (!pawn.WorkTagIsDisabled(WorkTags.Violent) && Rand.Chance(ExtraTrophiesChance(pawn, kind)))
                    total += Rand.RangeInclusive(0, baseRolls + BonusRolls(pawn, kind.npcGeneration));

                Adorn(pawn, kind, total, false);
            }
            catch (System.Exception ex)
            {
                Log.ErrorOnce($"[TribalTrophies] Failed to generate {kind.defName} trophies for {pawn?.LabelShort}: {ex}", 1948201 ^ kind.shortHash);
            }
        }
    }

    // Faction.TryGenerateNewLeader postfix, the leader is only known after generation
    public static void OnLeaderChosen(Pawn leader)
    {
        foreach (var kind in DefDatabase<TrophyKindDef>.AllDefsListForReading)
        {
            try
            {
                if (!BaseEligible(leader, kind, out _))
                    continue;

                // on top of the trophies the leader already wears
                Adorn(leader, kind, kind.npcGeneration.leaderTrophies, true);
            }
            catch (System.Exception ex)
            {
                Log.ErrorOnce($"[TribalTrophies] Failed to generate {kind.defName} leader trophies for {leader?.LabelShort}: {ex}", 1948202 ^ kind.shortHash);
            }
        }
    }

    private static bool BaseEligible(Pawn pawn, TrophyKindDef kind, out int baseRolls)
    {
        baseRolls = 0;

        var npc = kind.npcGeneration;
        if (npc == null || !npc.SettingsWorker.Generate)
            return false;
        if (!NpcGenerationUtility.IsGeneratedNpc(pawn))
            return false;
        if (pawn.apparel == null || pawn.story == null || pawn.skills == null)
            return false;
        if (!TechAllowed(pawn, npc.SettingsWorker))
            return false;
        // no adulthood yet, only a childhood that gives trophies counts
        if (pawn.story.Adulthood == null && !GuaranteesOf(pawn.story.Childhood, kind).Any())
            return false;

        baseRolls = StanceOf(pawn, npc).baseRolls;
        // highest possible guarantee, so the gate does not depend on a roll
        return baseRolls + MaxGuaranteed(pawn, kind) > 0;
    }

    private static bool TechAllowed(Pawn pawn, TrophyNpcSettingsWorker settings)
    {
        var factionDef = NpcGenerationUtility.FactionDefFor(pawn);
        if (factionDef == null)
            return false;

        var level = factionDef.techLevel;
        return level >= settings.MinTech && level <= settings.MaxTech;
    }

    // Faction == null counts as neutral
    private static TrophyNpcStance StanceOf(Pawn pawn, TrophyNpcGeneration npc)
    {
        var stance = pawn.Faction == null ? HonoringStance.Neutral : HonoringWarriorsPrecepts.StanceOf(pawn.Ideo);
        return npc.StanceFor(stance);
    }

    // tech level chance, scaled by ideo stance
    private static float ExtraTrophiesChance(Pawn pawn, TrophyKindDef kind)
    {
        var factionDef = NpcGenerationUtility.FactionDefFor(pawn);
        if (factionDef == null)
            return 0f;

        var npc = kind.npcGeneration;
        return npc.TechChance(factionDef.techLevel) * StanceOf(pawn, npc).chanceFactor;
    }

    // one roll per combat passion level, and one each for high melee, high shooting and old age
    private static int BonusRolls(Pawn pawn, TrophyNpcGeneration npc)
    {
        var melee = pawn.skills.GetSkill(SkillDefOf.Melee);
        var shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);

        // None 0, Minor 1, Major 2
        var rolls = Mathf.Min(npc.maxPassionRolls, (int)melee.passion + (int)shooting.passion);

        if (melee.Level > npc.highSkill)
            rolls++;

        if (shooting.Level > npc.highSkill)
            rolls++;

        if (pawn.ageTracker.AgeBiologicalYearsFloat > npc.highAge)
            rolls++;

        return rolls;
    }

    // the rolls of all sources add up, see NpcTrophyGuaranteeExtension
    private static int BackstoryGuaranteed(Pawn pawn, TrophyKindDef kind)
    {
        var guaranteed = 0;
        foreach (var source in GuaranteeSources(pawn, kind))
            guaranteed += source.count.RandomInRange;
        return guaranteed;
    }

    private static int MaxGuaranteed(Pawn pawn, TrophyKindDef kind)
    {
        var guaranteed = 0;
        foreach (var source in GuaranteeSources(pawn, kind))
            guaranteed += source.count.max;
        return guaranteed;
    }

    // every pick that rolls its chance
    private static IEnumerable<ThingDef> PreferredTrophies(Pawn pawn, TrophyKindDef kind)
    {
        foreach (var source in GuaranteeSources(pawn, kind))
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
    private static IEnumerable<NpcTrophyGuaranteeExtension> GuaranteeSources(Pawn pawn, TrophyKindDef kind)
    {
        foreach (var adulthood in GuaranteesOf(pawn.story.Adulthood, kind))
            yield return adulthood;

        foreach (var childhood in GuaranteesOf(pawn.story.Childhood, kind))
            yield return childhood;

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            foreach (var worn in GuaranteesOf(apparel.def, kind))
                yield return worn;
        }
    }

    // a def can carry one extension per kind
    private static IEnumerable<NpcTrophyGuaranteeExtension> GuaranteesOf(Def def, TrophyKindDef kind)
    {
        var extensions = def?.modExtensions;
        if (extensions == null)
            yield break;

        for (var i = 0; i < extensions.Count; i++)
        {
            if (extensions[i] is NpcTrophyGuaranteeExtension guarantee && guarantee.kind == kind)
                yield return guarantee;
        }
    }

    private static void Adorn(Pawn pawn, TrophyKindDef kind, int count, bool leader)
    {
        if (count <= 0)
            return;

        var pool = BuildPresenterPool(pawn);
        var fallbackPresenter = FallbackPresenter(pawn);

        // lets the Wear gate and HasPartsToWear patch through, same as a ceremony
        var previous = AwardedTrophyUtility.ApplyingCeremonyAdorning;
        AwardedTrophyUtility.ApplyingCeremonyAdorning = true;
        try
        {
            var defs = new List<ThingDef>();
            foreach (var def in FeatherTrophyDefs)
            {
                if (AwardedTrophyUtility.KindOf(def) == kind && ApparelUtility.HasPartsToWear(pawn, def))
                    defs.Add(def);
            }
            if (defs.Count == 0)
                return;

            var made = new List<AwardedTrophy>();

            // preferred feathers first (leaders get a second pass)
            var preferred = PreferredTrophies(pawn, kind).ToList();
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

            // each worn trophy type can have its own propgression trait
            var newest = new Dictionary<TrophyKindDef, AwardedTrophy>();
            foreach (var featherTrophy in made)
            {
                pawn.apparel.Wear(featherTrophy, false, true);
                if (pawn.apparel.Wearing(featherTrophy))
                {
                    featherTrophy.SetAdornedPawn(pawn);
                    newest[featherTrophy.Kind] = featherTrophy;
                }
                else if (!featherTrophy.Destroyed)
                    featherTrophy.Destroy();
            }

            if (TribalTrophiesMod.Settings.TrophyDynamicTraits)
            {
                foreach (var newestOfKind in newest.Values)
                    RitualOutcomeTraitUtility.UpdateDecoratedTrait(pawn, newestOfKind);
            }
        }
        finally
        {
            AwardedTrophyUtility.ApplyingCeremonyAdorning = previous;
        }
    }

    private static AwardedTrophy CreateFeatherTrophy(Pawn pawn, ThingDef def, List<Pawn> pool, Pawn fallbackPresenter, bool leader)
    {
        var featherTrophy = (AwardedTrophy)ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
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
        featherTrophy.tale = TaleUtility.Generate(featherTrophy.Kind, pawn.Faction, pawn, featherTrophy.adorned.tile);
        return featherTrophy;
    }

    private static void SetTrophyQuality(AwardedTrophy trophy, Pawn pawn, bool leader)
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