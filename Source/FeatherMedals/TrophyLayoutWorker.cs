using UnityEngine;
using Verse;

namespace TribalTrophies;

// Where the worn trophies of one kind are drawn on a pawn, see PawnRenderSubWorker_Trophy.
public abstract class TrophyLayoutWorker
{
    public TrophyKindDef def;

    // trophies of this kind are drawn on pawns at all
    public abstract bool DrawOnPawns { get; }

    // how many trophies of this kind are drawn on one pawn
    public abstract int MaxDisplayed { get; }

    public abstract float Scale { get; }

    // facings this kind is drawn in
    public virtual bool DrawsFacing(Rot4 facing) => true;

    // the slot has room on the pawn
    public virtual bool Fits(PawnRenderNode node, PawnDrawParms parms, int index, int count) => true;

    public abstract Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, int index, int count);

    /// Degrees around Y. Positive = top leans toward +x (clockwise from above).
    public abstract float TiltFor(PawnRenderNode node, PawnDrawParms parms, int index, int count);

    // on top of the node's layer, orders overlapping trophies
    public virtual float LayerOffsetFor(PawnRenderNode node, PawnDrawParms parms, int index, int count) => 0f;
}

// Feathers: fan over the head, tuned in mod settings
public class TrophyLayoutWorker_Feathers : TrophyLayoutWorker
{
    public override bool DrawOnPawns => TribalTrophiesMod.Settings.DrawFeatherTrophiesOnPawns;

    public override int MaxDisplayed => TribalTrophiesMod.Settings.MaxDisplayedTrophiesFeathers;

    public override float Scale => TribalTrophiesMod.Settings.TrophyScaleFeather;

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, int index, int count) =>
        TrophyLayout.OffsetFor(index, count, parms.facing);

    public override float TiltFor(PawnRenderNode node, PawnDrawParms parms, int index, int count) =>
        TrophyLayout.TiltFor(index, count, parms.facing);
}

// Embroideries: follows sash diagonally over body, starting at shoulder, curves in SashLayoutExtension
public class TrophyLayoutWorker_Embroideries : TrophyLayoutWorker
{
    public override bool DrawOnPawns => TribalTrophiesMod.Settings.DrawEmbroideryTrophiesOnPawns;

    public override int MaxDisplayed => TribalTrophiesMod.Settings.MaxDisplayedTrophiesEmbroideries;
    
    public override float Scale => TribalTrophiesMod.Settings.TrophyScaleEmbroidery;

    private SashLayoutExtension Sash => field ??= def.GetModExtension<SashLayoutExtension>();

    // trophies past the belt end are not drawn
    public override bool Fits(PawnRenderNode node, PawnDrawParms parms, int index, int count) =>
        TryPlace(node, parms, index, out _, out _);

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, int index, int count) =>
        TryPlace(node, parms, index, out var point, out _) ? point : Vector3.zero;

    // tilt of following sash curve: top of the trophy toward the shoulder, scaled by the follow setting;
    // sideways turns it a further -90 degrees
    public override float TiltFor(PawnRenderNode node, PawnDrawParms parms, int index, int count)
    {
        if (!TryPlace(node, parms, index, out _, out var direction))
            return 0f;

        var alongSash = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;
        var tilt = alongSash * TribalTrophiesMod.Settings.EmbroideryFollowSash;
        if (TribalTrophiesMod.Settings.EmbroiderySideways)
            tilt -= 90f;
        return tilt;
    }

    // shoulder end on top: all embroideries share layerSpread above the sash
    public override float LayerOffsetFor(PawnRenderNode node, PawnDrawParms parms, int index, int count) =>
        count > 0 ? (count - index) * (Sash?.layerSpread ?? 0f) / count : 0f;

    // from the shoulder end: half a trophy, then trophy size plus spacing for each next one;
    // the offset setting moves them off the sash line, positive toward its upper edge
    private bool TryPlace(PawnRenderNode node, PawnDrawParms parms, int index, out Vector3 point, out Vector3 direction)
    {
        point = Vector3.zero;
        direction = Vector3.zero;

        var curve = Sash?.CurveFor(parms.pawn.story?.bodyType);
        if (curve == null)
            return false;

        // sideways, the trophy's width lies along the sash
        var size = (TribalTrophiesMod.Settings.EmbroiderySideways ? node.Props.drawSize.x : node.Props.drawSize.y) * Scale;
        var step = size * (1f + TribalTrophiesMod.Settings.EmbroiderySpacing);
        var distance = Sash.startOffset + size / 2f + index * step;

        // children have a smaller body mesh, curves are for adults
        var bodyFactor = HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn(parms.pawn) / SashLayoutExtension.AdultBodyWidth;
        // facings without curve points (north, east) get no embroideries
        if (!curve.TryGetPoint(parms.facing, distance / bodyFactor, out point, out direction))
            return false;

        var normal = new Vector3(direction.z, 0f, -direction.x);
        point = (point + normal * TribalTrophiesMod.Settings.EmbroiderySashOffset) * bodyFactor;
        return true;
    }
}
