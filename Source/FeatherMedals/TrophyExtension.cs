using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

public class TrophyExtension : DefModExtension
{
    public List<MedalDynamicTrait> addsTraits;
    public List<MedalDynamicTrait> removesTraits;
}

public class MedalDynamicTrait
{
    public TraitDef trait;
    public int degree = 0;
    public float chance = 1.0f;
    
    public string Label
    {
        get
        {
            var data = trait?.DataAtDegree(degree);
            return data?.label ?? trait?.defName ?? "unknown";
        }
    }
}