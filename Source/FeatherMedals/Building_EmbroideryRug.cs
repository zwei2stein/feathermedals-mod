using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TribalTrophies;

public class Building_EmbroideryRug : Building, IThingHolder
{
    private static AwardedTrophy pendingEmbroidery;

    // rug or its blueprint with an embroidery's texture
    private static readonly Dictionary<(ThingDef, ThingDef), GraphicData> RugGraphicDatas =
        new Dictionary<(ThingDef, ThingDef), GraphicData>();

    private ThingOwner<Thing> innerContainer;

    public Building_EmbroideryRug()
    {
        innerContainer = new ThingOwner<Thing>(this, true);
    }

    public static bool IsRugDef(BuildableDef def)
    {
        return def is ThingDef thingDef && thingDef.thingClass != null
                                         && typeof(Building_EmbroideryRug).IsAssignableFrom(thingDef.thingClass);
    }

    public static void SetPendingEmbroidery(AwardedTrophy embroidery) => pendingEmbroidery = embroidery;

    public static AwardedTrophy TakePendingEmbroidery()
    {
        var embroidery = pendingEmbroidery;
        pendingEmbroidery = null;
        return embroidery;
    }

    public static GraphicData GraphicDataFor(ThingDef rugDef, ThingDef embroideryDef)
    {
        if (embroideryDef?.graphicData == null || !AwardedTrophyUtility.IsAwardedTrophyDef(embroideryDef))
            return null;

        var key = (rugDef, embroideryDef);
        if (RugGraphicDatas.TryGetValue(key, out var graphicData))
            return graphicData;
        
        graphicData = new GraphicData();
        graphicData.CopyFrom(rugDef.graphicData);
        graphicData.texPath = embroideryDef.graphicData.texPath;
        graphicData.maskPath = null;
        RugGraphicDatas[key] = graphicData;
        return graphicData;
    }

    public AwardedTrophy Embroidery => innerContainer.Count > 0 ? innerContainer[0] as AwardedTrophy : null;

    // takes the embroidery out of the rug, null = none
    public AwardedTrophy TakeEmbroidery()
    {
        var embroidery = Embroidery;
        if (embroidery != null)
            innerContainer.Remove(embroidery);
        return embroidery;
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)GetDirectlyHeldThings());
    }

    public override void PostMake()
    {
        base.PostMake();
        var embroidery = TakePendingEmbroidery();
        if (embroidery != null)
            innerContainer.TryAdd(embroidery);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        // god mode, dev spawn: a new embroidery is spawned
        if (!respawningAfterLoad && Embroidery == null && AwardedTrophyUtility.IsAwardedTrophyDef(Stuff))
        {
            var embroidery = ThingMaker.MakeThing(Stuff);
            embroidery.TryGetComp<CompArt>()?.InitializeArt(ArtGenerationContext.Colony);
            innerContainer.TryAdd(embroidery);
        }

        GetComp<CompArt_EmbroideryRug>()?.SyncTitle();

        base.SpawnSetup(map, respawningAfterLoad);
    }

    // deconstructing gives the embroidery back. destroyed rug destroys it
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        var map = Map;
        var position = Position;
        var embroidery = mode == DestroyMode.Deconstruct && map != null ? TakeEmbroidery() : null;

        base.Destroy(mode);

        if (embroidery != null)
            GenPlace.TryPlaceThing(embroidery, position, map, ThingPlaceMode.Near);
        innerContainer.ClearAndDestroyContents();
    }

    public override Graphic Graphic
    {
        get
        {
            var graphicData = GraphicDataFor(def, Embroidery?.def);
            return graphicData != null ? graphicData.GraphicColoredFor(this) : base.Graphic;
        }
    }

    public override string LabelNoCount
    {
        get
        {
            var embroidery = Embroidery;
            if (embroidery == null)
                return base.LabelNoCount;

            var rugLabel = def.GetModExtension<EmbroideryRugExtension>()?.labelShort ?? def.label;
            return "TribalTrophies_Embroidery_Rug_Label"
                .Translate(rugLabel.Named("RUG"), embroidery.def.label.Named("EMBROIDERY")).Resolve();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            innerContainer = new ThingOwner<Thing>(this, true);
    }
}

public class EmbroideryRugExtension : DefModExtension
{
    public string labelShort;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (labelShort.NullOrEmpty())
            yield return $"{nameof(labelShort)} is missing";
    }
}

public class Blueprint_EmbroideryRug : Blueprint_Build
{
    public override Graphic Graphic
    {
        get
        {
            var graphicData = Building_EmbroideryRug.GraphicDataFor(def, stuffToUse);
            return graphicData != null ? graphicData.Graphic : base.Graphic;
        }
    }
}

public class PlaceWorker_EmbroideryRug : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        // vanilla would let them stack: no rug on any cell under this one
        foreach (var cell in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
        {
            if (!cell.InBounds(map))
                continue;

            var things = cell.GetThingList(map);
            foreach (var other in things)
            {
                if (other == thingToIgnore || other == thing)
                    continue;

                // a rug of any size, or its blueprint or frame
                if (Building_EmbroideryRug.IsRugDef(GenConstruct.BuiltDefOf(other.def)))
                    return "IdenticalThingExists".Translate();
            }
        }
        return true;
    }
}

