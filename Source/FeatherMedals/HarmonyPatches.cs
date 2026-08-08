using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FeatherMedals;

public class HarmonyPatches
{
    
    public static bool CheckRitualStatus(Pawn pawn)
    {
        if (pawn.GetLord()?.LordJob is not LordJob_Ritual ritual) return false;
        var role = ritual.RoleFor(pawn);
        return role != null;
    }
    
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class PatchMedalBiocodeManual
    {
        
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not FeatherMedal medal) return true;
            var comp = medal.BiocodeComp;
            if (comp == null) return true;

            // The bestowal Apply path bypasses the ceremony rejection — see ApplyingCeremonyAward.
            if (!RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward
                && MedalMod.Settings.TrophiesRequireCeremony
                && !CheckRitualStatus(__instance.pawn)
                && !comp.Biocoded)
            {
                Messages.Message($"FeatherMedals_FailTrophyNeedsCeremony".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            if (comp.Biocoded && comp.CodedPawn != __instance.pawn)
            {
                Messages.Message("FeatherMedals_FailTrophyBiocoded".Translate(
                        comp.CodedPawn.Named("PAWN"),
                        __instance.pawn.Named("IMPOSTOR")),
                    comp.CodedPawn,
                    MessageTypeDefOf.RejectInput, 
                    false);
                return false; 
            }
            
            if (!comp.Biocoded)
            {
                comp.CodeFor(__instance.pawn);
                var cleanLabel = GenLabel.ThingLabel(medal.def, medal.Stuff, 1);
                var nameValue = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanLabel);
                Log.Message($"[FeatherMedals] Biocoded {cleanLabel} to {__instance.pawn.LabelShort}");
                Messages.Message( "FeatherMedals_TrophyAdornedWithoutRitual".Translate(__instance.pawn.Named("PAWN"), nameValue.Named("TROPHY")), __instance.pawn, MessageTypeDefOf.PositiveEvent);
                if (__instance.pawn.needs is { mood: not null })
                {
                    var awardedThought = FeatherMedalDefOf.FeatherMedals_AwardedTrophy_Thought;
                    if (awardedThought != null) 
                        __instance.pawn.needs.mood.thoughts.memories.TryGainMemory(awardedThought);
                }
            }
            return true; 
        }
    }
    
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.IsLocked))]
    public static class PatchRankAlwaysForced
    {
        public static void Postfix(Apparel __0, ref bool __result)
        {
            if (__0 is FeatherMedal medal)
                __result = medal.isLocked;
        }
    }
    
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.Strip))]
    public static class PatchCorpseStripMedals
    {
        public static void Prefix(Corpse __instance, out List<FeatherMedal> __state)
        {
            __state = null;
            var pawn = __instance.InnerPawn;
            if (pawn?.apparel == null) return;

            __state = pawn.apparel.WornApparel
                .OfType<FeatherMedal>()
                .Where(m => m.isLocked)
                .ToList();
        }

        public static void Postfix(Corpse __instance, List<FeatherMedal> __state)
        {
            if (__state == null || __state.Count == 0) return;
            var pawn = __instance.InnerPawn;
            if (pawn?.apparel == null) return;

            foreach (var medal in __state)
            {
                if (medal.Destroyed) continue;
                if (pawn.apparel.WornApparel.Contains(medal)) continue;

                if (medal.Spawned) medal.DeSpawn();
                pawn.apparel.Wear(medal, false, true);
            }
        }
    }
    
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Strip))]
    public static class PatchPawnStripMedals
    {
        public static void Prefix(Pawn __instance, out List<FeatherMedal> __state)
        {
            __state = null;
            if (__instance.apparel == null) return;

            __state = __instance.apparel.WornApparel
                .OfType<FeatherMedal>()
                .Where(m => m.isLocked)
                .ToList();
        }

        public static void Postfix(Pawn __instance, List<FeatherMedal> __state)
        {
            if (__state == null || __state.Count == 0) return;
            if (__instance.apparel == null) return;

            foreach (var medal in __state)
            {
                if (medal.Destroyed) continue;
                if (__instance.apparel.WornApparel.Contains(medal)) continue;

                if (medal.Spawned) medal.DeSpawn();
                __instance.apparel.Wear(medal, false, true);
            }
        }
    }

    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class Patch_ApparelUtility_HasPartsToWear
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null) return;
            if (!typeof(FeatherMedal).IsAssignableFrom(apparel.thingClass)) return;

            if (RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward) return;
            if (PawnOwnsBiocodedMedalDef(p, apparel)) return;

            if (MedalMod.Settings.TrophiesRequireCeremony && !CheckRitualStatus(p))
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
    
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.CanDrawNow))]
    public static class PatchMedalVisibility
    {
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (!__result) return;
            if (node is not PawnRenderNode_Apparel { apparel: FeatherMedal apparelNode}) return;
            if (!MedalMod.Settings.DrawTrophiesOnPawns)
            {
                __result = false;
                return;
            }
            
            var worn = parms.pawn.apparel.WornApparel; // This is a List<Apparel>
            var maxDisplayed = MedalMod.Settings.MaxDisplayedTrophies;
            var myIndex = 0;

            for (var i = 0; i < worn.Count; i++)
            {
                if (worn[i] is not FeatherMedal) continue;
                if (worn[i] == apparelNode)
                {
                    if (myIndex >= maxDisplayed) __result = false;
                    return;
                }
                myIndex++;
            }
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
    public static class PatchMedalConflict
    {
        public static void Postfix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            if (__result) return;
            var aIsMedal = typeof(FeatherMedal).IsAssignableFrom(A.thingClass);
            var bIsMedal = typeof(FeatherMedal).IsAssignableFrom(B.thingClass);
            if (aIsMedal && bIsMedal) 
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
            if (node is not PawnRenderNode_Apparel apparelNode || apparelNode.apparel is not FeatherMedal) 
                return;

            __result.z += -0.148f;
            var scale = PatchMedalScale.GetScale(node, parms);
            var indexOffset = 0.1f * scale;
            var bodyPartGroups = apparelNode.apparel?.def?.apparel?.bodyPartGroups;
            var shift = 0.02f * scale;
            var maxMedalsPerRow = 8;

            __result.x += (parms.facing == Rot4.West || parms.facing == Rot4.East) ? 0 : shift;

            var worn = parms.pawn.apparel.WornApparel; // This is a List<Apparel>
            var totalMedals = 0;
            var myIndex = -1;

            //medals per body part get grouped together - usually Torso, UpperHead
            for (var i = 0; i < worn.Count; i++)
            {
                if (worn[i] is FeatherMedal && worn[i].def.apparel.bodyPartGroups.Equals(bodyPartGroups))
                {
                    if (worn[i] == apparelNode.apparel)
                        myIndex = totalMedals;

                    totalMedals++;
                }
            }
            if (myIndex == -1) return;

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