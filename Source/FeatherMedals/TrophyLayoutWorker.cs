using UnityEngine;
using Verse;

namespace TribalTrophies;

// Where the worn trophies of one kind are drawn on a pawn, see PawnRenderSubWorker_Trophy.
public abstract class TrophyLayoutWorker
{
    public TrophyKindDef def;

    // how many trophies of this kind are drawn on one pawn
    public abstract int MaxDisplayed { get; }

    public abstract float ScaleFeather { get; }

    public abstract Vector3 OffsetFor(int index, int count, Rot4 facing);

    /// Degrees around Y. Positive = top leans toward +x (clockwise from above).
    public abstract float TiltFor(int index, int count, Rot4 facing);
}

// Feathers: fan over the head, tuned in mod settings
public class TrophyLayoutWorker_Feathers : TrophyLayoutWorker
{
    public override int MaxDisplayed => TribalTrophiesMod.Settings.MaxDisplayedTrophiesFeathers;

    public override float ScaleFeather => TribalTrophiesMod.Settings.TrophyScaleFeather;

    public override Vector3 OffsetFor(int index, int count, Rot4 facing) => TrophyLayout.OffsetFor(index, count, facing);

    public override float TiltFor(int index, int count, Rot4 facing) => TrophyLayout.TiltFor(index, count, facing);
}

// Embroideries: follows sash diagonally over body, starting at shoulder
public class TrophyLayoutWorker_Embroideries : TrophyLayoutWorker
{
    public override int MaxDisplayed => TribalTrophiesMod.Settings.MaxDisplayedTrophiesEmbroideries;
    
    public override float ScaleFeather => TribalTrophiesMod.Settings.TrophyScaleEmbroidery;
    
    // offset to control spacing/overlap.
    public override Vector3 OffsetFor(int index, int count, Rot4 facing)
    {
        throw new System.NotImplementedException();
    }

    // tilt of following sash curve
    public override float TiltFor(int index, int count, Rot4 facing)
    {
        throw new System.NotImplementedException();
    }
}