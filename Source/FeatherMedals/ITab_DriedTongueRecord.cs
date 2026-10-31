using System.Collections.Generic;
using Verse;

namespace TribalTrophies;

// A dried tongue.
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
        return rows;
    }

    protected override string Header(int rowCount) => rowCount == 1
        ? "TribalTrophies_TongueCutting_TongueTab_Header_One".Translate()
        : "TribalTrophies_TongueCutting_TongueTab_Header_Many".Translate();
}
