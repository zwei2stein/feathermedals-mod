using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class ThrophyExtension : DefModExtension
{
    public int honorAwarded = 0;
    public List<MedalDynamicTrait> addsTraits;
    public List<MedalDynamicTrait> removesTraits;
}

public class MedalDynamicTrait
{
    public TraitDef trait;
    public int degree = 0;
    public float chance = 1.0f;

    /// <summary>
    /// Readable label for messages, pulling from the specific degree data.
    /// Falls back to the def's label if the degree isn't found.
    /// </summary>
    public string Label
    {
        get
        {
            var data = trait?.DataAtDegree(degree);
            return data?.label ?? trait?.defName ?? "unknown";
        }
    }
}