using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class ScalpingGameComponent : GameComponent
{
    private const int ObserveInterval = 600;
    private const float ObserveRadius = 5f;

    private static readonly List<Pawn> observers = new();
    private static readonly List<Thing> scalped = new();
    private static readonly List<Thing> scalps = new();
    private static readonly List<Thing> scalpPoles = new();

    private bool ideosMigrated;

    public ScalpingGameComponent(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();

        if (ideosMigrated)
            return;
        ideosMigrated = true;
        ScalpingPrecepts.AddStanceToIdeosMissingIssue();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ideosMigrated, "ideosMigrated");
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % ObserveInterval != 0)
            return;

        foreach (var map in Find.Maps)
            Observe(map);
    }

    private static void Observe(Map map)
    {
        observers.Clear();
        foreach (var p in map.mapPawns.AllPawnsSpawned)
        {
            if (IsObserver(p))
                observers.Add(p);
        }
        if (observers.Count == 0)
            return;

        CollectScalped(map);
        CollectScalps(map);
        CollectScalpPoles(map);
        foreach (var observer in observers)
        {
            RecordFirstSeen(observer, scalped, FeatherMedalDefOf.FeatherTrophies_Scalping_Observed_Saw_Scalped);
            RecordFirstSeen(observer, scalps, FeatherMedalDefOf.FeatherTrophies_Scalping_Observed_Scalp);
            RecordFirstSeen(observer, scalpPoles, FeatherMedalDefOf.FeatherTrophies_Scalping_Observed_ScalpPole);
        }
        observers.Clear();
        scalped.Clear();
        scalps.Clear();
        scalpPoles.Clear();
    }

    private static void RecordFirstSeen(Pawn observer, List<Thing> things, HistoryEventDef eventDef)
    {
        foreach (var thing in things)
        {
            if (thing == observer || !CanSee(observer, thing))
                continue;
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
                eventDef,
                observer.Named(HistoryEventArgsNames.Doer), thing.Named(HistoryEventArgsNames.Subject)));
            break;
        }
    }

    private static bool IsObserver(Pawn p)
    {
        return p.RaceProps.Humanlike
               && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony)
               && p.needs?.mood != null
               && ScalpingPrecepts.StanceOf(p) == ScalpStance.Forbidden
               && p.Awake()
               && !PawnUtility.IsBiologicallyOrArtificiallyBlind(p);
    }

    private static void CollectScalped(Map map)
    {
        scalped.Clear();
        foreach (var p in map.mapPawns.AllPawnsSpawned)
        {
            if (IsScalped(p))
                scalped.Add(p);
        }
        foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
        {
            // Dessicated corpses are not drawn scalped.
            if (thing is Corpse corpse && corpse.GetRotStage() != RotStage.Dessicated && IsScalped(corpse.InnerPawn))
                scalped.Add(corpse);
        }
    }

    private static void CollectScalps(Map map)
    {
        scalps.Clear();
        foreach (var thing in map.listerThings.ThingsOfDef(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalp))
        {
            scalps.Add(thing);
        }
    }

    private static void CollectScalpPoles(Map map)
    {
        scalpPoles.Clear();
        foreach (var thing in map.listerThings.ThingsOfDef(FeatherMedalDefOf.FeatherTrophies_ScalpPole))
        {
            scalpPoles.Add(thing);
        }
    }

    private static bool IsScalped(Pawn p)
    {
        return p is { RaceProps.Humanlike: true }
               && p.health.hediffSet.HasHediff(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalped, mustBeVisible: true);
    }

    private static bool CanSee(Pawn observer, Thing thing)
    {
        return thing.Position.InHorDistOf(observer.Position, ObserveRadius)
               && GenSight.LineOfSight(thing.Position, observer.Position, observer.Map, true);
    }
}