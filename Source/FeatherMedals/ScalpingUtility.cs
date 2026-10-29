using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies
{
    public static class ScalpingTuning
    {
        // Goodwill lost with the victim's home faction. Dead victim loses half.
        public const int GoodwillLossAlive = 20;
        public const int GoodwillLossDead = GoodwillLossAlive / 2;

    }
    
    public static class ScalpingUtility
    {
        // Severity of the head cut on a living victim (vanilla Cut: bleed = 0.06 x severity).
        public const float CutSeverity = 8f;
        
        // Injuries add up on a part and enough of them destroy it. A scalping must leave the head on: at least this much head health stays.
        private const float MinHeadHealthLeft = 1f;
        
        // smallest injury still worth adding
        private const float MinInjurySeverity = 0.01f;

        public static Pawn PawnOf(Thing thing)
        {
            return thing is Corpse corpse ? corpse.InnerPawn : thing as Pawn;
        }

        public static BodyPartRecord HeadOf(Pawn pawn)
        {
            return pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(x => x.def == BodyPartDefOf.Head);
        }

        public static bool CanBeScalped(Thing thing)
        {
            Pawn pawn = PawnOf(thing);
            
            // Every rule that disqualifies target from being scalped based on physical state.
            // Criteria:
            // * Has head
            // * Is not scalped already
            // * Will not resist (pawns that are hostile and not prisoners: must be downed)
            // * If dead, corpse must be fresh
            // * Colonist must be downed (slaves and prisoners exempt)
            // Examples of pawns not physically eligible:
            // * Hostile that is not downed
            // * Pawn missing head
            // * Pawn with Scalped hediff
            // * Rotten corpse or dessicated corpse.
            if (!Can_PhysicallyCapableOfBeingScalped(pawn, thing))
                return false;

            // Every rule that disqualifies target from being scalped based on it being evil act.
            // It is evil to:
            // * Scalp own people
            // * Scalp children and babies
            // * Scalp non-hostiles (neutral and allied)
            // * Scalp non-guilty prisoners.
            // * Scalp downed neutral/allied pawns
            // Fair game:
            // * No-faction pawns
            // * Hostile pawns
            // * Guilty prisoners
            if (!Can_IsNotEvilToBeScalped(pawn, Faction.OfPlayer))
                return false;

            return true;
            
        }

        // What this scalper's ideology allows. Honorable: evil check applies. Merciless: no evil check. Forbidden: nothing.
        public static bool CanBeScalped(Thing thing, Pawn scalper)
        {
            Pawn pawn = PawnOf(thing);

            if (!Can_PhysicallyCapableOfBeingScalped(pawn, thing))
                return false;

            switch (ScalpingPrecepts.StanceOf(scalper))
            {
                case ScalpStance.Forbidden:
                    return false;
                case ScalpStance.Merciless:
                    return true;
                default:
                    return Can_IsNotEvilToBeScalped(pawn, scalper.Faction ?? Faction.OfPlayer);
            }
        }

        // To be able to show gizmo in mixed ideo colony.
        public static bool CanBeScalpedByAnyone(Thing thing)
        {
            if (CanBeScalped(thing))
                return true;

            return ScalpingPrecepts.AnyMercilessColonist() && Can_PhysicallyCapableOfBeingScalped(PawnOf(thing), thing);
        }

        // Evil from the point of view of the scalper's faction: its own people, its non-hostiles, children.
        private static bool Can_IsNotEvilToBeScalped(Pawn pawn, Faction scalperFaction)
        {
            if (!pawn.DevelopmentalStage.Adult())
                return false;

            // Imprisoned guilty colonist is still of player faction, we must check guilt first.
            if (pawn.IsPrisoner && pawn.HostFaction == scalperFaction && pawn.guilt.IsGuilty)
                return true;

            if (pawn.Faction == scalperFaction)
                return false;
    
            Faction home = pawn.HomeFaction;
            if (home == null)
                return true;

            if (home.HostileTo(scalperFaction) || pawn.HostileTo(scalperFaction))
                return true;

            return false;
        }

        private static bool Can_PhysicallyCapableOfBeingScalped(Pawn pawn, Thing thing)
        {
            if (pawn == null || !thing.Spawned)
                return false;
            
            if (!HasScalpToTake(pawn))
                return false;
            
            if (thing is Corpse corpse)
                return corpse.GetRotStage() == RotStage.Fresh;
            
            if (pawn.Dead)
                return false;
            
            if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                return true;

            if (pawn.Faction == Faction.OfPlayer)
                return pawn.Downed;
            
            var home = pawn.HomeFaction;
            if ((home != null && home.HostileTo(Faction.OfPlayer)) || pawn.HostileTo(Faction.OfPlayer))
                return  pawn.Downed;

            return true;

        }

        // Scalping a living non-hostile pawn angers their faction. Same condition vanilla capture uses.
        public static bool WouldAngerFaction(Thing target)
        {
            if (!(target is Pawn pawn))
                return false;

            var faction = pawn.Faction;
            return faction != null && faction != Faction.OfPlayer && !faction.Hidden
                   && !faction.HostileTo(Faction.OfPlayer) && !pawn.IsPrisonerOfColony;
        }

        public static void Provoke(Pawn target)
        {
            // Trying to scalp turns faction hostile.
            var faction = target.Faction;
            var change = Faction.OfPlayer.GoodwillToMakeHostile(faction);
            if (change != 0)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(faction, change, false, true,
                    TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_ScalpedMember, (GlobalTargetInfo)target);
            }

            Messages.Message(
                "TribalTrophies_Scalping_Message_Provoked".Translate(
                    Faction.OfPlayer.Named("PLAYERFACTION"), target.Named("TARGET"), faction.Named("FACTION")).CapitalizeFirst(),
                target, MessageTypeDefOf.NegativeEvent);
        }

        // Scalping in a mental break, ideo and other rules.
        // Criteria:
        // * Humanlike with head, not scalped already
        // * Children and babies only if enabled in settings
        // * If dead, corpse must be fresh
        // * No other (like must be downed) rules
        public static bool CanBeTargetedInBreak(Thing thing)
        {
            Pawn pawn = PawnOf(thing);

            if (pawn == null || !thing.Spawned)
                return false;

            if (!HasScalpToTake(pawn))
                return false;

            if (!pawn.DevelopmentalStage.Adult() && !TribalTrophiesMod.Settings.ScalpBreaksTargetChildren)
                return false;

            if (thing is Corpse corpse)
                return corpse.GetRotStage() == RotStage.Fresh;

            return !pawn.Dead;
        }

        // Same as above, in middle of break after running down and downing pawn
        public static bool CanBeScalpedInBreak(Thing thing)
        {
            if (!CanBeTargetedInBreak(thing))
                return false;

            Pawn pawn = PawnOf(thing);
            if (pawn.Dead || pawn.IsPrisoner || pawn.IsSlave)
                return true;

            return pawn.Downed;
        }

        public static Scalp DoScalp(Pawn scalper, Thing target, bool inBreak = false)
        {
            var victim = PawnOf(target);
            if (victim == null || !(inBreak ? CanBeScalpedInBreak(target) : CanBeScalped(target, scalper)))
            {
                return null;
            }

            var alive = !victim.Dead;
            var head = HeadOf(victim);
            var faction = victim.HomeFaction;
            var map = target.MapHeld;
            var pos = target.PositionHeld;

            // Before hediffs: scalp remembers victim as they were (value, skin, hair).
            var scalp = Scalp.Create(victim, scalper, EventTime.Now(map));

            // how much more damage the head can take before it is destroyed
            var headDamageRoom = victim.health.hediffSet.GetPartHealth(head) - MinHeadHealthLeft;

            var scalped = (Hediff_Scalped)HediffMaker.MakeHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped, victim, head);
            scalped.scalper = new RememberedPawn(scalper);
            scalped.Severity = Mathf.Clamp(scalped.Severity, MinInjurySeverity, Mathf.Max(MinInjurySeverity, headDamageRoom));
            victim.health.AddHediff(scalped, head);
            headDamageRoom -= scalped.Severity;

            if (alive)
            {
                // the cut takes only what the head has left; a battered head bleeds less, but stays on
                if (headDamageRoom >= MinInjurySeverity)
                {
                    var cut = HediffMaker.MakeHediff(HediffDefOf.Cut, victim, head);
                    cut.Severity = Mathf.Min(CutSeverity, headDamageRoom);
                    victim.health.AddHediff(cut, head);
                }

                victim.needs?.mood?.thoughts.memories.TryGainMemory(
                    TribalTrophiesDefOf.TribalTrophies_Scalping_ThoughtSocial_ScalpedMe, scalper);
                victim.needs?.mood?.thoughts.memories.TryGainMemory(
                    TribalTrophiesDefOf.TribalTrophies_Scalping_Thought_IWasScalped);
            }

            var blood = victim.RaceProps.BloodDef;
            if (blood != null && map != null)
            {
                FilthMaker.TryMakeFilth(pos, map, blood, victim.LabelIndefinite());
            }

            if (scalper.Map == null || !GenPlace.TryPlaceThing(scalp, scalper.Position, scalper.Map, ThingPlaceMode.Near))
            {
                scalp.Destroy();
                scalp = null;
            }

            // only the player's own scalping changes goodwill with the player
            if (faction != null && faction != Faction.OfPlayer && scalper.Faction == Faction.OfPlayer)
            {
                var loss = alive ? ScalpingTuning.GoodwillLossAlive : ScalpingTuning.GoodwillLossDead;
                faction.TryAffectGoodwillWith(Faction.OfPlayer, -loss, true, true,
                    TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_ScalpedMember, (GlobalTargetInfo)target);
            }

            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TribalTrophiesDefOf.TribalTrophies_Scalping_HistoryEvent_TookScalp,
                scalper.Named(HistoryEventArgsNames.Doer), victim.Named(HistoryEventArgsNames.Victim)));

            // messages are taking sides - heroic colonist, villain raider.
            var tale = scalper.Faction == Faction.OfPlayer
                ? TribalTrophiesDefOf.TribalTrophies_Scalping_Tale_TookScalp
                : TribalTrophiesDefOf.TribalTrophies_Scalping_Tale_ScalpedByEnemy;
            TaleRecorder.RecordTale(tale, scalper, victim);
            
            // Bloodlust gives thought, ignoring ideo stance.
            if (scalper.story?.traits?.HasTrait(TraitDefOf.Bloodlust) == true && ScalpingPrecepts.StanceOf(scalper) != ScalpStance.Merciless)
            {
                scalper.needs?.mood?.thoughts.memories.TryGainMemory(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Thought_Merciless_ClaimedRightOfVictor);
            }

            return scalp;
        }
        
        // Body can still give a scalp: humanlike, head present, not scalped yet.
        public static bool HasScalpToTake(Pawn pawn)
        {
            return pawn.RaceProps.Humanlike
                   && !pawn.health.hediffSet.HasHediff(TribalTrophiesDefOf.TribalTrophies_Scalping_Hediff_Scalped)
                   && HeadOf(pawn) != null;
        }
        
    }
}