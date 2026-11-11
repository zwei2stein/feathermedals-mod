using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

// A family of awarded trophies (Feather, Embroidery...)
public class TrophyKindDef : Def
{
    // ceremony
    public PreceptDef ceremonyPrecept;
    public InteractionDef speechInteraction;
    public ThoughtDef adornedThought;
    public ThoughtDef witnessedThought;
    public TaleDef adornedTale;
    public RulePackDef taleOfHonorRulePack;

    // wording
    public string communityLabel;
    public string honoreeLabel;

    // ideo roles that can present the trophy in the ceremony
    public List<PreceptDef> presenterRoles;

    // title trait
    public TraitDef decoratedTrait;
    public List<int> decoratedThresholds;

    // drawing on pawns
    public Type layoutWorkerClass;

    // NPC pawns spawning with this kind, none = never
    public TrophyNpcGeneration npcGeneration;

    [Unsaved] private TrophyLayoutWorker layoutWorkerInt;

    public TrophyLayoutWorker LayoutWorker
    {
        get
        {
            if (layoutWorkerInt == null)
            {
                layoutWorkerInt = (TrophyLayoutWorker)Activator.CreateInstance(layoutWorkerClass);
                layoutWorkerInt.def = this;
            }
            return layoutWorkerInt;
        }
    }

    public string HonoreeIndefinite => Find.ActiveLanguageWorker.WithIndefiniteArticle(honoreeLabel);

    public string PresenterRolesLabel => field ??=
        string.Join(", ", presenterRoles.Select(role => role.LabelCap.Resolve()));

    public bool IsPresenter(Pawn pawn)
    {
        var role = pawn.Ideo?.GetRole(pawn);
        if (role == null)
            return false;

        return presenterRoles.Contains(role.def);
    }

    // highest degree reached, -1 = none
    public int DecoratedDegreeFor(int trophyCount)
    {
        var degree = -1;
        if (decoratedTrait == null || decoratedThresholds == null)
            return degree;

        for (var i = 0; i < decoratedThresholds.Count; i++)
        {
            if (trophyCount >= decoratedThresholds[i])
                degree = i;
        }
        return degree;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (ceremonyPrecept == null)
            yield return $"{nameof(ceremonyPrecept)} is missing";
        else if (ceremonyPrecept.ritualPatternBase == null)
            yield return $"{nameof(ceremonyPrecept)} {ceremonyPrecept.defName} has no ritualPatternBase";

        if (speechInteraction == null)
            yield return $"{nameof(speechInteraction)} is missing";
        if (adornedThought == null)
            yield return $"{nameof(adornedThought)} is missing";
        if (witnessedThought == null)
            yield return $"{nameof(witnessedThought)} is missing";
        if (adornedTale == null)
            yield return $"{nameof(adornedTale)} is missing";
        if (taleOfHonorRulePack == null)
            yield return $"{nameof(taleOfHonorRulePack)} is missing";

        if (communityLabel.NullOrEmpty())
            yield return $"{nameof(communityLabel)} is missing";
        if (honoreeLabel.NullOrEmpty())
            yield return $"{nameof(honoreeLabel)} is missing";

        if (presenterRoles.NullOrEmpty())
            yield return $"{nameof(presenterRoles)} is missing";
        else
        {
            foreach (var role in presenterRoles)
            {
                if (role?.preceptClass == null || !typeof(Precept_Role).IsAssignableFrom(role.preceptClass))
                    yield return $"{nameof(presenterRoles)}: {role?.defName} is not an ideo role";
            }
        }

        if (layoutWorkerClass == null || !typeof(TrophyLayoutWorker).IsAssignableFrom(layoutWorkerClass))
            yield return $"{nameof(layoutWorkerClass)} {layoutWorkerClass} is not a {nameof(TrophyLayoutWorker)}";

        if (npcGeneration != null)
        {
            foreach (var error in npcGeneration.ConfigErrors())
                yield return $"{nameof(npcGeneration)}: {error}";
        }

        if (decoratedTrait != null && decoratedThresholds.NullOrEmpty())
            yield return $"{nameof(decoratedTrait)} {decoratedTrait.defName} has no {nameof(decoratedThresholds)}";

        if (decoratedThresholds != null)
        {
            if (decoratedTrait == null)
                yield return $"{nameof(decoratedThresholds)} without {nameof(decoratedTrait)}";

            for (var i = 0; i < decoratedThresholds.Count; i++)
            {
                if (i > 0 && decoratedThresholds[i] <= decoratedThresholds[i - 1])
                    yield return $"{nameof(decoratedThresholds)} must rise, [{i}] {decoratedThresholds[i]} <= [{i - 1}] {decoratedThresholds[i - 1]}";

                var degree = i;
                if (decoratedTrait?.degreeDatas != null && !decoratedTrait.degreeDatas.Any(d => d.degree == degree))
                    yield return $"{nameof(decoratedThresholds)}[{i}]: trait {decoratedTrait.defName} has no degree {i}";
            }
        }
    }
}

// ThingDef.ConfigErrors runs before our Harmony patches, so awarded trophies are checked once after loading
[StaticConstructorOnStartup]
public static class TrophyKindCheck
{
    static TrophyKindCheck()
    {
        var all = DefDatabase<ThingDef>.AllDefsListForReading;
        for (var i = 0; i < all.Count; i++)
        {
            var def = all[i];
            if (AwardedTrophyUtility.IsAwardedTrophyDef(def) && AwardedTrophyUtility.KindOf(def) == null)
                Log.Error($"Config error in {def.defName}: awarded trophy without {nameof(TrophyExtension)} {nameof(TrophyExtension.kind)}");
        }
    }
}
