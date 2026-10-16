using Verse;

namespace FeatherMedals
{
    // Pawn reference plus last known name. Name survives pawn being purged; follows renames while pawn exists.
    public class RememberedPawn : IExposable
    {
        private Pawn pawn;
        private string name;

        public RememberedPawn(string defaultName = null)
        {
            name = defaultName;
        }

        public RememberedPawn(Pawn pawn)
        {
            this.pawn = pawn;
            Refresh();
        }

        public Pawn Pawn => pawn;

        public string Name
        {
            get
            {
                Refresh();
                return name;
            }
        }

        private void Refresh()
        {
            if (pawn != null)
            {
                // get current name (if player renamed pawn...)
                name = pawn.NameFullColored;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref name, "name");
        }
    }
}