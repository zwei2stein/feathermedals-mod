using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalTrophies
{
    // A body part taken from a pawn as a trophy (scalp, ...). Remembers whose it was, who took it and when and where.
    public abstract class TrophyBodyPart : ThingWithComps
    {
        private RememberedPawn victim = new("TribalTrophies_TrophyBodyPart_Misc_UnknownName".Translate());
        private RememberedPawn taker = new("TribalTrophies_TrophyBodyPart_Misc_UnknownName".Translate());

        private string xenotypeName;

        private EventTime taken; // null = unknown, e.g. spawned by dev tools

        private float priceBonus;

        public RememberedPawn Victim => victim;
        public RememberedPawn Taker => taker;
        public EventTime Taken => taken;
        public string XenotypeName => xenotypeName;
        public float PriceBonus => priceBonus;

        protected abstract float VictimValueFactor { get; }

        protected abstract string LabelOf(string victimName, string factionPart, string xenotypePart);

        protected abstract string TakenByDescription(string takerName);

        public abstract string RecordTitle(string victimName);
        public abstract string RecordTakenBy(string takerName);

        public abstract string GiftKindLabel { get; }
        public abstract string GiftReasonText(ScalpGiftReason reason);

        protected void Remember(Pawn victimPawn, Pawn takerPawn, EventTime when)
        {
            victim = new RememberedPawn(victimPawn);
            taker = new RememberedPawn(takerPawn);

            if (victimPawn.genes != null && (victimPawn.genes.Xenotype != XenotypeDefOf.Baseliner || victimPawn.genes.CustomXenotype != null))
                xenotypeName = victimPawn.genes.XenotypeLabel;

            taken = when;

            priceBonus = victimPawn.MarketValue * VictimValueFactor;
        }

        private string TrophyOf
        {
            get
            {
                if (victim == null || victim.Name.NullOrEmpty())
                {
                    return null;
                }

                var factionPart = victim.FactionName.NullOrEmpty()
                    ? ""
                    : "TribalTrophies_TrophyBodyPart_Item_Label_FactionPart".Translate(victim.FactionName.Named("FACTION")).Resolve();

                var xenotypePart = xenotypeName.NullOrEmpty()
                    ? ""
                    : "TribalTrophies_TrophyBodyPart_Item_Label_XenotypePart".Translate(xenotypeName.CapitalizeFirst().Named("XENOTYPE")).Resolve();

                return LabelOf(victim.Name, factionPart, xenotypePart);
            }
        }

        public override string LabelNoCount => TrophyOf ?? base.LabelNoCount;

        public override string DescriptionFlavor
        {
            get
            {
                var descriptions = new List<string>();

                descriptions.Add(TrophyOf?.CapitalizeFirst());
                descriptions.Add(base.DescriptionFlavor);

                var takerName = taker?.Name;
                if (!takerName.NullOrEmpty())
                    descriptions.Add(TakenByDescription(takerName));

                return string.Join("\n\n", descriptions.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public override string DescriptionDetailed => DescriptionFlavor;

        // every trophy is somebody's, never stacks
        public override bool CanStackWith(Thing other)
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref victim, "victim");
            Scribe_Deep.Look(ref taker, "scalper");
            Scribe_Values.Look(ref xenotypeName, "xenotypeName");
            Scribe_Deep.Look(ref taken, "scalped");
            Scribe_Values.Look(ref priceBonus, "priceBonus");
        }
    }

    public class CompProperties_ScalpPrice : CompProperties
    {
        public CompProperties_ScalpPrice()
        {
            compClass = typeof(CompScalpPrice);
        }
    }

    public class CompScalpPrice : ThingComp
    {
        public override float GetStatOffset(StatDef stat)
        {
            if (stat == StatDefOf.MarketValue && parent is TrophyBodyPart trophy)
            {
                return trophy.PriceBonus;
            }
            return 0f;
        }
    }
}
