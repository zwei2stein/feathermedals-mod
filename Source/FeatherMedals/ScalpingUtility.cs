using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals
{
    public static class ScalpingTuning
    {
        // Ticks the scalping job takes.
        public const int DurationTicks = 300;

        // Severity of the head cut on a living victim (vanilla Cut: bleed = 0.06 x severity).
        public const float CutSeverity = 8f;

        // Goodwill lost with the victim's home faction. Dead victim loses half.
        public const int GoodwillLossAlive = 20;
        public const int GoodwillLossDead = GoodwillLossAlive / 2;
    }

    
    public static class ScalpingUtility
    {

        private static Pawn PawnOf(Thing thing)
        {
            return thing is Corpse corpse ? corpse.InnerPawn : thing as Pawn;
        }

        private static BodyPartRecord HeadOf(Pawn pawn)
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
            if (!Can_IsNotEvilToBeScalped(pawn))
                return false;

            return true;
            
        }

        // What this scalper's ideology allows. Honourable: evil check applies. Merciless: no evil check. Forbidden: nothing.
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
                    return Can_IsNotEvilToBeScalped(pawn);
            }
        }

        // To be able to show gizmo in mixed ideo colony.
        public static bool CanBeScalpedByAnyone(Thing thing)
        {
            if (CanBeScalped(thing))
                return true;

            return ScalpingPrecepts.AnyMercilessColonist() && Can_PhysicallyCapableOfBeingScalped(PawnOf(thing), thing);
        }

        private static bool Can_IsNotEvilToBeScalped(Pawn pawn)
        {
            if (!pawn.DevelopmentalStage.Adult())
                return false;

            if (pawn.Faction == Faction.OfPlayer)
                return false;

            Faction home = pawn.HomeFaction;
            if (home == null)
                return true;

            if (home.HostileTo(Faction.OfPlayer) || pawn.HostileTo(Faction.OfPlayer))
                return true;

            return pawn.IsPrisonerOfColony && pawn.guilt.IsGuilty;
        }

        private static bool Can_PhysicallyCapableOfBeingScalped(Pawn pawn, Thing thing)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike || !thing.Spawned)
                return false;
            
            if (pawn.health.hediffSet.HasHediff(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalped) || HeadOf(pawn) == null)
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
                    FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpedMember, (GlobalTargetInfo)target);
            }

            Messages.Message(
                "FeatherTrophies_Scalping_Provoked".Translate(
                    Faction.OfPlayer.Named("PLAYERFACTION"), target.Named("TARGET"), faction.Named("FACTION")).CapitalizeFirst(),
                target, MessageTypeDefOf.NegativeEvent);
        }

        public static void DoScalp(Pawn scalper, Thing target)
        {
            var victim = PawnOf(target);
            if (victim == null || !CanBeScalped(target, scalper))
            {
                return;
            }

            var alive = !victim.Dead;
            var head = HeadOf(victim);
            var faction = victim.HomeFaction;
            var map = target.MapHeld;
            var pos = target.PositionHeld;

            var scalped = (Hediff_Scalped)HediffMaker.MakeHediff(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalped, victim, head);
            scalped.scalper = scalper;
            // updates name label:
            scalped.GetScalperName();
            victim.health.AddHediff(scalped, head);

            if (alive)
            {
                var cut = HediffMaker.MakeHediff(HediffDefOf.Cut, victim, head);
                cut.Severity = ScalpingTuning.CutSeverity;
                victim.health.AddHediff(cut, head);

                victim.needs?.mood?.thoughts.memories.TryGainMemory(
                    FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpedMe, scalper);
                victim.needs?.mood?.thoughts.memories.TryGainMemory(
                    FeatherMedalDefOf.FeatherTrophies_Scalping_WasScalped);
            }

            var blood = victim.RaceProps.BloodDef;
            if (blood != null && map != null)
            {
                FilthMaker.TryMakeFilth(pos, map, blood, victim.LabelIndefinite());
            }

            // TODO Placeholder scalp item until the real one exists.
            var leather = victim.RaceProps.leatherDef;
            if (leather != null && scalper.Map != null)
            {
                var item = ThingMaker.MakeThing(leather);
                item.stackCount = 1;
                GenPlace.TryPlaceThing(item, scalper.Position, scalper.Map, ThingPlaceMode.Near);
            }

            if (faction != null && faction != Faction.OfPlayer)
            {
                var loss = alive ? ScalpingTuning.GoodwillLossAlive : ScalpingTuning.GoodwillLossDead;
                faction.TryAffectGoodwillWith(Faction.OfPlayer, -loss, true, true,
                    FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpedMember, (GlobalTargetInfo)target);
            }

            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(FeatherMedalDefOf.FeatherTrophies_Scalping_TookScalp,
                scalper.Named(HistoryEventArgsNames.Doer), victim.Named(HistoryEventArgsNames.Victim)));

            // Bloodlust gives thought, ignoring ideo stance.
            if (scalper.story?.traits?.HasTrait(TraitDefOf.Bloodlust) == true && ScalpingPrecepts.StanceOf(scalper) != ScalpStance.Merciless)
            {
                scalper.needs?.mood?.thoughts.memories.TryGainMemory(FeatherMedalDefOf.FeatherTrophies_Scalping_ClaimedRightOfVictor);
            }
        }
    }
}