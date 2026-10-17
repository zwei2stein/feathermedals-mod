using Verse;
using Verse.AI;

namespace FeatherMedals;

public class MentalStateWorker_ScalpRage : MentalStateWorker
{
    public override bool StateCanOccur(Pawn pawn)
    {
        return base.StateCanOccur(pawn)
               && ScalpRageUtility.CanDoViolentBreak(pawn)
               && ScalpRageUtility.FindTarget(pawn, def) != null;
    }
}