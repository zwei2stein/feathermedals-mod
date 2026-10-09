using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class HarmonyPatches
{
    
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class PatchMedalBiocodeManual
    {
        
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not FeatherMedal medal) return true;
            var comp = medal.BiocodeComp;
            if (comp == null) return true;

            var pawn = __instance.pawn;

            if (comp.Biocoded)
            {
                // re-wearing its own trophy (unlocked, stripped, ...) is fine, anyone else's is not
                if (comp.CodedPawn == pawn) return true;

                Messages.Message("FeatherMedals_FailTrophyBiocoded".Translate(
                        comp.CodedPawn.Named("PAWN"),
                        pawn.Named("IMPOSTOR")),
                    comp.CodedPawn,
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            // The ceremony is the only way to earn a trophy, see RitualOutcomeEffectWorkerAwardTrophy
            if (!RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward)
            {
                Messages.Message("FeatherMedals_FailTrophyNeedsCeremony".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            comp.CodeFor(pawn);

            if (Prefs.DevMode)
                Log.Message($"[FeatherTrophies] Biocoded {medal.MedalLabel} to {pawn.LabelShort}");

            return true;
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class Patch_ApparelUtility_HasPartsToWear
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null) return;
            if (!FeatherMedalUtil.IsMedalDef(apparel)) return;

            if (RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward) return;
            if (PawnOwnsBiocodedMedalDef(p, apparel)) return;

            // only the ceremony can put a trophy on a pawn
            __result = false;
        }

        private static bool PawnOwnsBiocodedMedalDef(Pawn p, ThingDef apparelDef)
        {
            var worn = p.apparel?.WornApparel;
            if (worn != null)
            {
                for (var i = 0; i < worn.Count; i++)
                {
                    if (worn[i].def != apparelDef) continue;
                    if (worn[i] is FeatherMedal m && m.BiocodeComp is { Biocoded: true } b && b.CodedPawn == p)
                        return true;
                }
            }
            var inv = p.inventory?.innerContainer;
            if (inv == null) return false;
            for (var i = 0; i < inv.Count; i++)
            {
                if (inv[i].def != apparelDef) continue;
                if (inv[i] is FeatherMedal m && m.BiocodeComp is { Biocoded: true } b && b.CodedPawn == p)
                    return true;
            }
            return false;
        }
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DressOtherPawn), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class Patch_DressOtherPawn_Medal
    {
        public static void Postfix(Thing clickedThing, ref FloatMenuOption __result)
        {
            if (__result == null || !FeatherMedalUtil.NeedsCeremony(clickedThing)) return;
            __result = FeatherMedalUtil.CeremonyOnlyForceWearOption((Apparel)clickedThing);
        }
    }

    [HarmonyPatch(typeof(Building_OutfitStand), "GetFloatMenuOptionForForceWear")]
    public static class Patch_OutfitStandForceWear_Medal
    {
        public static void Postfix(Apparel apparel, ref FloatMenuOption __result)
        {
            if (__result == null || !FeatherMedalUtil.NeedsCeremony(apparel)) return;
            __result = FeatherMedalUtil.CeremonyOnlyForceWearOption(apparel);
        }
    }
    
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.CanDrawNow))]
    public static class PatchMedalVisibility
    {
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (!__result) return;
            if (node is not PawnRenderNode_Apparel { apparel: FeatherMedal medal }) return;

            if (!MedalMod.Settings.DrawTrophiesOnPawns
                || !FeatherMedalUtil.TryGetDisplaySlot(parms.pawn, medal, out _, out _))
                __result = false;
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
    public static class PatchMedalConflict
    {
        public static void Postfix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            if (__result)
                return;
            
            if (FeatherMedalUtil.IsMedalDef(A) && FeatherMedalUtil.IsMedalDef(B)) 
                __result = true;
        }
    }
    
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
    public static class PatchMedalGraphicRecord
    {
        public static bool Prefix(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec, ref bool __result)
        {
            if (apparel is not FeatherMedal medal) return true;
            var path = medal.def.apparel.wornGraphicPath;
                
            var color = medal.DrawColor;
            var colorTwo = medal.DrawColorTwo;

            var graphic = GraphicDatabase.Get<Graphic_Single>(
                path, 
                ShaderDatabase.CutoutComplex, 
                Vector2.one, 
                color, 
                colorTwo
            );
                
            rec = new ApparelGraphicRecord(graphic, apparel);
            __result = true; 
            return false;

        }
    }

    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.FlipGraphic))]
    public static class PatchMedalFlip
    {
         public static void Postfix(PawnRenderNode __instance, PawnDrawParms parms, ref bool __result)
        {
            if (__instance is PawnRenderNode_Apparel { apparel: FeatherMedal } && parms.facing.IsHorizontal)
                __result = !__result;
        }
    }

    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
    public static class PatchMedalScale
    {
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (node is not PawnRenderNode_Apparel apparelNode || apparelNode.apparel is not FeatherMedal) return;
            var baseScale = 0.3f; 
            var bodyModifier = GetScale(node, parms);
            __result *= (baseScale * bodyModifier);
        }
        
        public static float GetScale(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is not PawnRenderNode_Apparel apparelNode || apparelNode.apparel is not FeatherMedal) return 0f;
            
            var bodyModifier = 1.0f; // Default for Male
            
            var bodyType = parms.pawn?.story?.bodyType;
            if (bodyType != null)
            {
                if (bodyType == BodyTypeDefOf.Hulk || bodyType == BodyTypeDefOf.Fat) bodyModifier = 1.15f;
                else if (bodyType == BodyTypeDefOf.Thin) bodyModifier = 0.9f;
                else if (bodyType == BodyTypeDefOf.Female) bodyModifier = 0.95f;
            }
            bodyModifier *= MedalMod.Settings.TrophyScale;
            return bodyModifier;
        }
    }
    
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
    public static class PatchMedalOffset
    {
        private const float BaseXOffset = 0.02f; 
        private const float RowZDrop = 0.1f; 
        
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            if (node is not PawnRenderNode_Apparel { apparel: FeatherMedal medal })
                return;

            __result.z += -0.148f;
            var scale = PatchMedalScale.GetScale(node, parms);
            var indexOffset = 0.1f * scale;
            var shift = 0.02f * scale;
            var maxMedalsPerRow = 8;

            __result.x += (parms.facing == Rot4.West || parms.facing == Rot4.East) ? 0 : shift;

            // trophies are laid out together on body part.
            if (!FeatherMedalUtil.TryGetDisplaySlot(parms.pawn, medal, out var myIndex, out var totalMedals))
                return;

            var reverseIndex = (totalMedals - 1) - myIndex;

            var row = reverseIndex / maxMedalsPerRow;
            var col = reverseIndex % maxMedalsPerRow;

            var totalRows = (totalMedals + maxMedalsPerRow - 1) / maxMedalsPerRow;
            var medalsInThisRow = maxMedalsPerRow;

            // If we are looking at the very bottom row, check if it's partially empty
            if (row == totalRows - 1 && totalMedals % maxMedalsPerRow != 0)
                medalsInThisRow = totalMedals % maxMedalsPerRow;

            // Shift the column to the right by half the missing width
            var missingMedals = maxMedalsPerRow - medalsInThisRow;
            var centeredCol = col + (missingMedals / 2f);

            // Apply X shift using our new 'centeredCol' instead of the raw 'col'
            var baseX = (parms.facing == Rot4.West || parms.facing == Rot4.East) ? 0 : BaseXOffset;
            var shiftX = baseX + (centeredCol * indexOffset);
            
            if (parms.facing == Rot4.West || parms.facing == Rot4.North)
                __result.x -= shiftX;
            else
                __result.x += shiftX;

            // Apply Z (Vertical) shift
            if (parms.facing == Rot4.South)
                __result.x -= BaseXOffset * 10;
            if (parms.facing == Rot4.North)
                __result.x += BaseXOffset * 10;
            __result.z -= (row * (RowZDrop / 2 ) * scale) - 0.75f;
        }
    }
    
}