// Construction destroys the delivered materials. For a rug, the embroidery is taken out first and handed to the new rug.
[HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
public static class Patch_Frame_CompleteConstruction_EmbroideryRug
{
    public static void Prefix(Frame __instance)
    {
        Building_EmbroideryRug.SetPendingEmbroidery(null);
        if (!Building_EmbroideryRug.IsRugDef(__instance.BuildDef))
            return;

        var resources = __instance.resourceContainer;
        for (var i = 0; i < resources.Count; i++)
        {
            if (resources[i] is AwardedTrophy embroidery)
            {
                resources.Remove(embroidery);
                Building_EmbroideryRug.SetPendingEmbroidery(embroidery);
                return;
            }
        }
    }

    // if case of weird error in construction
    public static void Finalizer()
    {
        var embroidery = Building_EmbroideryRug.TakePendingEmbroidery();
        if (embroidery != null && !embroidery.Destroyed)
            embroidery.Destroy();
    }
}

// A minified rug being deconstructed gives the embroidery back, like deconstructing building does.
[HarmonyPatch(typeof(MinifiedThing), nameof(MinifiedThing.Destroy))]
public static class Patch_MinifiedThing_Destroy_EmbroideryRug
{
    public static void Prefix(MinifiedThing __instance, DestroyMode mode,
        out (AwardedTrophy embroidery, IntVec3 position, Map map) __state)
    {
        __state = default;
        if (mode == DestroyMode.Deconstruct && __instance.Spawned && __instance.InnerThing is Building_EmbroideryRug rug)
            __state = (rug.TakeEmbroidery(), __instance.Position, __instance.Map);
    }

    public static void Postfix((AwardedTrophy embroidery, IntVec3 position, Map map) __state)
    {
        if (__state.embroidery != null)
            GenPlace.TryPlaceThing(__state.embroidery, __state.position, __state.map, ThingPlaceMode.Near);
    }
}

[HarmonyPatch(typeof(GhostUtility), nameof(GhostUtility.GhostGraphicFor))]
public static class Patch_GhostUtility_GhostGraphicFor_EmbroideryRug
{
    public static void Prefix(ref Graphic baseGraphic, ThingDef thingDef, ThingDef stuff)
    {
        if (stuff == null || !Building_EmbroideryRug.IsRugDef(thingDef))
            return;

        var graphicData = Building_EmbroideryRug.GraphicDataFor(thingDef, stuff);
        if (graphicData != null)
            baseGraphic = graphicData.Graphic;
    }
}

[HarmonyPatch(typeof(BuildCopyCommandUtility), nameof(BuildCopyCommandUtility.BuildCommand))]
public static class Patch_BuildCopyCommandUtility_BuildCommand_EmbroideryRug
{
    public static void Postfix(BuildableDef buildable, ThingDef stuff, Command __result)
    {
        // without stuff the result is the build designator itself
        if (stuff == null || __result is not Command_Action || !Building_EmbroideryRug.IsRugDef(buildable))
            return;

        var graphicData = Building_EmbroideryRug.GraphicDataFor((ThingDef)buildable, stuff);
        if (graphicData != null)
            __result.icon = graphicData.Graphic.MatSingle.mainTexture;
    }
}

// Fix to make rugs "stuffabe" with embroidery
[HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ProcessInput))]
public static class Patch_Designator_Build_ProcessInput_EmbroideryRug
{
    private static readonly AccessTools.FieldRef<Designator_Build, bool> WriteStuff =
        AccessTools.FieldRefAccess<Designator_Build, bool>("writeStuff");

    public static bool Prefix(Designator_Build __instance)
    {
        var rugDef = __instance.PlacingDef;
        if (!Building_EmbroideryRug.IsRugDef(rugDef))
            return true;

        var map = Find.CurrentMap;
        var options = new List<FloatMenuOption>();
        foreach (var stuffDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (!stuffDef.IsStuff || !stuffDef.stuffProps.CanMake(rugDef))
                continue;
            if (!DebugSettings.godMode && map.listerThings.ThingsOfDef(stuffDef).Count == 0)
                continue;

            var localStuffDef = stuffDef;
            options.Add(new FloatMenuOption(localStuffDef.LabelCap.ToString(), () =>
            {
                Select(__instance);
                __instance.SetStuffDef(localStuffDef);
                WriteStuff(__instance) = true;
            }, localStuffDef));
        }

        if (options.Count == 0)
        {
            Messages.Message("TribalTrophies_Embroidery_Rug_NoEmbroideries".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        options.SortBy(option => option.Label);
        Find.WindowStack.Add(new FloatMenu(options)
        {
            onCloseCallback = () => WriteStuff(__instance) = true
        });
        Select(__instance);
        return false;
    }

    // what Designator.ProcessInput does
    private static void Select(Designator_Build designator)
    {
        designator.activateSound?.PlayOneShotOnCamera();
        Find.DesignatorManager.Select(designator);
    }
}
