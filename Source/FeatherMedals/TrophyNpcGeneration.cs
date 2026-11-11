using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

// NPC pawns spawning with trophies of one kind, see NpcTrophyGenerator
public class TrophyNpcGeneration
{
    // player settings of this kind
    public Type settingsWorkerClass;

    // chance of extra trophies at Neutral stance by faction tech level, a level not listed = none
    public List<TrophyNpcTechChance> techChances;

    // every Honoring Warriors stance: factor on the extra trophies chance and base rolls
    public List<TrophyNpcStance> stances;

    // trophies a new faction leader gets on top of worn ones
    public int leaderTrophies = 8;

    // bonus rolls: one per combat passion level up to maxPassionRolls,
    // one each for melee and shooting above highSkill, one when older than highAge
    public int maxPassionRolls = 4;
    public int highSkill = 10;
    public int highAge = 50;

    public TrophyNpcSettingsWorker SettingsWorker => field ??= (TrophyNpcSettingsWorker)Activator.CreateInstance(settingsWorkerClass);

    public float TechChance(TechLevel level)
    {
        for (var i = 0; i < techChances.Count; i++)
        {
            if (techChances[i].techLevel == level)
                return techChances[i].chance;
        }
        return 0f;
    }

    public TrophyNpcStance StanceFor(HonoringStance stance)
    {
        for (var i = 0; i < stances.Count; i++)
        {
            if (stances[i].stance == stance)
                return stances[i];
        }
        return null;
    }

    public IEnumerable<string> ConfigErrors()
    {
        if (settingsWorkerClass == null || !typeof(TrophyNpcSettingsWorker).IsAssignableFrom(settingsWorkerClass))
            yield return $"{nameof(settingsWorkerClass)} {settingsWorkerClass} is not a {nameof(TrophyNpcSettingsWorker)}";

        if (techChances.NullOrEmpty())
            yield return $"{nameof(techChances)} is missing";
        else
        {
            var seen = new HashSet<TechLevel>();
            foreach (var entry in techChances)
            {
                if (!seen.Add(entry.techLevel))
                    yield return $"{nameof(techChances)}: {entry.techLevel} is listed twice";
                if (entry.chance < 0f || entry.chance > 1f)
                    yield return $"{nameof(techChances)}: {entry.techLevel} chance {entry.chance} is outside 0..1";
            }
        }

        if (stances == null)
            yield return $"{nameof(stances)} is missing";
        else
        {
            foreach (HonoringStance stance in Enum.GetValues(typeof(HonoringStance)))
            {
                var count = stances.Count(entry => entry.stance == stance);
                if (count != 1)
                    yield return $"{nameof(stances)}: {stance} must be listed once, found {count}";
            }

            foreach (var entry in stances)
            {
                if (entry.chanceFactor < 0f)
                    yield return $"{nameof(stances)}: {entry.stance} {nameof(TrophyNpcStance.chanceFactor)} {entry.chanceFactor} is negative";
                if (entry.baseRolls < 0)
                    yield return $"{nameof(stances)}: {entry.stance} {nameof(TrophyNpcStance.baseRolls)} {entry.baseRolls} is negative";
            }
        }

        if (leaderTrophies < 0)
            yield return $"{nameof(leaderTrophies)} {leaderTrophies} is negative";
        if (maxPassionRolls < 0)
            yield return $"{nameof(maxPassionRolls)} {maxPassionRolls} is negative";
    }
}

public class TrophyNpcTechChance
{
    public TechLevel techLevel;
    public float chance;
}

public class TrophyNpcStance
{
    public HonoringStance stance;
    public float chanceFactor;
    public int baseRolls;
}

// player settings of one kind's NPC trophies, see TribalTrophiesSettings
public abstract class TrophyNpcSettingsWorker
{
    public abstract bool Generate { get; }

    public abstract TechLevel MinTech { get; }

    public abstract TechLevel MaxTech { get; }
}

public class TrophyNpcSettingsWorker_Feathers : TrophyNpcSettingsWorker
{
    public override bool Generate => TribalTrophiesMod.Settings.GenerateNpcFeatherTrophies;

    public override TechLevel MinTech => TribalTrophiesMod.Settings.NpcFeatherTrophyMinTech;

    public override TechLevel MaxTech => TribalTrophiesMod.Settings.NpcFeatherTrophyMaxTech;
}

public class TrophyNpcSettingsWorker_Embroideries : TrophyNpcSettingsWorker
{
    public override bool Generate => TribalTrophiesMod.Settings.GenerateNpcEmbroideryTrophies;

    public override TechLevel MinTech => TribalTrophiesMod.Settings.NpcEmbroideryTrophyMinTech;

    public override TechLevel MaxTech => TribalTrophiesMod.Settings.NpcEmbroideryTrophyMaxTech;
}
