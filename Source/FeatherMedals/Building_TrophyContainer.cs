using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TribalTrophies;

// Building that keeps body part trophies inside itself (scalp pole, tongue basket): hauled in like books to a bookcase,
// not lying on the map, so no item spread, labels or clicks of its own. Label and description name what it holds.
// See vanilla Building_Bookcase.
public abstract class Building_TrophyContainer :
    Building,
    IThingHolderEvents<Thing>,
    IHaulEnroute,
    IStorageGroupMember,
    IHaulDestination,
    IStoreSettingsParent,
    IHaulSource,
    IThingHolder,
    ISearchableContents
{
    private ThingOwner<Thing> innerContainer;
    private StorageSettings settings;
    private StorageGroup storageGroup;

    protected Building_TrophyContainer()
    {
        innerContainer = new ThingOwner<Thing>(this, false);
    }

    public static int CapacityOf(ThingDef def) => def.building.maxItemsInCell * def.Size.Area;

    public int Capacity => CapacityOf(def);

    // label with the victim of the only trophy, or with the count of many
    protected abstract string LabelWithOne(string storageLabel, string victimName);
    protected abstract string LabelWithMany(string storageLabel, int count);

    // trophy put in or taken out
    protected virtual void Notify_ContentsChanged()
    {
    }

    public List<TrophyBodyPart> StoredTrophies()
    {
        var trophies = new List<TrophyBodyPart>();

        foreach (var thing in innerContainer.InnerListForReading)
        {
            if (thing is TrophyBodyPart trophy)
                trophies.Add(trophy);
        }

        trophies.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));
        return trophies;
    }

    public int TrophyCount => StoredTrophies().Count;

    // NPC settlements fill it directly.
    public bool TryStore(Thing thing) => Accepts(thing) && innerContainer.TryAdd(thing);

    // ===== container =====

    public ThingOwner SearchableContents => innerContainer;

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)GetDirectlyHeldThings());
    }

    public void Notify_ItemAdded(Thing item)
    {
        MapHeld?.listerHaulables.Notify_AddedThing(item);
        Notify_ContentsChanged();
    }

    public void Notify_ItemRemoved(Thing item)
    {
        Notify_ContentsChanged();
    }

    // ===== storage =====

    public bool StorageTabVisible => true;

    public StorageSettings GetStoreSettings() => storageGroup != null ? storageGroup.GetStoreSettings() : settings;

    public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

    public bool HaulSourceEnabled => true;

    public bool HaulDestinationEnabled => true;

    public bool Accepts(Thing t)
    {
        return (innerContainer.Count < Capacity || innerContainer.InnerListForReading.Contains(t))
               && GetStoreSettings().AllowedToAccept(t)
               && innerContainer.CanAcceptAnyOf(t);
    }

    public int SpaceRemainingFor(ThingDef _) => Capacity - innerContainer.Count;

    public void Notify_SettingsChanged()
    {
        if (!Spawned)
            return;
        MapHeld.listerHaulables.Notify_HaulSourceChanged(this);
    }

    StorageGroup IStorageGroupMember.Group
    {
        get => storageGroup;
        set => storageGroup = value;
    }

    bool IStorageGroupMember.DrawConnectionOverlay => Spawned;

    Map IStorageGroupMember.Map => MapHeld;

    string IStorageGroupMember.StorageGroupTag => def.building.storageGroupTag;

    StorageSettings IStorageGroupMember.StoreSettings => GetStoreSettings();

    StorageSettings IStorageGroupMember.ParentStoreSettings => GetParentStoreSettings();

    StorageSettings IStorageGroupMember.ThingStoreSettings => settings;

    bool IStorageGroupMember.DrawStorageTab => true;

    bool IStorageGroupMember.ShowRenameButton => Faction == Faction.OfPlayer;

    // ===== building =====

    public override void PostMake()
    {
        base.PostMake();
        settings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
            settings.CopyFrom(def.building.defaultStorageSettings);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (storageGroup == null || map == storageGroup.Map)
            return;
        var storeSettings = storageGroup.GetStoreSettings();
        storageGroup.RemoveMember(this);
        storageGroup = null;
        settings.CopyFrom(storeSettings);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (storageGroup != null)
        {
            storageGroup.RemoveMember(this);
            storageGroup = null;
        }
        if (mode != DestroyMode.WillReplace)
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        base.DeSpawn(mode);
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        StorageGroupUtility.DrawSelectionOverlaysFor(this);
    }

    public override string GetInspectString()
    {
        var inspectString = base.GetInspectString();
        if (!Spawned)
            return inspectString;

        if (!inspectString.NullOrEmpty())
            inspectString += "\n";
        if (storageGroup != null)
        {
            inspectString += $"{"StorageGroupLabel".Translate()}: {storageGroup.RenamableLabel.CapitalizeFirst()} ";
            inspectString += storageGroup.MemberCount <= 1
                ? "(" + "OneBuilding".Translate().Resolve() + ")\n"
                : "(" + "NumBuildings".Translate((NamedArgument)storageGroup.MemberCount).CapitalizeFirst().Resolve() + ")\n";
        }
        inspectString += "TribalTrophies_TrophyBodyPart_Inspect_Stored".Translate(
            innerContainer.Count.Named("COUNT"), Capacity.Named("MAX")).Resolve();
        return inspectString;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        foreach (var gizmo in StorageSettingsClipboard.CopyPasteGizmosFor(GetStoreSettings()))
            yield return gizmo;
        if (StorageTabVisible && MapHeld != null)
        {
            foreach (var gizmo in StorageGroupUtility.StorageGroupMemberGizmos(this))
                yield return gizmo;
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (var option in HaulSourceUtility.GetFloatMenuOptions(this, selPawn))
            yield return option;
        foreach (var option in base.GetFloatMenuOptions(selPawn))
            yield return option;
    }

    public override string LabelNoCount
    {
        get
        {
            var trophies = StoredTrophies();
            var storageLabel = GenLabel.ThingLabel(def, Stuff);

            if (trophies.Count == 1 && !trophies[0].Victim.Name.NullOrEmpty())
                return LabelWithOne(storageLabel, trophies[0].Victim.Name);

            if (trophies.Count > 0)
                return LabelWithMany(storageLabel, trophies.Count);

            return base.LabelNoCount;
        }
    }

    public override string DescriptionFlavor
    {
        get
        {
            var trophies = StoredTrophies();
            if (trophies.Count == 0)
                return base.DescriptionFlavor;

            var lines = new List<string>();
            foreach (var trophy in trophies)
            {
                lines.Add(trophy.LabelNoCount);
            }

            return base.DescriptionFlavor + "\n\n" + string.Join("\n", lines);
        }
    }

    public override string DescriptionDetailed => DescriptionFlavor;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Deep.Look(ref settings, "settings", this);
        Scribe_References.Look(ref storageGroup, "storageGroup");

        // scalp poles saved before 1.1.0 were shelf-like storage, with no container
        if (Scribe.mode == LoadSaveMode.LoadingVars && innerContainer == null)
            innerContainer = new ThingOwner<Thing>(this, false);
    }
}

// Meditation focus from the trophies stored, up to the container's capacity.
public abstract class FocusStrengthOffset_StoredTrophies : FocusStrengthOffset
{
    // capacity is known even before anything is built (info card)
    protected abstract ThingDef StorageDef { get; }

    protected abstract string Explanation(int count, int max, string perTrophy, string value);
    protected abstract string ExplanationAbstract(int max, string perTrophy, string value);

    private int Capacity => Building_TrophyContainer.CapacityOf(StorageDef);

    public override float GetOffset(Thing parent, Pawn user = null)
    {
        if (parent is Building_TrophyContainer container)
            return offset * container.TrophyCount;

        return 0f;
    }

    public override float MaxOffset(Thing parent = null)
    {
        return offset * Capacity;
    }

    public override string GetExplanation(Thing parent)
    {
        if (!parent.Spawned || parent is not Building_TrophyContainer container)
            return GetExplanationAbstract(parent.def);

        return Explanation(container.TrophyCount, Capacity, offset.ToString("0%"), GetOffset(parent).ToStringWithSign("0%"));
    }

    public override string GetExplanationAbstract(ThingDef def = null)
    {
        return ExplanationAbstract(Capacity, offset.ToString("0%"), MaxOffset().ToString("0%"));
    }
}
