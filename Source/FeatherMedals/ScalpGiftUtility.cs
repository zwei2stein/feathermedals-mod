using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FeatherMedals
{
    public enum ScalpGiftReason
    {
        Forbidden,      // primary ideo forbids scalping
        NoScalpBountyPurchases,  // no trader of the faction would buy scalps
        Victim          // scalp was taken from one of their members
    }

    public class ScalpGiftResult
    {
        public int Loss;
        public readonly HashSet<ScalpGiftReason> Reasons = new HashSet<ScalpGiftReason>();
    }

    // Scalps sent as a gift (drop pods, caravan) to a faction that does not want them anger it, 1 goodwill per scalp per reason.
    public static class ScalpGiftUtility
    {
        public static List<ScalpGiftReason> ReasonsFor(Scalp scalp, Faction faction)
        {
            var reasons = new List<ScalpGiftReason>();

            if (scalp == null || faction == null || faction.IsPlayer || !faction.HasGoodwill)
                return reasons;

            var ideo = faction.ideos?.PrimaryIdeo;
            if (ideo != null && ideo.HasPrecept(FeatherMedalDefOf.FeatherTrophies_ScalpTaking_Forbidden))
                reasons.Add(ScalpGiftReason.Forbidden);

            if (!AnyTraderBuysScalps(faction))
                reasons.Add(ScalpGiftReason.NoScalpBountyPurchases);

            // Scalps of pawns that were purged from save are considered forgotten by faction
            var victim = scalp.Victim?.Pawn;
            if (victim != null && (victim.HomeFaction ?? victim.Faction) == faction)
                reasons.Add(ScalpGiftReason.Victim);

            return reasons;
        }

        private static bool AnyTraderBuysScalps(Faction faction)
        {
            var scalpDef = FeatherMedalDefOf.TribalTrophies_Trophy_Scalping_Scalp;
            var def = faction.def;
            return AnyBuys(def.baseTraderKinds, scalpDef)
                   || AnyBuys(def.caravanTraderKinds, scalpDef)
                   || AnyBuys(def.visitorTraderKinds, scalpDef)
                   || AnyBuys(def.orbitalTraderKinds, scalpDef);
        }

        private static bool AnyBuys(List<TraderKindDef> kinds, ThingDef thingDef)
        {
            if (kinds == null)
                return false;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i].WillTrade(thingDef))
                    return true;
            }
            return false;
        }

        // Call before the gift is handed over, scalps are gone afterwards.
        public static ScalpGiftResult Evaluate(IEnumerable<Scalp> scalps, Faction faction)
        {
            var result = new ScalpGiftResult();
            foreach (Scalp scalp in scalps)
            {
                List<ScalpGiftReason> reasons = ReasonsFor(scalp, faction);
                result.Loss += reasons.Count;
                foreach (ScalpGiftReason reason in reasons)
                    result.Reasons.Add(reason);
            }
            return result;
        }

        public static void Apply(ScalpGiftResult result, Faction faction, GlobalTargetInfo lookTarget)
        {
            if (result == null || result.Loss <= 0 || faction == null)
                return;

            int before = faction.PlayerGoodwill;
            Faction.OfPlayer.TryAffectGoodwillWith(faction, -result.Loss, canSendMessage: false, lookTarget: lookTarget);
            int lost = before - faction.PlayerGoodwill;
            if (lost <= 0)
                return;

            var reasonLines = new List<string>();
            if (result.Reasons.Contains(ScalpGiftReason.Forbidden))
                reasonLines.Add("TribalTrophies_Scalping_Letter_GiftResented_Reason_Forbidden".Translate().Resolve());
            if (result.Reasons.Contains(ScalpGiftReason.NoScalpBountyPurchases))
                reasonLines.Add("TribalTrophies_Scalping_Letter_GiftResented_Reason_NoBounties".Translate().Resolve());
            if (result.Reasons.Contains(ScalpGiftReason.Victim))
                reasonLines.Add("TribalTrophies_Scalping_Letter_GiftResented_Reason_Victim".Translate().Resolve());

            Find.LetterStack.ReceiveLetter(
                "TribalTrophies_Scalping_Letter_GiftResented_Label".Translate(),
                "TribalTrophies_Scalping_Letter_GiftResented".Translate(
                    faction.NameColored.Named("FACTION"),
                    (-lost).ToStringWithSign().Named("LOSS"),
                    string.Join("\n", reasonLines).Named("REASONS")),
                LetterDefOf.NegativeEvent,
                lookTarget,
                faction);
        }
    }
}