using RimWorld;
using UnityEngine;
using Verse;

namespace TribalTrophies
{
    public class Scalp : TrophyBodyPart
    {
        public const float PriceFactor = 0.1f;

        private Color skinColor = new(0.74f, 0.58f, 0.47f);
        private HairDef hairDef = TribalTrophiesDefOf.Messy;
        private Color hairColor = Color.black;
        private Rot4 hairFacing = Rot4.South;

        private Texture2D texture;
        private bool composeTried;
        private Graphic_RuntimeTexture graphic;
        private Graphic rotated; // on ground with a bit of wiggle

        protected override float VictimValueFactor => PriceFactor;

        public override void PostMake()
        {
            base.PostMake();
            hairFacing = RandomHairFacing();
        }

        private static Rot4 RandomHairFacing()
        {
            float roll = Rand.Value;
            if (roll < 0.8f)
                return Rot4.South;
            if (roll < 0.9f)
                return Rot4.East;
            return Rot4.West;
        }

        public static Scalp Create(Pawn victim, Pawn scalper, EventTime scalped)
        {
            var scalp = (Scalp)ThingMaker.MakeThing(TribalTrophiesDefOf.TribalTrophies_Trophy_Scalping_Scalp);

            scalp.Remember(victim, scalper, scalped);

            scalp.skinColor = victim.story?.SkinColor ?? Color.white;
            scalp.hairDef = victim.story?.hairDef;
            scalp.hairColor = victim.story?.HairColor ?? Color.white;

            return scalp;
        }

        public static Scalp CreateFromTemporaryVictim(Pawn victim, Pawn scalper, EventTime scalped)
        {
            var scalp = Create(victim, scalper, scalped);
            scalp.Victim.DiscardPawn();
            return scalp;
        }

        // ===== Texts =====

        protected override string LabelOf(string victimName, string factionPart, string xenotypePart)
        {
            return "TribalTrophies_Scalping_Item_Label".Translate(
                victimName.Named("VICTIM"),
                factionPart.Named("FACTIONPART"),
                xenotypePart.Named("XENOTYPEPART")).Resolve();
        }

        protected override string TakenByDescription(string takerName)
        {
            return "TribalTrophies_Scalping_Item_TakenBy".Translate(takerName.Named("SCALPER"));
        }

        public override string RecordTitle(string victimName)
        {
            return "TribalTrophies_Scalping_ScalpTab_ScalpOf".Translate(victimName.Named("VICTIM")).Resolve();
        }

        public override string RecordTakenBy(string takerName)
        {
            return "TribalTrophies_Scalping_ScalpTab_By".Translate(takerName.Named("SCALPER")).Resolve();
        }

        public override string GiftKindLabel => "TribalTrophies_Scalping_Letter_GiftResented_Kind".Translate();

        public override string GiftReasonText(ScalpGiftReason reason)
        {
            return reason switch
            {
                ScalpGiftReason.Forbidden => "TribalTrophies_Scalping_Letter_GiftResented_Reason_Forbidden".Translate().Resolve(),
                ScalpGiftReason.NoScalpBountyPurchases => "TribalTrophies_Scalping_Letter_GiftResented_Reason_NoBounties".Translate().Resolve(),
                ScalpGiftReason.Victim => "TribalTrophies_Scalping_Letter_GiftResented_Reason_Victim".Translate().Resolve(),
                _ => null
            };
        }

        // Scalps stored on a scalp pole hang one above another.
        public override Vector3 DrawPos
        {
            get
            {
                Vector3 position = base.DrawPos;
                if (Spawned && this.StoringThing() is Building_ScalpPole pole)
                {
                    position += pole.DrawOffsetFor(this);
                }
                return position;
            }
        }

        private Texture2D ScalpTextureWithHair
        {
            get
            {
                if (texture == null && !composeTried)
                {
                    composeTried = true;
                    texture = ScalpTextureFactory.Compose(skinColor, hairDef, hairColor, hairFacing);
                }
                return texture;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (graphic == null && ScalpTextureWithHair != null)
                {
                    graphic = new Graphic_RuntimeTexture(ScalpTextureWithHair, def.graphicData);
                    float angle = def.graphicData?.onGroundRandomRotateAngle ?? 0f;
                    rotated = angle > 0.01f ? new Graphic_RandomRotated(graphic, angle) : graphic;
                }
                return rotated ?? base.Graphic;
            }
        }

        // Inventory, trade and other UI icons.
        public override Texture UIIconOverride => ScalpTextureWithHair;

        private void ReleaseGraphics()
        {
            graphic?.Release();
            graphic = null;
            rotated = null;
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            // destroyed scalp falls back to def graphic/icon
            composeTried = true;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            ReleaseGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref skinColor, "skinColor", Color.white);
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor", Color.white);
            Scribe_Values.Look(ref hairFacing, "hairFacing", Rot4.South);
        }
    }

}
