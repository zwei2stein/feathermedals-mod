using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TribalTrophies;

public class ScalpingGameComponent : GameComponent
{
    private const int ObserveInterval = 600;
    private const float ObserveRadius = 5f;

    private static readonly List<Pawn> observers = new();
    private static readonly List<Thing> scalped = new();
    private static readonly List<Thing> scalps = new();
    private static readonly List<Thing> scalpPoles = new();
    private static readonly List<Thing> scalpBelts = new();
    
    public ScalpingGameComponent(Game game)
    {
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
        CollectScalpBelts(map);
        foreach (var observer in observers)
        {
            RecordFirstSeen(observer, scalped, TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_Observed_Saw_Scalped);
            RecordFirstSeen(observer, scalps, TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Item_Scalp);
            RecordFirstSeen(observer, scalpPoles, TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Building_Pole);
            RecordFirstSeen(observer, scalpBelts, TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_Observed_Scalp_Item_Belt);
        }
        observers.Clear();
        scalped.Clear();
        scalps.Clear();
        scalpPoles.Clear();
        scalpBelts.Clear();
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
        foreach (var thing in map.listerThings.ThingsOfDef(TribalTrophiesDefOf.TribalTrophies_Trophy_Scalping_Scalp))
        {
            scalps.Add(thing);
        }
    }

    private static void CollectScalpPoles(Map map)
    {
        scalpPoles.Clear();
        foreach (var thing in map.listerThings.ThingsOfDef(TribalTrophiesDefOf.TribalTrophies_Scalping_Building_ScalpPole))
        {
            scalpPoles.Add(thing);
        }
    }

    private static void CollectScalpBelts(Map map)
    {
        scalpBelts.Clear();
        foreach (var p in map.mapPawns.AllPawnsSpawned)
        {
            if (ScalpDisplayExtension.WornBy(p))
                scalpBelts.Add(p);
        }
        foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel))
        {
            if (thing.def.HasModExtension<ScalpDisplayExtension>())
                scalpBelts.Add(thing);
        }
    }

    private static bool IsScalped(Pawn p)
    {
        return p is { RaceProps.Humanlike: true }
               && p.health.hediffSet.HasHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped, mustBeVisible: true);
    }

    private static bool CanSee(Pawn observer, Thing thing)
    {
        return thing.Position.InHorDistOf(observer.Position, ObserveRadius)
               && GenSight.LineOfSight(thing.Position, observer.Position, observer.Map, true);
    }
}