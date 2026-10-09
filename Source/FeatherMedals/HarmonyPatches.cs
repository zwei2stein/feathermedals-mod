using HarmonyLib;
using RimWorld;
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
    
}