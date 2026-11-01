using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies;

// Trophies kept in a trophy container (scalp pole, tongue basket), as vanilla's contents list.
// See vanilla ITab_ContentsBooks.
public class ITab_ContentsTrophies : ITab_ContentsBase
{
    // a copy: dropping a trophy changes the container while the list is drawn
    public override IList<Thing> container =>
        (SelThing as Building_TrophyContainer)?.GetDirectlyHeldThings().ToList() ?? new List<Thing>();

    public override bool IsVisible => SelThing is Building_TrophyContainer && base.IsVisible;

    public override bool VisibleInBlueprintMode => false;

    public ITab_ContentsTrophies()
    {
        labelKey = "TabCasketContents";
        containedItemsKey = "TabCasketContents";
    }
}
