using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

// Laying the rug makes no new art and the builder is not its author.
public class CompArt_EmbroideryRug : CompArt
{
    private CompArt EmbroideryArt => (parent as Building_EmbroideryRug)?.Embroidery?.TryGetComp<CompArt>();

    public override bool Active
    {
        get
        {
            var art = EmbroideryArt;
            return art != null && art.Active;
        }
    }

    public void SyncTitle()
    {
        var art = EmbroideryArt;
        Title = art != null && art.Active ? art.Title : null;
    }
    
    protected override void InitializeArtInternal(Thing relatedThing, ArtGenerationContext source) => SyncTitle();

    public override void JustCreatedBy(Pawn pawn)
    {
    }

    public override string CompInspectStringExtra() => EmbroideryArt?.CompInspectStringExtra();

    public override string GetDescriptionPart() => EmbroideryArt?.GetDescriptionPart();

    public override TaggedString GenerateImageDescription() => EmbroideryArt?.GenerateImageDescription() ?? default;
}

public class StatPart_EmbroideryRugQuality : StatPart
{
    // vanilla's quality factors of the stat
    private StatPart_Quality QualityPart => field ??= parentStat.parts.OfType<StatPart_Quality>().FirstOrDefault();

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (TryGetEmbroidery(req, out var embroidery))
            QualityPart?.TransformValue(StatRequest.For(embroidery), ref val);
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (QualityPart == null || !TryGetEmbroidery(req, out var embroidery))
            return null;

        var multiplier = 1f;
        QualityPart.TransformValue(StatRequest.For(embroidery), ref multiplier);
        embroidery.TryGetQuality(out var quality);
        return "TribalTrophies_Embroidery_Rug_StatsReport_Quality".Translate(quality.GetLabel().Named("QUALITY"))
               + ": x" + multiplier.ToStringPercent();
    }

    private static bool TryGetEmbroidery(StatRequest req, out AwardedTrophy embroidery)
    {
        embroidery = (req.Thing as Building_EmbroideryRug)?.Embroidery;
        return embroidery != null && embroidery.TryGetQuality(out _);
    }
}
