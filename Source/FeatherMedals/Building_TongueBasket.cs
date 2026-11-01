using System.Collections.Generic;
using Verse;

namespace TribalTrophies;

// Tongues are inside, not drawn: the basket's picture shows how full it is.
public class Building_TongueBasket : Building_TrophyContainer
{
    public List<DriedTongue> StoredTongues()
    {
        var tongues = new List<DriedTongue>();

        foreach (var trophy in StoredTrophies())
        {
            if (trophy is DriedTongue tongue)
                tongues.Add(tongue);
        }

        return tongues;
    }

    public override Graphic Graphic =>
        def.GetModExtension<FillGraphicsExtension>()?.GraphicFor(this, TrophyCount) ?? base.Graphic;

    protected override void Notify_ContentsChanged()
    {
        base.Notify_ContentsChanged();
        if (Spawned)
            DirtyMapMesh(Map);
    }

    protected override string LabelWithOne(string storageLabel, string victimName)
    {
        return "TribalTrophies_TongueBasket_Label_OneTongue"
            .Translate(storageLabel.Named("BASKET"), victimName.Named("VICTIM")).Resolve();
    }

    protected override string LabelWithMany(string storageLabel, int count)
    {
        return "TribalTrophies_TongueBasket_Label_ManyTongues"
            .Translate(storageLabel.Named("BASKET"), count.Named("COUNT")).Resolve();
    }
}

public class FocusStrengthOffset_TongueBasketTongues : FocusStrengthOffset_StoredTrophies
{
    protected override ThingDef StorageDef => TribalTrophiesDefOf.TribalTrophies_TongueCutting_Building_TongueBasket;

    protected override string Explanation(int count, int max, string perTrophy, string value)
    {
        return "TribalTrophies_TongueBasket_Meditation_FocusOffset".Translate(
            count.Named("COUNT"),
            max.Named("MAX"),
            perTrophy.Named("PERTONGUE"),
            value.Named("VALUE")
        ).Resolve();
    }

    protected override string ExplanationAbstract(int max, string perTrophy, string value)
    {
        return "TribalTrophies_TongueBasket_Meditation_FocusOffsetAbstract".Translate(
            max.Named("MAX"),
            perTrophy.Named("PERTONGUE"),
            value.Named("VALUE")
        ).Resolve();
    }
}
