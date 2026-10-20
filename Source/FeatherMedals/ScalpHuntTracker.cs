using System.Collections.Generic;
using Verse;

namespace FeatherMedals;

public class ScalpHunt : IExposable
{

    public int goal;
    public int escaped;
    // several groups, message once
    public bool goalReachedAnnounced;

    public bool GoalReached => escaped >= goal;

    public void ExposeData()
    {
        Scribe_Values.Look(ref goal, "goal");
        Scribe_Values.Look(ref escaped, "escaped");
        Scribe_Values.Look(ref goalReachedAnnounced, "goalReachedAnnounced");
    }
}

public class ScalpHuntTracker : MapComponent
{
    private Dictionary<int, ScalpHunt> hunts = new();
    private List<int> tmpRaidIds;
    private List<ScalpHunt> tmpHunts;
    
    public ScalpHuntTracker(Map map) : base(map)
    {
    }

    public static ScalpHuntTracker For(Map map) => map?.GetComponent<ScalpHuntTracker>();
    
    public void Register(int raidId, int goal)
    {
        if (!hunts.ContainsKey(raidId))
            hunts[raidId] = new ScalpHunt { goal = goal };
    }

    public ScalpHunt Get(int raidId) => hunts.TryGetValue(raidId, out var hunt) ? hunt : null;

    public void Forget(int raidId) => hunts.Remove(raidId);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref hunts, "hunts", LookMode.Value, LookMode.Deep, ref tmpRaidIds, ref tmpHunts);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            hunts ??= new Dictionary<int, ScalpHunt>();
    }
}