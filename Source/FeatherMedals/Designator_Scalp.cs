using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Designator_Scalp : Designator
{
    protected override DesignationDef Designation => TribalTrophiesDefOf.TribalTrophies_Scalping_Designation_ScalpDesignation;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_Scalp()
    {
        defaultLabel = "TribalTrophies_Scalping_Designator_Label".Translate();
        defaultDesc = "TribalTrophies_Scalping_Designator_Desc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/Scalp");
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_ExtractSkull;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map))
            return false;

        foreach (var unused in ScalpablesInCell(c))
            return true;

        return "TribalTrophies_Scalping_Designator_MustDesignateScalpable".Translate();
    }

    public override void DesignateSingleCell(IntVec3 loc)
    {
        foreach (var thing in ScalpablesInCell(loc))
            DesignateThing(thing);
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (!ScalpingUtility.CanBeScalpedByAnyone(t))
            return false;

        return Map.designationManager.DesignationOn(t, Designation) == null;
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.AddDesignation(new Verse.Designation(t, Designation));
    }

    private IEnumerable<Thing> ScalpablesInCell(IntVec3 c)
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