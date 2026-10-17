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
        
        private RememberedPawn victim = new("FeatherTrophies_Scalp_Anonymous".Translate());
        private RememberedPawn scalper = new("FeatherTrophies_Scalp_Anonymous".Translate());

        private string factionName;
        private string xenotypeName;

        private Color skinColor = new Color(0.74f, 0.58f, 0.47f);
        private HairDef hairDef = FeatherMedalDefOf.Messy;
        private Color hairColor = Color.black;

        private float priceBonus;

        private Texture2D texture;
        private bool bakeTried;
        private Graphic_Scalp graphic;
        private Graphic rotated; // on ground with a bit of wiggle
        
        public float PriceBonus => priceBonus;
        public Pawn VictimPawn => victim?.Pawn;

        public static Scalp Create(Pawn victim, Pawn scalper)
        {
            var scalp = (Scalp)ThingMaker.MakeThing(FeatherMedalDefOf.FeatherTrophies_Scalping_Scalp);

            scalp.victim = new RememberedPawn(victim);
            scalp.scalper = new RememberedPawn(scalper);

            scalp.factionName = (victim.HomeFaction ?? victim.Faction)?.Name;
            
            if (victim.genes != null && (victim.genes.Xenotype != XenotypeDefOf.Baseliner || victim.genes.CustomXenotype != null))
                scalp.xenotypeName = victim.genes.XenotypeLabel;

            scalp.skinColor = victim.story?.SkinColor ?? Color.white;
            scalp.hairDef = victim.story?.hairDef;
            scalp.hairColor = victim.story?.HairColor ?? Color.white;

            scalp.priceBonus = victim.MarketValue * PriceFactor;

            return scalp;
        }

        private string ScalpOf
        {
            get
            {
                string victimName = victim?.Name;
                if (victimName.NullOrEmpty())
                {
                    return null;
                }
                
                var itemName = new List<string>();
                
                itemName.Add("FeatherTrophies_Scalp_Label".Translate(victimName.Named("VICTIM")).Resolve());

                if (!factionName.NullOrEmpty())
                    itemName.Add("FeatherTrophies_Scalp_LabelFaction".Translate(factionName.Named("FACTION")).Resolve());
                
                if (!xenotypeName.NullOrEmpty())
                    itemName.Add("FeatherTrophies_Scalp_Xenotype".Translate(xenotypeName.CapitalizeFirst().Named("XENOTYPE")).Resolve());
                
                itemName.Add(".");
                
                return string.Join("", itemName);
            }
        }

        public override string LabelNoCount => ScalpOf ?? base.LabelNoCount;

        public override string DescriptionFlavor
        {
            get
            {
                var descriptions = new List<string>();
                
                descriptions.Add(ScalpOf);
                descriptions.Add(base.DescriptionFlavor);
                
                var scalperName = scalper?.Name;
                if (!scalperName.NullOrEmpty())
                    descriptions.Add("FeatherTrophies_Scalp_TakenBy".Translate(scalperName.Named("SCALPER")));

                return string.Join("\n\n", descriptions.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        public override string DescriptionDetailed => DescriptionFlavor;

        public override bool CanStackWith(Thing other)
        {
            return false;
        }

        private Texture2D ScalpTextureWithHair
        {
            get
            {
                if (texture == null && !bakeTried)
                {
                    bakeTried = true;
                    texture = ScalpTextureBaker.Bake(skinColor, hairDef, hairColor);
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
                    graphic = new Graphic_Scalp(ScalpTextureWithHair, def.graphicData);
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
            bakeTried = false;
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
            Scribe_Values.Look(ref factionName, "factionName");
            Scribe_Values.Look(ref xenotypeName, "xenotypeName");
            Scribe_Values.Look(ref skinColor, "skinColor", Color.white);
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor", Color.white);
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