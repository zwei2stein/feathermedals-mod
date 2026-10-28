using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class TrophyCeremonyLedger : GameComponent
{
    private const int KeepTicks = GenDate.TicksPerYear * 2;

    private List<int> ceremonyTicks = new();
    private int lastCeremonyTick = -1;
    private bool seeded;

    public TrophyCeremonyLedger(Game game)
    {
    }

    public static TrophyCeremonyLedger Instance => Current.Game?.GetComponent<TrophyCeremonyLedger>();

    public void Record(int tick)
    {
        ceremonyTicks.Add(tick);
        if (tick > lastCeremonyTick)
            lastCeremonyTick = tick;
        Prune(tick);
    }

    public int CountWithin(int ticks)
    {
        var now = Find.TickManager.TicksGame;
        var count = 0;
        for (var i = 0; i < ceremonyTicks.Count; i++)
        {
            if (now - ceremonyTicks[i] < ticks)
                count++;
        }
        return count;
    }

    public int YearsWithoutCeremony()
    {
        var now = Find.TickManager.TicksGame;
        var baseline = lastCeremonyTick < 0 ? 0 : lastCeremonyTick;
        return now <= baseline ? 0 : (now - baseline) / GenDate.TicksPerYear;
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        
        HonoringWarriorsUtil.ResetCache();

        if (seeded)
            return;
        
        seeded = true;
        SeedFromExistingTrophies();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref ceremonyTicks, "ceremonyTicks", LookMode.Value);
        Scribe_Values.Look(ref lastCeremonyTick, "lastCeremonyTick", -1);
        Scribe_Values.Look(ref seeded, "seeded");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            ceremonyTicks ??= new List<int>();
    }

    private void Prune(int now)
    {
        ceremonyTicks.RemoveAll(t => now - t > KeepTicks);
    }

    private void SeedFromExistingTrophies()
    {
        var found = new HashSet<int>();

        foreach (var pawn in PawnsFinder.AllMapsAndWorld_Alive)
        {
            // NPC trophies do not imply ceremonies
            if (pawn.Faction is not { IsPlayer: true })
                continue;
            ScanList(pawn.apparel?.WornApparel, found);
            var inventory = pawn.inventory?.innerContainer;
            if (inventory == null)
                continue;
            for (var i = 0; i < inventory.Count; i++)
                Note(inventory[i], found);
        }

        foreach (var map in Find.Maps)
        {
            if (!map.IsPlayerHome)
                continue;
            var apparel = map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            for (var i = 0; i < apparel.Count; i++)
                Note(apparel[i], found);
        }

        var now = Find.TickManager.TicksGame;
        foreach (var tick in found)
        {
            if (tick > lastCeremonyTick)
                lastCeremonyTick = tick;
            if (now - tick <= KeepTicks)
                ceremonyTicks.Add(tick);
        }
    }

    private static void ScanList(List<Apparel> list, HashSet<int> found)
    {
        if (list == null)
            return;
        for (var i = 0; i < list.Count; i++)
            Note(list[i], found);
    }

    private static void Note(Thing thing, HashSet<int> found)
    {
        // generated pasts can date before the game started, no colony ceremony then
        if (thing is FeatherTrophy { adorned.tick: >= 0 } featherTrophy)
            found.Add(featherTrophy.adorned.tick);
    }
}