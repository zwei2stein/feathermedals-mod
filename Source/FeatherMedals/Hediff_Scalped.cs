using Verse;

namespace FeatherMedals
{
    public class Hediff_Scalped : Hediff_Injury
    {
        // Severity to become permanent scar.
        private const float ScarSeverity = 0.1f;

        private const float ChancePainless = 0.10f;
        private const float ChanceLowPain = 0.40f;
        private const float ChanceMediumPain = 0.35f;

        public RememberedPawn scalper;

        private bool wasFresh = true;

        // Fresh until scaring
        public bool IsFresh => !this.IsPermanent();

        public override string TipStringExtra
        {
            get
            {
                string text = base.TipStringExtra;
                string scalperName = scalper?.Name;
                if (!scalperName.NullOrEmpty())
                {
                    text += "\n" + "TribalTrophies_Scalping_Hediff_ScalpedBy".Translate(scalperName.Named("SCALPER"));
                }
                return text;
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            HediffComp_GetsPermanent comp = this.TryGetComp<HediffComp_GetsPermanent>();
            if (comp != null)
            {
                comp.permanentDamageThreshold = ScarSeverity;
            }
        }
        
        public void BecomeScar()
        {
            HediffComp_GetsPermanent comp = this.TryGetComp<HediffComp_GetsPermanent>();
            if (comp != null)
            {
                comp.IsPermanent = true;
                comp.SetPainCategory(RollPainCategory());
            }
            Severity = ScarSeverity;
            wasFresh = false;
        }

        private static PainCategory RollPainCategory()
        {
            float roll = Rand.Value;

            if (roll < ChancePainless)
                return PainCategory.Painless;
            roll -= ChancePainless;

            if (roll < ChanceLowPain)
                return PainCategory.LowPain;
            roll -= ChanceLowPain;

            if (roll < ChanceMediumPain)
                return PainCategory.MediumPain;

            return PainCategory.HighPain;
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            bool fresh = IsFresh;
            if (wasFresh && !fresh)
            {
                // We want extra pain, not vanilla
                this.TryGetComp<HediffComp_GetsPermanent>()?.SetPainCategory(RollPainCategory());
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            wasFresh = fresh;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref scalper, "scalper");
            Scribe_Values.Look(ref wasFresh, "wasFresh", true);
        }
    }
}