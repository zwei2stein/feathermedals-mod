using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Sash the embroideries are sewn on, on the embroidery kind def, see TrophyLayoutWorker_Embroideries.
public class SashLayoutExtension : DefModExtension
{
    // curves are for the adult body mesh
    public const float AdultBodyWidth = 1.5f;

    public string texPath;

    public Color defaultColor = Color.white;
    public BodyTypeDef fallbackBodyType;

    public float startOffset;

    public float layerSpread = 4f;

    public List<SashCurve> curves;

    public SashCurve CurveFor(BodyTypeDef bodyType)
    {
        SashCurve fallback = null;
        for (var i = 0; i < curves.Count; i++)
        {
            if (curves[i].bodyType == bodyType)
                return curves[i];
            if (curves[i].bodyType == fallbackBodyType)
                fallback = curves[i];
        }
        return fallback;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (texPath.NullOrEmpty())
            yield return $"{nameof(texPath)} is missing";

        if (curves.NullOrEmpty())
        {
            yield return $"{nameof(curves)} is missing";
            yield break;
        }

        foreach (var curve in curves)
        {
            if (curve.bodyType == null)
                yield return $"{nameof(curves)}: {nameof(SashCurve.bodyType)} is missing";
            if (curve.south == null)
                yield return $"{nameof(curves)}: {curve.bodyType?.defName} {nameof(SashCurve.south)} is missing";
            else if (curve.south.Length <= 0f)
                yield return $"{nameof(curves)}: {curve.bodyType?.defName} {nameof(SashCurve.south)} has no length, {nameof(SashBezier.start)} = {nameof(SashBezier.end)}";
            if (curve.west != null && curve.west.Length <= 0f)
                yield return $"{nameof(curves)}: {curve.bodyType?.defName} {nameof(SashCurve.west)} has no length, {nameof(SashBezier.start)} = {nameof(SashBezier.end)}";
        }

        if (fallbackBodyType == null)
            yield return $"{nameof(fallbackBodyType)} is missing";
        else if (!curves.Any(curve => curve.bodyType == fallbackBodyType))
            yield return $"{nameof(fallbackBodyType)} {fallbackBodyType.defName} has no curve";

        if (layerSpread <= 0f)
            yield return $"{nameof(layerSpread)} {layerSpread} must be positive";
    }
}

public class SashCurve
{
    public BodyTypeDef bodyType;

    // shoulder to belt, facing south
    public SashBezier south;

    // from the shoulder toward the front
    public SashBezier west;

    // embroideries are drawn only in facings with a path
    public SashBezier PathFor(Rot4 facing)
    {
        if (facing == Rot4.South)
            return south;
        if (facing == Rot4.West)
            return west;
        return null;
    }

    public bool TryGetPoint(Rot4 facing, float distance, out Vector3 point, out Vector3 direction)
    {
        point = Vector3.zero;
        direction = Vector3.zero;

        var path = PathFor(facing);
        return path != null && path.TryGetPoint(distance, out point, out direction);
    }
}

public class SashBezier
{
    // pieces the path is measured in
    private const int Segments = 32;

    public Vector3 start;
    public Vector3 control;
    public Vector3 end;

    public float Length => Lengths[Segments];

    private Vector3 PointAt(float t)
    {
        var u = 1f - t;
        return u * u * start + 2f * u * t * control + t * t * end;
    }

    // not normalized, may be zero
    private Vector3 TangentAt(float t) => 2f * (1f - t) * (control - start) + 2f * t * (end - control);

    private float[] Lengths
    {
        get
        {
            if (field != null)
                return field;

            field = new float[Segments + 1];
            var previous = PointAt(0f);
            for (var i = 1; i <= Segments; i++)
            {
                var current = PointAt((float)i / Segments);
                field[i] = field[i - 1] + (current - previous).magnitude;
                previous = current;
            }
            return field;
        }
    }
    
