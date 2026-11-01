using System.Collections.Generic;
using Verse;

namespace TribalTrophies;

// A dried tongue, the dried tongues on a necklace, or in a tongue basket.
public class ITab_DriedTongueRecord : ITab_TrophyRows
{
    public ITab_DriedTongueRecord() : base("TribalTrophies_TongueCutting_TongueTab_Label")
    {
    }

    protected override List<ITrophyRecordRow> Rows()
    {
        var rows = new List<ITrophyRecordRow>();
        if (SelThing is DriedTongue tongue)
            rows.Add(new TrophyBodyPartRecordDrawer(tongue));

        if (SelThing is Building_TongueBasket basket)
            foreach (var storedTongue in basket.StoredTongues())
            {
                rows.Add(new TrophyBodyPartRecordDrawer(storedTongue));
            }

        var container = SelThing?.TryGetComp<CompTrophyContainer>();
        if (container != null)
            foreach (var heldTongue in container.DriedTongues)
            {
                rows.Add(new TrophyBodyPartRecordDrawer(heldTongue));
            }

        return rows;
    }

    protected override string Header(int rowCount) => rowCount == 1
        ? "TribalTrophies_TongueCutting_TongueTab_Header_One".Translate()
        : "TribalTrophies_TongueCutting_TongueTab_Header_Many".Translate();
}
