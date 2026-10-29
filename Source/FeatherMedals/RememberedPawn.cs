using RimWorld;
using Verse;

namespace TribalTrophies
{
    // Pawn reference plus last known name. Name survives pawn being purged; follows renames while pawn exists.
    public class RememberedPawn : IExposable
    {
        private Pawn pawn;
        private string name;

        private Faction faction;
        private string factionName;
        
        public RememberedPawn()
        {
        }

        public RememberedPawn(string defaultName = null)
        {
            name = defaultName;
        }

        public RememberedPawn(Pawn pawn)
        {
            this.pawn = pawn;
            Refresh();
            if (pawn != null)
            {
                faction = pawn.HomeFaction ?? pawn.Faction;
                factionName = faction?.Name;
            }
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
        
        public Faction Faction => faction;

        public string FactionName
        {
            get
            {
                Refresh();
                return factionName;
            }
        }

        public string FactionNameColored
        {
            get
            {
                var plain = FactionName;
                if (plain.NullOrEmpty() || faction == null)
                    return plain;
                return plain.Colorize(faction.Color);
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
            // refresh last known name when saving
            if (Scribe.mode == LoadSaveMode.Saving)
                Refresh();

            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref name, "name");
            
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref factionName, "factionName");
        }

        public void DiscardPawn()
        {
            pawn = null;
        }
    }
}