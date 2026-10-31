using RimWorld;
using Verse;
using Verse.AI;

namespace TribalTrophies;

public class FloatMenuOptionProvider_CutTongue : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        return Build(clickedPawn, context);
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        return clickedThing is Corpse ? Build(clickedThing, context) : null;
    }

    private static FloatMenuOption Build(Thing target, FloatMenuContext context)
    {
        var actor = context.FirstSelectedPawn;
        // Forbidden by ideology: still listed (disabled) when it would be allowed otherwise.
        var forbidden = ScalpingPrecepts.StanceOf(actor) == ScalpStance.Forbidden;
        if (!(forbidden ? TongueCuttingUtility.CanBeCut(target) : TongueCuttingUtility.CanBeCut(target, actor)))
            return null;

        string label = "TribalTrophies_TongueCutting_FloatMenu_CutTongue".Translate(target.LabelShortCap.Named("TARGET"));
        if (forbidden)
            return new FloatMenuOption(label + " (" + "IdeoligionForbids".Translate() + ")", null);

        bool angers = TrophyBodyPartUtility.WouldAngerFaction(target);
        if (angers)
            label += ": " + "AngersFaction".Translate().CapitalizeFirst();

        if (actor.WorkTagIsDisabled(WorkTags.Violent))
            return new FloatMenuOption(label + " (" + "IsIncapableOfViolenceShort".Translate((NamedArgument)(Thing)actor) + ")", null);

        if (!actor.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            return new FloatMenuOption(label + " (" + "NoPath".Translate() + ")", null);

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(label, () =>
            {
                Job job = JobMaker.MakeJob(TribalTrophiesDefOf.TribalTrophies_TongueCutting_Job_CutTongueJob, target);
                actor.jobs.TryTakeOrderedJob(job);
                if (angers)
                {
                    Messages.Message(
                        "TribalTrophies_TongueCutting_Message_WillAnger".Translate(target.Named("TARGET"), ((Pawn)target).Faction.Named("FACTION")).CapitalizeFirst(),
                        target, MessageTypeDefOf.CautionInput, false);
                }
            }, revalidateClickTarget: target),
            actor, (LocalTargetInfo)target);
    }
}
