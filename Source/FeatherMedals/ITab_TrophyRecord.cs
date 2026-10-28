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
        if (SelThing is not FeatherTrophy featherTrophy)
            return rows;

        // adorned: the record of its ceremony; not yet adorned: what it may do
        if (featherTrophy.adorned != null)
            rows.Add(new FeatherTrophyRecordDrawer(featherTrophy, verbose: true));
        else
            rows.Add(new FeatherTrophyDetailDrawer(featherTrophy));
        return rows;
    }

    protected override string Header(int rowCount) => "TribalTrophies_Trophy_TrophyTab_Header".Translate();
}