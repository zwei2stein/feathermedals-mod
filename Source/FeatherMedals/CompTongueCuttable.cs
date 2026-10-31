using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies
{
    public class CompProperties_TongueCuttable : CompProperties
    {
        public CompProperties_TongueCuttable()
        {
            compClass = typeof(CompTongueCuttable);
        }
    }

    [StaticConstructorOnStartup]
    public class CompTongueCuttable : ThingComp
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Designators/CutTongue");

        private DesignationManager Designations => parent.Map.designationManager;

        private bool Designated => Designations.DesignationOn(parent, TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation) != null;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!TribalTrophiesMod.Settings.ShowTongueCuttingGizmo)
                yield break;

            if (!parent.Spawned)
                yield break;

            // Designation that stopped being possible be cancelled.
            if (!Designated && !TongueCuttingUtility.CanBeCutByAnyone(parent))
                yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "TribalTrophies_TongueCutting_Gizmo_Label".Translate(),
                defaultDesc = "TribalTrophies_TongueCutting_Gizmo_Desc".Translate(),
                icon = Icon,
                isActive = () => Designated,
                toggleAction = Toggle,
                turnOnSound = SoundDefOf.Designate_ExtractSkull
            };
        }

        private void Toggle()
        {
            DesignationDef def = TribalTrophiesDefOf.TribalTrophies_TongueCutting_Designation_CutTongueDesignation;
            if (Designated)
            {
                Designations.TryRemoveDesignationOn(parent, def);
            }
            else
            {
                Designations.AddDesignation(new Designation(parent, def));
            }
        }
    }

    // Corpse defs can not be patched in xml
    [StaticConstructorOnStartup]
    public static class TongueCuttableCompInjector
    {
        static TongueCuttableCompInjector()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null && d.race.Humanlike).ToList())
            {
                Inject(def);
                Inject(def.race.corpseDef);
            }
        }

        private static void Inject(ThingDef def)
        {
            if (def == null)
                return;

            if (def.comps == null)
                def.comps = new List<CompProperties>();

            if (!def.comps.Any(c => c is CompProperties_TongueCuttable))
                def.comps.Add(new CompProperties_TongueCuttable());
        }
    }
}
