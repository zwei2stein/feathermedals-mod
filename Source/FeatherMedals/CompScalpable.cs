using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies
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
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Designators/Scalp");

        private DesignationManager Designations => parent.Map.designationManager;

        private bool Designated => Designations.DesignationOn(parent, TribalTrophiesDefOf.TribalTrophies_Scalping_Designation_ScalpDesignation) != null;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!TribalTrophiesMod.Settings.ShowScalpingGizmo)
                yield break;
            
            if (!parent.Spawned)
                yield break;
            
            // Designation that stopped being possible be cancelled.
            if (!Designated && !ScalpingUtility.CanBeScalpedByAnyone(parent))
                yield break;
            
            var rule = TrophyRuleUtility.RuleForColony(ScalpingUtility.PawnOf(parent));
            var ruleDesc = TrophyRuleUtility.Description(rule);

            yield return new Command_TrophyToggle
            {
                defaultLabel = "TribalTrophies_Scalping_Gizmo_Label".Translate(),
                defaultDesc = ruleDesc.NullOrEmpty()
                    ? "TribalTrophies_Scalping_Gizmo_Desc".Translate().Resolve()
                    : "TribalTrophies_Scalping_Gizmo_Desc".Translate().Resolve() + "\n\n" + ruleDesc,
                backgroundTint = rule == TrophyRule.Merciless ? (Color?)TrophyUIStyle.MercilessGizmoTint : null,
                groupKey = (int)rule,
                icon = Icon,
                isActive = () => Designated,
                toggleAction = Toggle,
                turnOnSound = SoundDefOf.Designate_ExtractSkull
            };
        }

        private void Toggle()
        {
            DesignationDef def = TribalTrophiesDefOf.TribalTrophies_Scalping_Designation_ScalpDesignation;
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
                def.comps = new List<CompProperties>();
            
            if (!def.comps.Any(c => c is CompProperties_Scalpable))
                def.comps.Add(new CompProperties_Scalpable());
            
        }
    }
}