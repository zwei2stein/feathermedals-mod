using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    public class CompProperties_Scalpable : CompProperties
    {
        public CompProperties_Scalpable()
        {
            compClass = typeof(CompScalpable);
        }
    }

    [StaticConstructorOnStartup]
    public class CompScalpable : ThingComp
    {
        // Placeholder icon, swap for own texture later.
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Designators/Slaughter");

        private DesignationManager Designations => parent.Map.designationManager;

        private bool Designated => Designations.DesignationOn(parent, FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation) != null;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.Spawned)
            {
                yield break;
            }
            // Designation that stopped being possible be cancelled.
            if (!Designated && !ScalpingUtility.CanBeScalped(parent))
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "FeatherTrophies_Scalping_GizmoLabel".Translate(),
                defaultDesc = "FeatherTrophies_Scalping_GizmoDesc".Translate(),
                icon = Icon,
                isActive = () => Designated,
                toggleAction = Toggle
            };
        }

        private void Toggle()
        {
            DesignationDef def = FeatherMedalDefOf.FeatherTrophies_Scalping_ScalpDesignation;
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
    public static class ScalpableCompInjector
    {
        static ScalpableCompInjector()
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
            {
                def.comps = new List<CompProperties>();
            }
            if (!def.comps.Any(c => c is CompProperties_Scalpable))
            {
                def.comps.Add(new CompProperties_Scalpable());
            }
        }
    }
}