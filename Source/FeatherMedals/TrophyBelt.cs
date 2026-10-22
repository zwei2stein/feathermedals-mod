using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class Patch_GenRecipe_MakeRecipeProducts
{
    public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, List<Thing> ingredients)
    {
        foreach (var product in __result)
        {
            product.TryGetComp<CompTrophyContainer>()?.TakeTrophiesFrom(ingredients);
            yield return product;
        }
    }
}

public class TrophyBelt : Apparel
{
    private Texture2D[] textures; // by Rot4.AsInt
    private bool composeTried;
    private Graphic_RuntimeTexture groundGraphic;
    private Graphic_RuntimeTexture wornGraphic;

    public CompTrophyContainer Trophies => GetComp<CompTrophyContainer>();

    private Texture2D[] Textures
    {
        get
        {
            if (textures == null && !composeTried)
            {
                composeTried = true;
                textures = TrophyBeltTextureFactory.Compose(def.graphicData.texPath, GetColorIgnoringTainted(), Trophies.Held, Trophies.Props.slots);
            }
            return textures;
        }
    }

    public override Graphic Graphic
    {
        get
        {
            if (groundGraphic == null && Textures != null)
                groundGraphic = new Graphic_RuntimeTexture(Textures[Rot4.South.AsInt], def.graphicData);
            return groundGraphic ?? base.Graphic;
        }
    }

    public Graphic WornGraphic
    {
        get
        {
            if (wornGraphic == null && Textures != null)
                wornGraphic = new Graphic_RuntimeTexture(Textures, def.graphicData);
            return wornGraphic;
        }
    }

    public override Texture UIIconOverride => Textures?[Rot4.South.AsInt];

    public override string DescriptionFlavor
    {
        get
        {
            var lines = new List<string>();
            foreach (var trophy in Trophies.Held)
                lines.Add(trophy.LabelNoCount.CapitalizeFirst());

            if (lines.Count == 0)
                return base.DescriptionFlavor;
            return base.DescriptionFlavor + "\n\n" + string.Join("\n", lines);
        }
    }

    public override string DescriptionDetailed => DescriptionFlavor;

    public override void Notify_ColorChanged()
    {
        ReleaseGraphics();
        composeTried = false;
        base.Notify_ColorChanged();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode != DestroyMode.Vanish && mode != DestroyMode.WillReplace && MapHeld != null)
            Trophies.DropAll(PositionHeld, MapHeld);
        base.Destroy(mode);
        ReleaseGraphics();
        // destroyed belt falls back to def graphic/icon
        composeTried = true;
    }

    private void ReleaseGraphics()
    {
        groundGraphic?.Release();
        groundGraphic = null;
        wornGraphic?.Release();
        wornGraphic = null;
        if (textures != null)
        {
            foreach (var texture in textures)
                UnityEngine.Object.Destroy(texture);
            textures = null;
        }
    }
}

public class CompProperties_TrophyContainer : CompProperties
{
    public List<ThingDef> trophyDefs = new();
    public List<TrophySlot> slots = new();

    public CompProperties_TrophyContainer()
    {
        compClass = typeof(CompTrophyContainer);
    }
}

public class TrophySlot
{
    public Vector3? north;
    public Vector3? east;
    public Vector3? south;
    public Vector3? west;

    public Vector3? For(Rot4 facing)
    {
        if (facing == Rot4.North)
            return north;
        if (facing == Rot4.East)
            return east;
        if (facing == Rot4.West)
            return west;
        return south;
    }
}

public class CompTrophyContainer : ThingComp, IThingHolder
{
    private ThingOwner<Thing> innerContainer;

    public CompTrophyContainer()
    {
        innerContainer = new ThingOwner<Thing>(this);
    }

    public CompProperties_TrophyContainer Props => (CompProperties_TrophyContainer)props;

    public IReadOnlyList<Thing> Held => innerContainer.InnerListForReading;

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public void TakeTrophiesFrom(List<Thing> ingredients)
    {
        foreach (var ingredient in ingredients.ToArray())
        {
            if (!Props.trophyDefs.Contains(ingredient.def))
                continue;
            ingredients.Remove(ingredient);
            Add(ingredient);
        }
    }

    public void Add(Thing trophy)
    {
        if (trophy.Spawned)
            trophy.DeSpawn();
        while (trophy.stackCount > 1)
            innerContainer.TryAdd(trophy.SplitOff(1), canMergeWithExistingStacks: false);
        innerContainer.TryAdd(trophy, canMergeWithExistingStacks: false);
    }

    public void DropAll(IntVec3 cell, Map map)
    {
        innerContainer.TryDropAll(cell, map, ThingPlaceMode.Near);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!parent.Spawned)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "TribalTrophies_Trophy_Gizmo_Disassemble_Label".Translate(parent.def.label.Named("ITEM")),
            defaultDesc = "TribalTrophies_Trophy_Gizmo_Disassemble_Desc".Translate(),
            icon = TrophyTextures.DisassembleIcon,
            action = Disassemble
        };
    }

    private void Disassemble()
    {
        DropAll(parent.Position, parent.Map);
        parent.Destroy();
    }

    public override string CompInspectStringExtra()
    {
        var names = new List<string>();
        foreach (var trophy in Held)
        {
            var name = TrophyNames.VictimName(trophy);
            if (!name.NullOrEmpty())
                names.Add(name);
        }
        if (names.Count == 0)
            return null;
        return "TribalTrophies_Trophy_Inspect_TakenFrom".Translate(names.ToCommaList().Named("NAMES"));
    }

    public override float GetStatOffset(StatDef stat)
    {
        if (stat != StatDefOf.MarketValue)
            return 0f;

        float extra = 0f;
        foreach (var trophy in Held)
            extra += trophy.MarketValue - trophy.def.BaseMarketValue;
        return extra;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref innerContainer, "trophies", this);
    }
}

public static class TrophyNames
{
    private static readonly AccessTools.FieldRef<CompHasSources, List<string>> SourcesOf =
        AccessTools.FieldRefAccess<CompHasSources, List<string>>("sources");

    public static string VictimName(Thing trophy)
    {
        if (trophy is Scalp scalp)
            return scalp.VictimName;

        var sources = trophy.TryGetComp<CompHasSources>();
        if (sources == null)
            return null;
        var names = SourcesOf(sources);
        return names.NullOrEmpty() ? null : names[0];
    }
}

public class StatPart_TerrorFreeWearersOnly : StatPart
{
    public List<ThingDef> thingDefs;

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is Apparel { Wearer: { } wearer } apparel
            && thingDefs != null && thingDefs.Contains(apparel.def)
            && (wearer.IsSlave || wearer.IsPrisoner))
            val = 0f;
    }

    public override string ExplanationPart(StatRequest req) => null;
}