    public bool TryGetPoint(float distance, out Vector3 point, out Vector3 direction)
    {
        var all = Lengths;
        var t = 0f;
        if (distance > 0f)
        {
            if (distance > all[Segments])
            {
                point = Vector3.zero;
                direction = Vector3.zero;
                return false;
            }

            var i = 1;
            while (i < Segments && all[i] < distance)
                i++;

            var piece = all[i] - all[i - 1];
            var within = piece > 0f ? (distance - all[i - 1]) / piece : 0f;
            t = (i - 1 + within) / Segments;
        }

        point = PointAt(t);
        direction = TangentAt(t);
        if (direction.sqrMagnitude <= 0f)
            direction = end - start;
        direction = direction.normalized;
        return true;
    }
}

public static class TrophySashUtility
{
    private static readonly Dictionary<ThingDef, ThingDef> MaterialCache = new Dictionary<ThingDef, ThingDef>();

    // leather or fabric ingredient of the recipe making the trophy, null = none
    public static ThingDef MaterialOf(ThingDef trophyDef)
    {
        if (MaterialCache.TryGetValue(trophyDef, out var material))
            return material;

        material = FindMaterial(trophyDef);
        MaterialCache[trophyDef] = material;
        return material;
    }

    private static ThingDef FindMaterial(ThingDef trophyDef)
    {
        var recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            if (recipe.ProducedThingDef != trophyDef || recipe.ingredients == null)
                continue;

            foreach (var ingredient in recipe.ingredients)
            {
                foreach (var def in ingredient.filter.AllowedThingDefs)
                {
                    if (IsSashMaterial(def))
                        return def;
                }
            }
        }
        return null;
    }

    private static bool IsSashMaterial(ThingDef def)
    {
        var categories = def.stuffProps?.categories;
        return categories != null
               && (categories.Contains(StuffCategoryDefOf.Fabric) || categories.Contains(StuffCategoryDefOf.Leathery));
    }

    // most common material of the displayed trophies of the kind, a tie goes to the one drawn first
    public static Color SashColor(Pawn pawn, TrophyKindDef kind, SashLayoutExtension sash)
    {
        var counts = new Dictionary<ThingDef, int>();
        var firstSeen = new List<ThingDef>();

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            if (apparel is not AwardedTrophy trophy || trophy.Kind != kind
                || !AwardedTrophyUtility.TryGetDisplaySlot(pawn, trophy, out _, out _))
                continue;

            var material = MaterialOf(trophy.def);
            if (material == null)
                continue;

            if (counts.TryGetValue(material, out var count))
                counts[material] = count + 1;
            else
            {
                counts[material] = 1;
                firstSeen.Add(material);
            }
        }

        ThingDef best = null;
        var bestCount = 0;
        // in drawing order, so only a higher count replaces an earlier material
        foreach (var material in firstSeen)
        {
            if (counts[material] > bestCount)
            {
                best = material;
                bestCount = counts[material];
            }
        }

        return best?.stuffProps.color ?? sash.defaultColor;
    }
}

// Sash under the embroideries, one per pawn: only the first displayed trophy draws it.
public class PawnRenderNode_TrophySash : PawnRenderNode
{
    public PawnRenderNode_TrophySash(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
    }

    protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
    {
        if (apparel is not AwardedTrophy trophy || pawn.story?.bodyType == null || pawn.apparel == null)
            yield break;

        var sash = trophy.Kind?.GetModExtension<SashLayoutExtension>();
        if (sash == null)
            yield break;

        // body types without a sash texture (children, other mods) get no sash
        var path = $"{sash.texPath}_{pawn.story.bodyType.defName}";
        if (ContentFinder<Texture2D>.Get(path + "_south", false) == null)
            yield break;

        yield return GraphicDatabase.Get<Graphic_Multi>(path, ShaderFor(pawn), Vector2.one,
            TrophySashUtility.SashColor(pawn, trophy.Kind, sash));
    }
}

public class PawnRenderSubWorker_TrophySash : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node.apparel is not AwardedTrophy trophy)
            return false;

        // every facing, also those without embroideries
        return trophy.Kind.LayoutWorker.DrawOnPawns
               && AwardedTrophyUtility.TryGetDisplaySlot(parms.pawn, trophy, out var index, out _)
               && index == 0;
    }
}
