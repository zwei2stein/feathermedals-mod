using System.Collections.Generic;
using Verse;

namespace TribalTrophies;

// Picture of a storage by how much it holds: the entry with the highest minCount that the count reaches.
public class FillGraphicsExtension : DefModExtension
{
    public List<FillGraphic> fillGraphics = new();

    public Graphic GraphicFor(Thing storage, int count)
    {
        FillGraphic best = null;
        foreach (var fill in fillGraphics)
        {
            if (fill.minCount <= count && (best == null || fill.minCount > best.minCount))
                best = fill;
        }
        return best?.GraphicFor(storage);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        foreach (var fill in fillGraphics)
        {
            if (fill.texPath.NullOrEmpty())
                yield return $"fill graphic for {fill.minCount} has no texPath";
        }
    }
}

public class FillGraphic
{
    public int minCount;
    [NoTranslate] public string texPath;

    private GraphicData graphicData;

    // the storage's own graphic data with another texture: same shader, size, and stuff or paint color
    public Graphic GraphicFor(Thing storage)
    {
        if (graphicData == null)
        {
            graphicData = new GraphicData();
            graphicData.CopyFrom(storage.def.graphicData);
            graphicData.texPath = texPath;
            graphicData.maskPath = null;
        }
        return graphicData.GraphicColoredFor(storage);
    }
}
