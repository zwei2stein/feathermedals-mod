using Verse;

namespace TribalTrophies
{
    public class TongueCuttingExtension : DefModExtension
    {
        public float victimValueFactor = 0.1f;

        public float headCutSeverity = 8f;
        public float jawCutSeverity = 2f;

        public int goodwillLossAlive = 20;
        public int goodwillLossDead = 10;

        public int minDurationTicks = 50;
        public int maxDurationTicks = 300;
        public float medicineXp = 100f;
    }

    public class DriedTongue : TrophyBodyPart
    {
        protected override float VictimValueFactor => TongueCuttingUtility.Tuning.victimValueFactor;

        public static DriedTongue Create(Pawn victim, Pawn cutter, EventTime cut)
        {
            var tongue = (DriedTongue)ThingMaker.MakeThing(TribalTrophiesDefOf.TribalTrophies_Trophy_TongueCutting_DriedTongue);
            tongue.Remember(victim, cutter, cut);
            return tongue;
        }

        protected override string LabelOf(string victimName, string factionPart, string xenotypePart)
        {
            return "TribalTrophies_TongueCutting_Item_Label".Translate(
                victimName.Named("VICTIM"),
                factionPart.Named("FACTIONPART"),
                xenotypePart.Named("XENOTYPEPART")).Resolve();
        }

        protected override string TakenByDescription(string takerName)
        {
            return "TribalTrophies_TongueCutting_Item_CutOutBy".Translate(takerName.Named("CUTTER"));
        }

        public override string RecordTitle(string victimName)
        {
            return "TribalTrophies_TongueCutting_TongueTab_TongueOf".Translate(victimName.Named("VICTIM")).Resolve();
        }

        public override string RecordTakenBy(string takerName)
        {
            return "TribalTrophies_TongueCutting_TongueTab_By".Translate(takerName.Named("CUTTER")).Resolve();
        }

        public override string GiftKindLabel => "TribalTrophies_TongueCutting_Letter_GiftResented_Kind".Translate();

        public override string GiftReasonText(ScalpGiftReason reason)
        {
            return reason switch
            {
                ScalpGiftReason.Forbidden => "TribalTrophies_TongueCutting_Letter_GiftResented_Reason_Forbidden".Translate().Resolve(),
                ScalpGiftReason.NoScalpBountyPurchases => "TribalTrophies_TongueCutting_Letter_GiftResented_Reason_NoBuyers".Translate().Resolve(),
                ScalpGiftReason.Victim => "TribalTrophies_TongueCutting_Letter_GiftResented_Reason_Victim".Translate().Resolve(),
                _ => null
            };
        }
    }
}
