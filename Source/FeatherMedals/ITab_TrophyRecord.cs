using System.Collections.Generic;
using Verse;

namespace FeatherMedals;

public class ITab_TrophyRecord : ITab_TrophyRows
{
    public ITab_TrophyRecord() : base("TribalTrophies_Trophy_TrophyTab_Label")
    {
    }

    protected override List<ITrophyRecordRow> Rows()
    {
        var rows = new List<ITrophyRecordRow>();
        if (SelThing is not FeatherMedal medal)
            return rows;

        // awarded: the record of its ceremony; not yet awarded: what it may do
        if (medal.awarded != null)
            rows.Add(new FeatherTrophyRecordDrawer(medal, verbose: true));
        else
            rows.Add(new FeatherTrophyDetailDrawer(medal));
        return rows;
    }

    protected override string Header(int rowCount) => "TribalTrophies_Trophy_TrophyTab_Header".Translate();
}