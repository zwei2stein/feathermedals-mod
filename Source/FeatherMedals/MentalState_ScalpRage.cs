using Verse;
using Verse.AI;

namespace FeatherMedals;

public class MentalState_ScalpRage : MentalState
{
    public Pawn target;

    private bool scalped;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref target, "target");
        Scribe_Values.Look(ref scalped, "scalped");
    }

    public override void PreStart()
    {
        base.PreStart();
        TryFindNewTarget();
    }

    public void Notify_Scalped()
    {
        scalped = true;
    }

    public override void MentalStateTick(int delta)
    {
        base.MentalStateTick(delta);

        if (pawn.MentalState != this)
            return;

        if (scalped)
        {
            RecoverFromState();
            return;
        }

        if (pawn.IsHashIntervalTick(120, delta) && !ScalpRageUtility.IsValidTarget(pawn, target, def))
        {
            // Corpse gone, target scalped by someone else, unreachable, whatever: start over.
            if (!TryFindNewTarget())
                RecoverFromState();
        }
    }

    public bool TryFindNewTarget()
    {
        target = ScalpRageUtility.FindTarget(pawn, def);
        return target != null;
    }

    public override TaggedString GetBeginLetterText()
    {
        if (target == null || def.beginLetter.NullOrEmpty())
            return "";

        return def.beginLetter.Formatted(
            pawn.NameShortColored, target.NameShortColored, pawn.Named("PAWN"), target.Named("TARGET")
        ).AdjustedFor(pawn).Resolve().CapitalizeFirst();
    }
}