using Verse;

namespace FeatherMedals
{
    public class Hediff_Scalped : Hediff_Injury
    {
        private const float FreshDays = 5f;
        private const int FreshTicks = (int)(FreshDays * 60000f);
        
        public RememberedPawn scalper;

        private bool wasFresh = true;

        public bool IsFresh => ageTicks < FreshTicks;

        public override string TipStringExtra
        {
            get
            {
                string text = base.TipStringExtra;
                string scalperName = scalper?.Name;
                if (!scalperName.NullOrEmpty())
                {
                    text += "\n" + "FeatherTrophies_Scalping_ScalpedBy".Translate(scalperName.Named("SCALPER"));
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
                comp.IsPermanent = true;
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            bool fresh = IsFresh;
            if (wasFresh && !fresh)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            wasFresh = fresh;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref scalper, "scalper");
        }
    }
}