using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Rules and consequences shared by every way of taking a body part as a trophy (scalping, ...).
public static class TrophyBodyPartUtility
{
    // Injuries add up on a part and enough of them destroy it. Taking a trophy must leave the part on: at least this much health stays.
    private const float MinPartHealthLeft = 1f;

    // smallest injury still worth adding
    private const float MinInjurySeverity = 0.01f;

    public static Pawn PawnOf(Thing thing)
    {
        return thing is Corpse corpse ? corpse.InnerPawn : thing as Pawn;
    }

    public static bool HasTrophyBody(Pawn pawn)
    {
        return pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh;
    }

    // Evil from the point of view of the taker's faction: its own people, its non-hostiles, children.
    public static bool IsNotEvilToTakeFrom(Pawn pawn, Faction takerFaction)
    {
        if (!pawn.DevelopmentalStage.Adult())
            return false;

        // Imprisoned guilty colonist is still of player faction, we must check guilt first.
        if (pawn.IsPrisoner && pawn.HostFaction == takerFaction && pawn.guilt.IsGuilty)
            return true;

        if (pawn.Faction == takerFaction)
            return false;

        Faction home = pawn.HomeFaction;
        if (home == null)
            return true;

        if (IsHostileTo(pawn, takerFaction))
            return true;

        return false;
    }

    // Body is held still: fresh corpse, captive, or downed if it would resist.
    public static bool IsHeldStill(Pawn pawn, Thing thing)
    {
        if (thing is Corpse corpse)
            return corpse.GetRotStage() == RotStage.Fresh;

        if (pawn.Dead)
            return false;

        if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            return true;

        if (pawn.Faction == Faction.OfPlayer)
            return pawn.Downed;

        if (IsHostileTo(pawn, Faction.OfPlayer))
            return pawn.Downed;

        return true;
    }

    // Taking from a living non-hostile pawn angers their faction. Same condition vanilla capture uses.
    public static bool WouldAngerFaction(Thing target)
    {
        if (!(target is Pawn pawn))
            return false;

        var faction = pawn.Faction;
        return faction != null && faction != Faction.OfPlayer && !faction.Hidden
               && !faction.HostileTo(Faction.OfPlayer) && !pawn.IsPrisonerOfColony;
    }

    // Trying to take from a non-hostile turns its faction hostile.
    public static void MakeFactionHostile(Pawn target, HistoryEventDef reason)
    {
        var faction = target.Faction;
        var change = Faction.OfPlayer.GoodwillToMakeHostile(faction);
        if (change != 0)
        {
            Faction.OfPlayer.TryAffectGoodwillWith(faction, change, false, true, reason, (GlobalTargetInfo)target);
        }
    }

    // Only the player's own taking changes goodwill with the player.
    public static void LoseGoodwill(Faction victimFaction, Pawn taker, int loss, HistoryEventDef reason, Thing target)
    {
        if (victimFaction == null || victimFaction == Faction.OfPlayer || taker.Faction != Faction.OfPlayer)
            return;

        victimFaction.TryAffectGoodwillWith(Faction.OfPlayer, -loss, true, true, reason, (GlobalTargetInfo)target);
    }

    // Injury that never destroys its part: takes only what the part has left, at least MinPartHealthLeft stays.
    public static void AddInjuryLeavingPart(Pawn victim, BodyPartRecord part, HediffDef injury, float severity)
    {
        var room = victim.health.hediffSet.GetPartHealth(part) - MinPartHealthLeft;
        if (room < MinInjurySeverity)
            return;

        var hediff = HediffMaker.MakeHediff(injury, victim, part);
        hediff.Severity = Mathf.Min(severity, room);
        victim.health.AddHediff(hediff, part);
    }

    public static void MakeBlood(Pawn victim, IntVec3 pos, Map map)
    {
        var blood = victim.RaceProps.BloodDef;
        if (blood != null && map != null)
        {
            FilthMaker.TryMakeFilth(pos, map, blood, victim.LabelIndefinite());
        }
    }

    // Trophy goes next to the taker; null when there is no place for it.
    public static T PlaceNear<T>(T trophy, Pawn taker) where T : Thing
    {
        if (taker.Map == null || !GenPlace.TryPlaceThing(trophy, taker.Position, taker.Map, ThingPlaceMode.Near))
        {
            trophy.Destroy();
            return null;
        }
        return trophy;
    }

    // History event for ideology, tale from the player's side: heroic colonist, villain outsider.
    public static void RecordTaking(Pawn taker, Pawn victim, HistoryEventDef tookEvent, TaleDef colonyTale, TaleDef enemyTale)
    {
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(tookEvent,
            taker.Named(HistoryEventArgsNames.Doer), victim.Named(HistoryEventArgsNames.Victim)));

        var tale = taker.Faction == Faction.OfPlayer ? colonyTale : enemyTale;
        TaleRecorder.RecordTale(tale, taker, victim);
    }

    // Bloodlust gives thought, ignoring ideo stance (merciless stance gives it through its precept).
    public static void GiveBloodlustThought(Pawn taker)
    {
        if (taker.story?.traits?.HasTrait(TraitDefOf.Bloodlust) == true && ScalpingPrecepts.StanceOf(taker) != ScalpStance.Merciless)
        {
            taker.needs?.mood?.thoughts.memories.TryGainMemory(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Thought_Merciless_ClaimedRightOfVictor);
        }
    }

    // Hostile itself, or its home faction is (prisoners keep their home faction).
    private static bool IsHostileTo(Pawn pawn, Faction faction)
    {
        var home = pawn.HomeFaction;
        return (home != null && home.HostileTo(faction)) || pawn.HostileTo(faction);
    }
}