using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class HarmonyPatches
{
    
    private static ScalpRequestComp previewRequest;

    private static IEnumerable<Scalp> ScalpsToBeGiven(List<Tradeable> tradeables)
    {
        foreach (Tradeable tradeable in tradeables)
        {
            if (tradeable.ActionToDo != TradeAction.PlayerSells)
                continue;

            var left = Mathf.Min(tradeable.CountToTransferToDestination, tradeable.CountHeldBy(Transactor.Colony));
            foreach (var thing in tradeable.thingsColony)
            {
                if (left <= 0)
                    break;
                if (thing is Scalp scalp)
                    yield return scalp;
                left -= thing.stackCount;
            }
        }
    }
    
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class PatchFeatherTrophyBiocodeManualWear
    {
        public static void Finalizer(Apparel newApparel)
        {
            if (newApparel is FeatherTrophy) FeatherTrophyUtil.EvaluatingItem = null;
        }
        
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not FeatherTrophy featherTrophy) return true;
            var comp = featherTrophy.BiocodeComp;
            if (comp == null) return true;

            var pawn = __instance.pawn;

            if (comp.Biocoded)
            {
                // re-wearing its own trophy (unlocked, stripped, ...) is fine, anyone else's is not
                if (comp.CodedPawn == pawn)
                {
                    // vanilla Wear asks HasPartsToWear about this exact item, see Patch_ApparelUtility_HasPartsToWear
                    FeatherTrophyUtil.EvaluatingItem = featherTrophy;
                    return true;
                }

                Messages.Message("TribalTrophies_Trophy_Common_OwnedByOther".Translate(
                        comp.CodedPawn.Named("PAWN"),
                        pawn.Named("IMPOSTOR")),
                    comp.CodedPawn,
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            // The ceremony is the only way to earn a trophy, see RitualOutcomeEffectWorkerAwardTrophy
            if (!RitualOutcomeEffectWorkerAdornTrophy.ApplyingCeremonyAdorning)
            {
                Messages.Message("TribalTrophies_Trophy_Common_NeedsCeremony".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            comp.CodeFor(pawn);

            if (Prefs.DevMode)
                Log.Message($"[TribalTrophies] Biocoded {featherTrophy.TrophyLabel} to {pawn.LabelShort}");

            return true;
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class Patch_ApparelUtility_HasPartsToWear
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null)
                return;
            
            if (!FeatherTrophyUtil.IsFeatherTrophyDef(apparel))
                return;

            if (RitualOutcomeEffectWorkerAdornTrophy.ApplyingCeremonyAdorning)
                return;
            
            if (FeatherTrophyUtil.EvaluatingItem is { } item && item.def == apparel && FeatherTrophyUtil.IsOwnedBy(item, p))
                return;
            
            if (FeatherTrophyUtil.PawnOwnsBiocodedFeatherTrophyDef(p, apparel))
                return;

            // only the ceremony can put a trophy on a pawn
            __result = false;
        }
        
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class PatchWearFeatherTrophy
    {
        public static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedThing is not FeatherTrophy featherTrophy) return true;

            var pawn = context.FirstSelectedPawn;
            if (FeatherTrophyUtil.IsOwnedBy(featherTrophy, pawn))
            {
                // vanilla runs, HasPartsToWear lets the owner through
                FeatherTrophyUtil.EvaluatingItem = featherTrophy;
                return true;
            }

            __result = FeatherTrophyUtil.BlockedWearOption(featherTrophy, FeatherTrophyUtil.WearBlockedReason(featherTrophy, pawn), false);
            return false;
        }

        public static void Finalizer() => FeatherTrophyUtil.EvaluatingItem = null;
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DressOtherPawn), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class PatchDressOtherPawnFeatherTrophy
    {
        public static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedThing is not FeatherTrophy { BiocodeComp: { Biocoded: true } } featherTrophy) return true;

            __result = FeatherTrophyUtil.ForceWearOption(featherTrophy, context.FirstSelectedPawn);
            return false;
        }

        public static void Postfix(Thing clickedThing, ref FloatMenuOption __result)
        {
            if (__result == null || !FeatherTrophyUtil.NeedsCeremony(clickedThing)) return;
            __result = FeatherTrophyUtil.CeremonyOnlyForceWearOption((Apparel)clickedThing);
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
    public static class PatchFeatherTrophyConflict
    {
        public static void Postfix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            if (__result)
                return;
            
            if (FeatherTrophyUtil.IsFeatherTrophyDef(A) && FeatherTrophyUtil.IsFeatherTrophyDef(B)) 
                __result = true;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    public static class Patch_PawnGenerator_GenerateGearFor
    {
        public static void Postfix(Pawn pawn)
        {
            NpcTrophyGenerator.OnGearGenerated(pawn);
            NpcScalpGenerator.OnGearGenerated(pawn);
            NpcScalpBeltGenerator.OnGearGenerated(pawn);
            NpcSkullBeltGenerator.OnGearGenerated(pawn);
        }
    }

    [HarmonyPatch(typeof(Faction), nameof(Faction.TryGenerateNewLeader))]
    public static class Patch_Faction_TryGenerateNewLeader
    {
        public static void Postfix(Faction __instance, bool __result)
        {
            if (__result)
                NpcTrophyGenerator.OnLeaderChosen(__instance.leader);
        }
    }
    
    [HarmonyPatch(typeof(FactionGiftUtility), "GetBaseGoodwillChange")]
    public static class Patch_GetBaseGoodwillChange
    {
        public static bool Prefix(Thing anyThing, Faction theirFaction, ref float __result)
        {
            if (anyThing is not Scalp scalp)
                return true;

            if (previewRequest != null && previewRequest.Wants(scalp))
            {
                __result = 0f;
                return false;
            }

            if (ScalpGiftUtility.ReasonsFor(scalp, theirFaction).Count == 0)
                return true;

            __result = 0f;
            return false;
        }
    }

    // Pod gift preview (float menu label)
    [HarmonyPatch(typeof(FactionGiftUtility), nameof(FactionGiftUtility.GetGoodwillChange),
        new[] { typeof(IEnumerable<IThingHolder>), typeof(Settlement) })]
    public static class Patch_GetGoodwillChange_Pods
    {
        public static void Prefix(Settlement giveTo)
        {
            previewRequest = giveTo?.GetComponent<ScalpRequestComp>();
        }

        public static void Finalizer()
        {
            previewRequest = null;
        }
    }

    // Drop pods
    [HarmonyPatch(typeof(FactionGiftUtility), nameof(FactionGiftUtility.GiveGift),
        new[] { typeof(List<ActiveTransporterInfo>), typeof(Settlement) })]
    public static class Patch_GiveGift_Pods
    {
        public static void Prefix(List<ActiveTransporterInfo> pods, Settlement giveTo, out ScalpGiftResult __state)
        {
            giveTo.GetComponent<ScalpRequestComp>()?.TakeFromPods(pods);

            var scalps = new List<Scalp>();
            foreach (ActiveTransporterInfo pod in pods)
            {
                ThingOwner contents = pod.innerContainer;
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contents[i] is Scalp scalp)
                        scalps.Add(scalp);
                }
            }
            __state = ScalpGiftUtility.Evaluate(scalps, giveTo.Faction);
        }

        public static void Postfix(Settlement giveTo, ScalpGiftResult __state)
        {
            ScalpGiftUtility.Apply(__state, giveTo.Faction, (GlobalTargetInfo)(WorldObject)giveTo);
        }
    }
    
    // Caravan gift
    [HarmonyPatch(typeof(FactionGiftUtility), nameof(FactionGiftUtility.GiveGift),
        new[] { typeof(List<Tradeable>), typeof(Faction), typeof(GlobalTargetInfo) })]
    public static class Patch_GiveGift_Tradeables
    {
        public static void Prefix(List<Tradeable> tradeables, Faction giveTo, out ScalpGiftResult __state)
        {
            __state = ScalpGiftUtility.Evaluate(ScalpsToBeGiven(tradeables), giveTo);
        }

        public static void Postfix(Faction giveTo, GlobalTargetInfo lookTarget, ScalpGiftResult __state)
        {
            ScalpGiftUtility.Apply(__state, giveTo, lookTarget);
        }
        
    }
    
}