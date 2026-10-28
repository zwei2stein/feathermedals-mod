using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

// A skull, or the skulls hung on a belt.
public class ITab_SkullRecord : ITab_TrophyRows
{
    public ITab_SkullRecord() : base("TribalTrophies_Trophy_SkullTab_Label")
    {
    }

    protected override List<ITrophyRecordRow> Rows()
    {
        var rows = new List<ITrophyRecordRow>();
        var thing = SelThing;

        // loose skulls: one row per remembered victim, nameless ones show nothing
        if (thing?.def == ThingDefOf.Skull)
            foreach (var name in TrophyNames.VictimNames(thing))
                rows.Add(new SkullRecordDrawer(thing, name));

        // belt: every skull, named or not
        var container = thing?.TryGetComp<CompTrophyContainer>();
        if (container != null)
            foreach (var held in container.Held)
            {
                if (held.def == ThingDefOf.Skull)
                    rows.Add(new SkullRecordDrawer(held, TrophyNames.VictimName(held)));
            }

        return rows;
    }

    protected override string Header(int rowCount) => rowCount == 1
        ? "TribalTrophies_Trophy_SkullTab_Header_One".Translate()
        : "TribalTrophies_Trophy_SkullTab_Header_Many".Translate();
}