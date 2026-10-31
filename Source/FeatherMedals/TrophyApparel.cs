using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TribalTrophies;

public class TrophyApparel : Apparel
{
    public CompTrophyContainer Trophies => GetComp<CompTrophyContainer>();

    public override string DescriptionFlavor
    {
        get
        {
            var lines = new List<string>();
            foreach (var trophy in Trophies.Held)
                lines.Add(trophy.LabelNoCount.CapitalizeFirst());

            if (lines.Count == 0)
                return base.DescriptionFlavor;
            return base.DescriptionFlavor + "\n\n" + string.Join("\n", lines);
        }
    }

    public override string DescriptionDetailed => DescriptionFlavor;

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode != DestroyMode.Vanish && mode != DestroyMode.WillReplace && MapHeld != null)
            Trophies.DropAll(PositionHeld, MapHeld);
        base.Destroy(mode);
    }
}
