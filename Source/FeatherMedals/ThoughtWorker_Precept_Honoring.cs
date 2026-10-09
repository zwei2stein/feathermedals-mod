using RimWorld;
using Verse;

namespace FeatherMedals;

public abstract class ThoughtWorker_Precept_Honoring : ThoughtWorker_Precept
{
    protected abstract int Count(Pawn p);

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        if (!HonoringWarriorsUtil.IsEligible(p))
            return ThoughtState.Inactive;
        if (!HonoringWarriorsUtil.MeetsExpectation(p, def.minExpectationForNegativeThought))
            return ThoughtState.Inactive;

        var count = Count(p);
        return count > 0 ? ThoughtState.ActiveAtStage(count - 1) : ThoughtState.Inactive;
    }
}

public class ThoughtWorker_Precept_TrophiesWorn : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p) => HonoringWarriorsUtil.WornTrophies(p);
}

public class ThoughtWorker_Precept_NoTrophyWorn : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p) => HonoringWarriorsUtil.WornTrophies(p) == 0 ? 1 : 0;
}

public class ThoughtWorker_Precept_OthersWearing : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p) => HonoringWarriorsUtil.OthersWearing(p);
}

public class ThoughtWorker_Precept_OthersNotWearing : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p) => HonoringWarriorsUtil.OthersNotWearing(p);
}

public class ThoughtWorker_Precept_RecentCeremonies : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p)
        => TrophyCeremonyLedger.Instance?.CountWithin(GenDate.TicksPerYear) ?? 0;
}

public class ThoughtWorker_Precept_YearsWithoutCeremony : ThoughtWorker_Precept_Honoring
{
    protected override int Count(Pawn p)
        => TrophyCeremonyLedger.Instance?.YearsWithoutCeremony() ?? 0;
}

public class ThoughtWorker_Precept_Social_TrophyWearer : ThoughtWorker_Precept_Social
{
    protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
    {
        return HonoringWarriorsUtil.IsEligible(p)
               && otherPawn.RaceProps.Humanlike
               && HonoringWarriorsUtil.WornTrophies(otherPawn) > 0;
    }
}