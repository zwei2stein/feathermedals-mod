using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TribalTrophies
{
    public enum ScalpGiftReason
    {
        Forbidden,      // primary ideo forbids taking body part trophies
        NoScalpBountyPurchases,  // no trader of the faction would buy this kind of trophy
        Victim          // trophy was taken from one of their members
    }

    // One kind of trophy (scalps, ...) the faction objected to, and why.
    public class ScalpGiftObjection
    {
        public TrophyBodyPart Trophy; // any one of the kind, words its part of the letter
        public readonly HashSet<ScalpGiftReason> Reasons = new HashSet<ScalpGiftReason>();
    }

    public class ScalpGiftResult
    {
        public int Loss;
        public readonly List<ScalpGiftObjection> Objections = new List<ScalpGiftObjection>();
    }

    // Body part trophies sent as a gift (drop pods, caravan) to a faction that does not want them anger it, 1 goodwill per trophy per reason.
    public static class ScalpGiftUtility
    {
        public static List<ScalpGiftReason> ReasonsFor(TrophyBodyPart trophy, Faction faction)
        {
            var reasons = new List<ScalpGiftReason>();

            if (trophy == null || faction == null || faction.IsPlayer || !faction.HasGoodwill)
                return reasons;

            var ideo = faction.ideos?.PrimaryIdeo;
            if (ideo != null && ideo.HasPrecept(TribalTrophiesDefOf.TribalTrophies_Issue_ScalpTaking_Forbidden))
                reasons.Add(ScalpGiftReason.Forbidden);

            if (!AnyTraderBuys(faction, trophy.def))
                reasons.Add(ScalpGiftReason.NoScalpBountyPurchases);

            // Trophies of pawns that were purged from save are considered forgotten by faction
            var victim = trophy.Victim?.Pawn;
            if (victim != null && (victim.HomeFaction ?? victim.Faction) == faction)
                reasons.Add(ScalpGiftReason.Victim);

            return reasons;
        }

        private static bool AnyTraderBuys(Faction faction, ThingDef trophyDef)
        {
            var def = faction.def;
            return AnyBuys(def.baseTraderKinds, trophyDef)
                   || AnyBuys(def.caravanTraderKinds, trophyDef)
                   || AnyBuys(def.visitorTraderKinds, trophyDef)
                   || AnyBuys(def.orbitalTraderKinds, trophyDef);
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

        // Call before the gift is handed over, trophies are gone afterwards.
        public static ScalpGiftResult Evaluate(IEnumerable<TrophyBodyPart> trophies, Faction faction)
        {
            var result = new ScalpGiftResult();
            foreach (TrophyBodyPart trophy in trophies)
            {
                List<ScalpGiftReason> reasons = ReasonsFor(trophy, faction);
                if (reasons.Count == 0)
                    continue;

                result.Loss += reasons.Count;
                var objection = ObjectionTo(result, trophy);
                foreach (ScalpGiftReason reason in reasons)
                    objection.Reasons.Add(reason);
            }
            return result;
        }

        // one objection per kind of trophy
        private static ScalpGiftObjection ObjectionTo(ScalpGiftResult result, TrophyBodyPart trophy)
        {
            foreach (var objection in result.Objections)
            {
                if (objection.Trophy.def == trophy.def)
                    return objection;
            }

            var added = new ScalpGiftObjection { Trophy = trophy };
            result.Objections.Add(added);
            return added;
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

            // kinds always in the same order, whatever order they were packed in
            var objections = result.Objections.OrderBy(objection => objection.Trophy.def.index).ToList();

            // "scalps", "scalps and ..."
            var items = objections.Select(objection => objection.Trophy.GiftKindLabel).ToCommaList(useAnd: true);

            var reasonLines = new List<string>();
            foreach (var objection in objections)
            {
                if (objection.Reasons.Contains(ScalpGiftReason.Forbidden))
                    reasonLines.Add(objection.Trophy.GiftReasonText(ScalpGiftReason.Forbidden));
                if (objection.Reasons.Contains(ScalpGiftReason.NoScalpBountyPurchases))
                    reasonLines.Add(objection.Trophy.GiftReasonText(ScalpGiftReason.NoScalpBountyPurchases));
                if (objection.Reasons.Contains(ScalpGiftReason.Victim))
                    reasonLines.Add(objection.Trophy.GiftReasonText(ScalpGiftReason.Victim));
            }

            Find.LetterStack.ReceiveLetter(
                "TribalTrophies_TrophyBodyPart_Letter_GiftResented_Label".Translate(items.CapitalizeFirst().Named("ITEMS")),
                "TribalTrophies_TrophyBodyPart_Letter_GiftResented".Translate(
                    faction.NameColored.Named("FACTION"),
                    items.Named("ITEMS"),
                    (-lost).ToStringWithSign().Named("LOSS"),
                    string.Join("\n", reasonLines).Named("REASONS")),
                LetterDefOf.NegativeEvent,
                lookTarget,
                faction);
        }
    }
}
