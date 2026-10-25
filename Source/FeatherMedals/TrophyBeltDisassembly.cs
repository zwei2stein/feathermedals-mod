using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FeatherMedals;

// like vanilla skull extraction
public class WorkGiver_DisassembleTrophyBelt : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (var designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Designation_Disassemble))
            yield return designation.target.Thing;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Designation_Disassemble);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.Destroyed || t.TryGetComp<CompTrophyContainer>() == null)
            return false;
        if (t.Map.designationManager.DesignationOn(t, FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Designation_Disassemble) == null)
            return false;
        return pawn.CanReserve(t, ignoreOtherReservations: forced);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Job_Disassemble, t);
    }
}

public class JobDriver_DisassembleTrophyBelt : JobDriver
{
    private const TargetIndex BeltInd = TargetIndex.A;

    private Thing Belt => job.GetTarget(BeltInd).Thing;
    private CompTrophyContainer Trophies => Belt.TryGetComp<CompTrophyContainer>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(BeltInd), job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(BeltInd);
        this.FailOn(() => Map.designationManager.DesignationOn(Belt, FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Designation_Disassemble) == null);

        yield return Toils_Goto.GotoThing(BeltInd, PathEndMode.ClosestTouch);

        var props = Trophies.Props;
        var work = Toils_General.Wait(props.disassembleTicks, BeltInd);
        work.WithProgressBarToilDelay(BeltInd);
        if (props.disassembleEffecter != null)
            work.WithEffect(() => props.disassembleEffecter, BeltInd);
        if (props.disassembleSound != null)
            work.PlaySustainerOrSound(props.disassembleSound);
        yield return work;

        yield return Toils_General.Do(() =>
        {
            Map.designationManager.RemoveDesignation(Map.designationManager.DesignationOn(Belt, FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Designation_Disassemble));
            Trophies.Disassemble();
        });
    }
}

public class FloatMenuOptionProvider_DisassembleTrophyBelt : FloatMenuOptionProvider
{
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    private static WorkGiverDef GiverDef => FeatherMedalDefOf.TribalTrophies_Apparel_Belt_WorkGiver_Disassemble;
    
    protected override bool AppliesInt(FloatMenuContext context)
    {
        var pawn = context.FirstSelectedPawn;
        return pawn.workSettings != null && pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>() != null;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        var trophies = clickedThing.TryGetComp<CompTrophyContainer>();
        if (trophies == null)
            return null;

        var pawn = context.FirstSelectedPawn;
        var worker = (WorkGiver_Scanner)GiverDef.Worker;
        var label = "TribalTrophies_Trophy_Common_Disassemble_Label".Translate(clickedThing.def.label.Named("ITEM")).Resolve();

        var reason = DisabledReason(pawn, worker, clickedThing);
        if (reason != null)
        {
            var cannot = "CannotGenericWork".Translate(GiverDef.verb, clickedThing.Label).Resolve();
            return new FloatMenuOption($"{cannot}: {reason}", null);
        }

        void Order()
        {
            trophies.MarkForDisassembly();
            var job = worker.JobOnThing(pawn, clickedThing, true);
            job.workGiverDef = GiverDef;
            pawn.jobs.TryTakeOrderedJobPrioritizedWork(job, worker, clickedThing.Position);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label.CapitalizeFirst(), Order), pawn, clickedThing);
    }
    
    private static string DisabledReason(Pawn pawn, WorkGiver_Scanner worker, Thing belt)
    {
        var capacity = worker.MissingRequiredCapacity(pawn);
        if (capacity != null)
            return "CannotMissingHealthActivities".Translate(capacity.label);

        if (pawn.WorkTagIsDisabled(GiverDef.workTags))
            return "CannotPrioritizeWorkGiverDisabled".Translate(GiverDef.label);

        var workType = GiverDef.workType;
        if (pawn.WorkTypeIsDisabled(workType))
            return "CannotPrioritizeWorkTypeDisabled".Translate(workType.gerundLabel);
        if (pawn.workSettings.GetPriority(workType) == 0)
            return "CannotPrioritizeNotAssignedToWorkType".Translate(workType.gerundLabel);

        if (pawn.CurJobDef == FeatherMedalDefOf.TribalTrophies_Apparel_Belt_Job_Disassemble && pawn.CurJob.targetA.Thing == belt)
            return "CannotGenericAlreadyAm".Translate(GiverDef.gerund, belt.LabelShort, belt);

        if (belt.IsForbidden(pawn))
        {
            if (belt.Position.InAllowedArea(pawn))
                return "CannotPrioritizeForbidden".Translate(belt.Label, belt);
            return "CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + $" ({pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.Label})";
        }

        if (!pawn.CanReserve(belt))
        {
            var reserver = pawn.Map.reservationManager.FirstRespectedReserver(belt, pawn);
            return reserver != null ? "ReservedBy".Translate(reserver.LabelShort, reserver) : "Reserved".Translate();
        }

        if (!pawn.CanReach(belt, worker.PathEndMode, Danger.Deadly))
            return "NoPath".Translate().CapitalizeFirst();

        return null;
    }
}