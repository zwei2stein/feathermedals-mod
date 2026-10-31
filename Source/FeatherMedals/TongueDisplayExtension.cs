using RimWorld;
using Verse;

namespace TribalTrophies;

public class TongueDisplayExtension : DefModExtension
{
    public static bool WornBy(Pawn p)
    {
        if (p.apparel == null)
            return false;
        foreach (var apparel in p.apparel.WornApparel)
        {
            if (apparel.def.HasModExtension<TongueDisplayExtension>())
                return true;
        }
        return false;
    }
}

public class ThoughtWorker_Precept_WearingTongues : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        return TongueDisplayExtension.WornBy(p);
    }
}
