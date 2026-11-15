using System.Collections.Generic;
using Verse;

namespace TribalTrophies;

public class ITab_TrophyRecord : ITab_TrophyRows
{
    public ITab_TrophyRecord() : base("TribalTrophies_Trophy_TrophyTab_Label")
    {
    }

    // the selected trophy, or the embroidery a rug is made of
    private AwardedTrophy SelTrophy => SelThing as AwardedTrophy ?? (SelThing as Building_EmbroideryRug)?.Embroidery;

    protected override List<ITrophyRecordRow> Rows()
    {
        var rows = new List<ITrophyRecordRow>();
        var featherTrophy = SelTrophy;
        if (featherTrophy == null)
            return rows;

        // adorned: the record of its ceremony; not yet adorned: what it may do
        if (featherTrophy.IsAdorned)
            rows.Add(new FeatherTrophyRecordDrawer(featherTrophy, verbose: true));
        else
            rows.Add(new AwardedTrophyDetailDrawer(featherTrophy));
        return rows;
    }

    protected override string Header(int rowCount) => SelTrophy?.Kind?.labelShort.CapitalizeFirst();
}