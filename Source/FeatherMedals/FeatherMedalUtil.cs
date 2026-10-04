using System.Collections.Generic;
using Verse;

namespace FeatherMedals;

public static class FeatherMedalUtil
{
    public static bool IsMedalDef(ThingDef def) => def?.thingClass == typeof(FeatherMedal);

    public static bool TryGetDisplaySlot(Pawn pawn, FeatherMedal medal, out int index, out int count)
    {
        index = -1;
        count = 0;

        var worn = pawn?.apparel?.WornApparel;
        if (worn == null)
            return false;

        var maxDisplayed = MedalMod.Settings.MaxDisplayedTrophies;
        var groups = medal.def.apparel.bodyPartGroups;
        var seen = 0;

        for (var i = 0; i < worn.Count; i++)
        {
            if (worn[i] is not FeatherMedal other)
                continue;
            if (seen++ >= maxDisplayed)
                break;
            if (!SameGroups(other.def.apparel.bodyPartGroups, groups))
                continue;

            if (other == medal)
                index = count;
            count++;
        }

        return index >= 0;
    }
    
    private static bool SameGroups(List<BodyPartGroupDef> a, List<BodyPartGroupDef> b)
    {
        if (a == b)
            return true;
        if (a == null || b == null || a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!b.Contains(a[i]))
                return false;
        }
        return true;
    }
}