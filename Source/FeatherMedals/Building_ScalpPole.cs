using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies
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

    public class FocusStrengthOffset_ScalpPoleScalps : FocusStrengthOffset_StoredTrophies
    {
        protected override ThingDef StorageDef => TribalTrophiesDefOf.TribalTrophies_Scalping_Building_ScalpPole;

        protected override string Explanation(int count, int max, string perTrophy, string value)
        {
            return "TribalTrophies_ScalpPole_Meditation_FocusOffset".Translate(
                       count.Named("COUNT"),
                       max.Named("MAX"),
                       perTrophy.Named("PERSCALP"),
                       value.Named("VALUE")
                       ).Resolve();
        }

        protected override string ExplanationAbstract(int max, string perTrophy, string value)
        {
            return "TribalTrophies_ScalpPole_Meditation_FocusOffsetAbstract".Translate(
                max.Named("MAX"),
                perTrophy.Named("PERSCALP"),
                value.Named("VALUE")
            ).Resolve();
        }
    }


    public class Building_ScalpPole : Building_TrophyContainer
    {
        public List<Scalp> StoredScalps()
        {
            var scalps = new List<Scalp>();

            foreach (var trophy in StoredTrophies())
            {
                if (trophy is Scalp scalp)
                    scalps.Add(scalp);
            }

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

        // Scalps hang on the pole one above another, drawn by the pole (see vanilla Building_Bookcase drawing its books).
        // Slots are measured from the pole as drawn, so they follow the pole's draw offset.
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            var poleCenter = drawLoc + def.graphicData.DrawOffsetForRot(Rotation);
            foreach (var scalp in StoredScalps())
            {
                scalp.Graphic.Draw(poleCenter + DrawOffsetFor(scalp), Rot4.North, scalp);
            }
        }

        protected override string LabelWithOne(string storageLabel, string victimName)
        {
            return "TribalTrophies_ScalpPole_Label_OneScalp"
                .Translate(storageLabel.Named("POLE"), victimName.Named("VICTIM")).Resolve();
        }

        protected override string LabelWithMany(string storageLabel, int count)
        {
            return "TribalTrophies_ScalpPole_Label_ManyScalps"
                .Translate(storageLabel.Named("POLE"), count.Named("COUNT")).Resolve();
        }
    }

}
