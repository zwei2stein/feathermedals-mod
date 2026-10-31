using RimWorld;
using Verse;

namespace TribalTrophies;

public static class TongueCuttingUtility
{
    public static TongueCuttingExtension Tuning => field ??=
        TribalTrophiesDefOf.TribalTrophies_Trophy_TongueCutting_DriedTongue.GetModExtension<TongueCuttingExtension>() ?? new TongueCuttingExtension();

    // Every rule that disqualifies target from losing its tongue, from the player's point of view.
    // Same rules as scalping:
    // * Body is held still (fresh corpse, captive, or downed if it would resist)
    // * Not evil to do (no own people, children, non-hostiles, non-guilty prisoners)
    // Own rule:
    // * Still has its own natural tongue
    public static bool CanBeCut(Thing thing)
    {
        Pawn pawn = TrophyBodyPartUtility.PawnOf(thing);

        if (!CanPhysicallyBeCut(pawn, thing))
            return false;

        return TrophyBodyPartUtility.IsNotEvilToTakeFrom(pawn, Faction.OfPlayer);
    }

    // What this cutter's ideology allows. Honorable: evil check applies. Merciless: no evil check. Forbidden: nothing.
    public static bool CanBeCut(Thing thing, Pawn cutter)
    {
        var pawn = TrophyBodyPartUtility.PawnOf(thing);

        if (!CanPhysicallyBeCut(pawn, thing))
            return false;

        switch (ScalpingPrecepts.StanceOf(cutter))
        {
            case ScalpStance.Forbidden:
                return false;
            case ScalpStance.Merciless:
                return true;
            default:
                return TrophyBodyPartUtility.IsNotEvilToTakeFrom(pawn, cutter.Faction ?? Faction.OfPlayer);
        }
    }

    // To be able to show gizmo in mixed ideo colony.
    public static bool CanBeCutByAnyone(Thing thing)
    {
        if (CanBeCut(thing))
            return true;

        return ScalpingPrecepts.AnyMercilessColonist() && CanPhysicallyBeCut(TrophyBodyPartUtility.PawnOf(thing), thing);
    }

    private static bool CanPhysicallyBeCut(Pawn pawn, Thing thing)
    {
        if (pawn == null || !thing.Spawned || !HasTongueToTake(pawn))
            return false;

        return TrophyBodyPartUtility.IsHeldStill(pawn, thing);
    }

    // Body can still give a tongue: humanlike flesh with its own natural tongue.
    public static bool HasTongueToTake(Pawn pawn)
    {
        return TrophyBodyPartUtility.HasTrophyBody(pawn) && TongueOf(pawn) != null;
    }

    // Natural tongue: present, not replaced, nothing artificial above it (an artificial jaw has no natural tongue in it).
    public static BodyPartRecord TongueOf(Pawn pawn)
    {
        var hediffSet = pawn.health.hediffSet;
        foreach (var part in hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.Tongue))
        {
            if (!hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                return part;
        }
        return null;
    }

    public static void Provoke(Pawn target)
    {
        // Trying to cut out a tongue turns faction hostile.
        var faction = target.Faction;
        TrophyBodyPartUtility.MakeFactionHostile(target, TribalTrophiesDefOf.TribalTrophies_TongueCutting_HistoryEvent_CutTongueOfMember);

        Messages.Message(
            "TribalTrophies_TongueCutting_Message_Provoked".Translate(
                Faction.OfPlayer.Named("PLAYERFACTION"), target.Named("TARGET"), faction.Named("FACTION")).CapitalizeFirst(),
            target, MessageTypeDefOf.NegativeEvent);
    }

    public static DriedTongue DoCutTongue(Pawn cutter, Thing target)
    {
        var victim = TrophyBodyPartUtility.PawnOf(target);
        if (victim == null || !CanBeCut(target, cutter))
            return null;

        var alive = !victim.Dead;
        var tongue = TongueOf(victim);
        var faction = victim.HomeFaction;
        var map = target.MapHeld;
        var pos = target.PositionHeld;

        // Before hediffs: dried tongue remembers victim as they were (value).
        var driedTongue = DriedTongue.Create(victim, cutter, EventTime.Now(map));

        // cut out, not surgically removed
        var missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, victim, tongue);
        missing.lastInjury = HediffDefOf.Cut;
        victim.health.AddHediff(missing, tongue);

        if (alive)
        {
            // A tongue does not bleed by itself (inside part) and neither does the jaw (solid).
            // The head bleeds for the mouth; the jaw cut is for the record.
            var head = HeadAbove(tongue);
            if (head != null)
                TrophyBodyPartUtility.AddInjuryLeavingPart(victim, head, HediffDefOf.Cut, Tuning.headCutSeverity);

            // part holding the tongue: jaw on humans
            var holder = tongue.parent;
            if (holder != null && holder != head)
                TrophyBodyPartUtility.AddInjuryLeavingPart(victim, holder, HediffDefOf.Cut, Tuning.jawCutSeverity);

            victim.needs?.mood?.thoughts.memories.TryGainMemory(
                TribalTrophiesDefOf.TribalTrophies_TongueCutting_ThoughtSocial_CutOutMyTongue, cutter);
            victim.needs?.mood?.thoughts.memories.TryGainMemory(
                TribalTrophiesDefOf.TribalTrophies_TongueCutting_Thought_TongueCutOut);
        }

        TrophyBodyPartUtility.MakeBlood(victim, pos, map);

        driedTongue = TrophyBodyPartUtility.PlaceNear(driedTongue, cutter);

        // only the player's own tongue cutting changes goodwill with the player
        var loss = alive ? Tuning.goodwillLossAlive : Tuning.goodwillLossDead;
        TrophyBodyPartUtility.LoseGoodwill(faction, cutter, loss,
            TribalTrophiesDefOf.TribalTrophies_TongueCutting_HistoryEvent_CutTongueOfMember, target);

        // messages are taking sides - heroic colonist, villain outsider.
        TrophyBodyPartUtility.RecordTaking(cutter, victim,
            TribalTrophiesDefOf.TribalTrophies_TongueCutting_HistoryEvent_CutTongue,
            TribalTrophiesDefOf.TribalTrophies_TongueCutting_Tale_CutTongue,
            TribalTrophiesDefOf.TribalTrophies_TongueCutting_Tale_TongueCutByEnemy);

        // Bloodlust gives thought, ignoring ideo stance.
        TrophyBodyPartUtility.GiveBloodlustThought(cutter);

        return driedTongue;
    }

    private static BodyPartRecord HeadAbove(BodyPartRecord part)
    {
        for (var above = part.parent; above != null; above = above.parent)
        {
            if (above.def == BodyPartDefOf.Head)
                return above;
        }
        return null;
    }
}
