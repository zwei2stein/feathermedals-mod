using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{

    public class ScalpPoleExtension : DefModExtension
    {
        public List<Vector3> slotOffsets = new List<Vector3>();
    }

    public class StatPart_ScalpPoleCount : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Building_ScalpPole pole)
                val *= pole.ScalpCount;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is Building_ScalpPole pole)
                return "TribalTrophies_ScalpPole_StatExplanation_DisplayedScalps".Translate(pole.ScalpCount.Named("COUNT")).Resolve();

            return null;
        }
    }
    
    public class FocusStrengthOffset_ScalpPoleScalps : FocusStrengthOffset
    {
        private int Capacity => FeatherMedalDefOf.TribalTrophies_Scalping_Building_ScalpPole.building.maxItemsInCell;

        public override float GetOffset(Thing parent, Pawn user = null)
        {
            if (parent is Building_ScalpPole pole)
                return offset * pole.ScalpCount;
            
            return 0f;
        }

        public override float MaxOffset(Thing parent = null)
        {
            return offset * Capacity;
        }

        public override string GetExplanation(Thing parent)
        {
            if (!parent.Spawned || parent is not Building_ScalpPole pole)
                return GetExplanationAbstract(parent.def);

            return "TribalTrophies_ScalpPole_Meditation_FocusOffset".Translate(
                       pole.ScalpCount.Named("COUNT"),
                       Capacity.Named("MAX"),
                       offset.ToString("0%").Named("PERSCALP"),
                        GetOffset(parent).ToStringWithSign("0%").Named("VALUE")
                       ).Resolve();
        }

        public override string GetExplanationAbstract(ThingDef def = null)
        {
            return "TribalTrophies_ScalpPole_Meditation_FocusOffsetAbstract".Translate(
                Capacity.Named("MAX"),
                offset.ToString("0%").Named("PERSCALP"),
                MaxOffset().ToString("0%").Named("VALUE")
            ).Resolve();
        }
    }


    public class Building_ScalpPole : Building_Storage
    {
        public List<Scalp> StoredScalps()
        {
            var scalps = new List<Scalp>();

            if (!Spawned) return
                scalps;
            
            foreach (Thing thing in slotGroup.HeldThings)
            {
                if (thing is Scalp scalp)
                    scalps.Add(scalp);
            }

            scalps.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));
            return scalps;
        }

        public int ScalpCount => StoredScalps().Count;

        public Vector3 DrawOffsetFor(Scalp scalp)
        {
            var extension = def.GetModExtension<ScalpPoleExtension>();
            if (extension == null)
                return Vector3.zero;

            var index = StoredScalps().IndexOf(scalp);
            if (index < 0 || index >= extension.slotOffsets.Count)
                return Vector3.zero;

            return extension.slotOffsets[index];
        }

        public override string LabelNoCount
        {
            get
            {
                var scalps = StoredScalps();
                var poleLabel = GenLabel.ThingLabel(def, Stuff);

                if (scalps.Count == 1 && !scalps[0].Victim.Name.NullOrEmpty())
                    return "TribalTrophies_ScalpPole_Label_OneScalp"
                        .Translate(poleLabel.Named("POLE"), scalps[0].Victim.Name.Named("VICTIM")).Resolve();

                if (scalps.Count > 0)
                    return "TribalTrophies_ScalpPole_Label_ManyScalps"
                        .Translate(poleLabel.Named("POLE"), scalps.Count.Named("COUNT")).Resolve();

                return base.LabelNoCount;
            }
        }

        public override string DescriptionFlavor
        {
            get
            {
                var scalps = StoredScalps();
                if (scalps.Count == 0)
                    return base.DescriptionFlavor;

                var lines = new List<string>();
                foreach (Scalp scalp in scalps)
                {
                    lines.Add(scalp.LabelNoCount);
                }

                return base.DescriptionFlavor + "\n\n" + string.Join("\n", lines);
            }
        }

        public override string DescriptionDetailed => DescriptionFlavor;
    }

}