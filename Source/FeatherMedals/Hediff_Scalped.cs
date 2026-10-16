using Verse;

namespace FeatherMedals
{
    public class Hediff_Scalped : Hediff_Injury
    {
        private const float FreshDays = 5f;
        private const int FreshTicks = (int)(FreshDays * 60000f);
        
        public Pawn scalper;
        public string scalperName;

        private bool wasFresh = true;

        public bool IsFresh => ageTicks < FreshTicks;

        public string GetScalperName()
        {
            if (scalper != null)
            {
                // get current name (if player renamed pawn...)
                scalperName = scalper.NameFullColored;
            }
            return scalperName;
        }

        public override string TipStringExtra
        {
            get
            {
                string text = base.TipStringExtra;
                if (!scalperName.NullOrEmpty())
                {
                    text += "\n" + "FeatherTrophies_Scalping_ScalpedBy".Translate(GetScalperName().Named("SCALPER"));
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
            Scribe_References.Look(ref scalper, "scalper");
            Scribe_Values.Look(ref scalperName, "scalperName");
        }
    }
}