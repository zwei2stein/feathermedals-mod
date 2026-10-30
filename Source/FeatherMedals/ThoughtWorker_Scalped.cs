using RimWorld;
using Verse;

namespace TribalTrophies;

public class ThoughtWorker_Scalped : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return ScalpingUtility.IsScalped(p);
    }
}