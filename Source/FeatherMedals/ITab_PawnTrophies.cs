using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FeatherMedals
{
    [StaticConstructorOnStartup]
    public static class InjectTrophyTab
    {
        static InjectTrophyTab()
        {
            var tabType = typeof(ITab_PawnTrophies);
            var tabInstance = InspectTabManager.GetSharedInstance(tabType);

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race is not { Humanlike: true })
                    continue;

                InjectIntoDef(def, tabType, tabInstance);
                InjectIntoDef(def.race.corpseDef, tabType, tabInstance);
                InjectIntoDef(def.race.unnaturalCorpseDef, tabType, tabInstance);
            }
            
        }

        private static void InjectIntoDef(ThingDef def, Type tabType, InspectTabBase tabInstance)
        {
            if (def == null)
                return;
            
            def.inspectorTabs ??= new List<Type>();
            def.inspectorTabsResolved ??= new List<InspectTabBase>();

            if (def.inspectorTabs.Contains(tabType))
                return;
            
            def.inspectorTabs.Add(tabType);
            
            if (!def.inspectorTabsResolved.Contains(tabInstance))
                def.inspectorTabsResolved.Add(tabInstance);
            
            if (Prefs.DevMode)
                Log.Message($"[TribalTrophies] Trophies tab added to {def.defName}.");
            
        }
    }
    
    public class ITab_PawnTrophies : ITab_TrophyRows
    {
        public ITab_PawnTrophies() : base("TribalTrophies_Trophy_PawnTab_Label")
        {
        }

        private Pawn SelPawnForGear
        {
            get
            {
                return SelThing switch
                {
                    Pawn p => p,
                    Corpse corpse => corpse.InnerPawn,
                    _ => null
                };
            }
        }

        // feathers first, then scalps; an empty belt holds no trophies
        protected override List<ITrophyRecordRow> Rows()
        {
            var rows = new List<ITrophyRecordRow>();
            var pawn = SelPawnForGear;
            if (pawn?.apparel == null)
                return rows;

            foreach (var featherTrophy in pawn.apparel.WornApparel.OfType<FeatherTrophy>())
                rows.Add(new FeatherTrophyRecordDrawer(featherTrophy));
            foreach (var scalp in WornScalps(pawn))
                rows.Add(new ScalpRecordDrawer(scalp));
            return rows;
        }

        protected override string Header(int rowCount) => rowCount == 1
            ? "TribalTrophies_Trophy_PawnTab_Header_One".Translate()
            : "TribalTrophies_Trophy_PawnTab_Header_Many".Translate();

        private static IEnumerable<Scalp> WornScalps(Pawn pawn)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var container = apparel.TryGetComp<CompTrophyContainer>();
                if (container == null)
                    continue;
                foreach (var scalp in container.Held.OfType<Scalp>())
                    yield return scalp;
            }
        }
    }
}