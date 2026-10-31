using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class Designator_CutTongue : Designator
{
    protected override DesignationDef Designation => TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_CutTongue()
    {
        defaultLabel = "TribalTrophies_TongueCutting_Designator_Label".Translate();
        defaultDesc = "TribalTrophies_TongueCutting_Designator_Desc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/CutTongue");
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_ExtractSkull;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map))
            return false;

        foreach (var unused in CuttablesInCell(c))
            return true;

        return "TribalTrophies_TongueCutting_Designator_MustDesignateCuttable".Translate();
    }

    public override void DesignateSingleCell(IntVec3 loc)
    {
        foreach (var thing in CuttablesInCell(loc))
            DesignateThing(thing);
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (!TongueCuttingUtility.CanBeCutByAnyone(t))
            return false;

        return Map.designationManager.DesignationOn(t, Designation) == null;
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.AddDesignation(new Verse.Designation(t, Designation));
    }

    private IEnumerable<Thing> CuttablesInCell(IntVec3 c)
    {
        if (c.Fogged(Map))
            yield break;

        var things = c.GetThingList(Map);
        for (var i = 0; i < things.Count; i++)
        {
            if (CanDesignateThing(things[i]).Accepted)
                yield return things[i];
        }
    }
}
