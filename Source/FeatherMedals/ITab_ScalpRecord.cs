using System.Collections.Generic;
using Verse;

namespace FeatherMedals;

// A scalp, or the scalps hung on a belt or a scalp pole.
public class ITab_ScalpRecord : ITab_TrophyRows
{
    public ITab_ScalpRecord() : base("TribalTrophies_Scalping_ScalpTab_Label")
    {
    }

    protected override List<ITrophyRecordRow> Rows()
    {
        var rows = new List<ITrophyRecordRow>();
        foreach (var scalp in Scalps(SelThing))
            rows.Add(new ScalpRecordDrawer(scalp));
        return rows;
    }

    protected override string Header(int rowCount) => rowCount == 1
        ? "TribalTrophies_Scalping_ScalpTab_Header_One".Translate()
        : "TribalTrophies_Scalping_ScalpTab_Header_Many".Translate();

    private static IEnumerable<Scalp> Scalps(Thing thing)
    {
        if (thing is Scalp scalp)
            yield return scalp;

        if (thing is Building_ScalpPole pole)
            foreach (var scalpItem in pole.StoredScalps())
            {
                yield return scalpItem;
            }

        var container = thing?.TryGetComp<CompTrophyContainer>();
        if (container != null)
            foreach (var held in container.Held)
            {
                if (held is Scalp heldScalp)
                    yield return heldScalp;
            }
    }
}