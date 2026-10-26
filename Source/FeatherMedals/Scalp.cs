using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals
{
    public class Scalp : ThingWithComps
    {
        public const float PriceFactor = 0.1f;
        
        private RememberedPawn victim = new("TribalTrophies_Scalping_Misc_UnknownName".Translate());
        private RememberedPawn scalper = new("TribalTrophies_Scalping_Misc_UnknownName".Translate());

        private string xenotypeName;

        private EventTime scalped; // null = unknown, e.g. spawned by dev tools

        private Color skinColor = new(0.74f, 0.58f, 0.47f);
        private HairDef hairDef = TribalTrophiesDefOf.Messy;
        private Color hairColor = Color.black;
        private Rot4 hairFacing = Rot4.South;

        private float priceBonus;

        private Texture2D texture;
        private bool composeTried;
        private Graphic_RuntimeTexture graphic;
        private Graphic rotated; // on ground with a bit of wiggle
        
        public float PriceBonus => priceBonus;

        public RememberedPawn Victim => victim;
        public RememberedPawn Scalper => scalper;
        public EventTime Scalped => scalped;
        public string XenotypeName => xenotypeName;
        
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

            scalp.victim = new RememberedPawn(victim);
            scalp.scalper = new RememberedPawn(scalper);
            
            if (victim.genes != null && (victim.genes.Xenotype != XenotypeDefOf.Baseliner || victim.genes.CustomXenotype != null))
                scalp.xenotypeName = victim.genes.XenotypeLabel;
            
            scalp.scalped = scalped;

            scalp.skinColor = victim.story?.SkinColor ?? Color.white;
            scalp.hairDef = victim.story?.hairDef;
            scalp.hairColor = victim.story?.HairColor ?? Color.white;

            scalp.priceBonus = victim.MarketValue * PriceFactor;

            return scalp;
        }
        
        public static Scalp CreateFromTemporaryVictim(Pawn victim, Pawn scalper, EventTime scalped)
        {
            var scalp = Create(victim, scalper, scalped);
            scalp.victim.DiscardPawn();
            return scalp;
        }

        private string ScalpOf
        {
            get
            {
                if (victim == null || victim.Name.NullOrEmpty())
                {
                    return null;
                }

                var factionPart = victim.FactionName.NullOrEmpty()
                    ? ""
                    : "TribalTrophies_Scalping_Item_Label_FactionPart".Translate(victim.FactionName.Named("FACTION")).Resolve();

                var xenotypePart = xenotypeName.NullOrEmpty()
                    ? ""
                    : "TribalTrophies_Scalping_Item_Label_XenotypePart".Translate(xenotypeName.CapitalizeFirst().Named("XENOTYPE")).Resolve();

                return "TribalTrophies_Scalping_Item_Label".Translate(
                    victim.Name.Named("VICTIM"),
                    factionPart.Named("FACTIONPART"),
                    xenotypePart.Named("XENOTYPEPART")).Resolve();
            }
        }

        public override string LabelNoCount => ScalpOf ?? base.LabelNoCount;

        public override string DescriptionFlavor
        {
            get
            {
                var descriptions = new List<string>();
                
                descriptions.Add(ScalpOf?.CapitalizeFirst());
                descriptions.Add(base.DescriptionFlavor);
                
                var scalperName = scalper?.Name;
                if (!scalperName.NullOrEmpty())
                    descriptions.Add("TribalTrophies_Scalping_Item_TakenBy".Translate(scalperName.Named("SCALPER")));

                return string.Join("\n\n", descriptions.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public override string DescriptionDetailed => DescriptionFlavor;

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

        public override bool CanStackWith(Thing other)
        {
            return false;
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
            Scribe_Deep.Look(ref victim, "victim");
            Scribe_Deep.Look(ref scalper, "scalper");
            Scribe_Values.Look(ref xenotypeName, "xenotypeName");
            Scribe_Deep.Look(ref scalped, "scalped");
            Scribe_Values.Look(ref skinColor, "skinColor", Color.white);
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor", Color.white);
            Scribe_Values.Look(ref hairFacing, "hairFacing", Rot4.South);
            Scribe_Values.Look(ref priceBonus, "priceBonus");
        }
    }
    
    
    public class CompProperties_ScalpPrice : CompProperties
    {
        public CompProperties_ScalpPrice()
        {
            compClass = typeof(CompScalpPrice);
        }
    }

    public class CompScalpPrice : ThingComp
    {
        public override float GetStatOffset(StatDef stat)
        {
            if (stat == StatDefOf.MarketValue && parent is Scalp scalp)
            {
                return scalp.PriceBonus;
            }
            return 0f;
        }
    }
    
}