using RimWorld;
using Verse;

namespace TribalTrophies;

public class ScalpDisplayExtension : DefModExtension
{
    public static bool WornBy(Pawn p)
    {
        if (p.apparel == null)
            return false;
        foreach (var apparel in p.apparel.WornApparel)
        {
            if (apparel.def.HasModExtension<ScalpDisplayExtension>())
                return true;
        }
        return false;
    }
}

public class ThoughtWorker_Precept_WearingScalps : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        return ScalpDisplayExtension.WornBy(p);
    }
}