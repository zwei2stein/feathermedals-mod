using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TribalTrophies;

public class HarmonyPatches
{
    
    private static ScalpRequestComp previewRequest;

    private static IEnumerable<TrophyBodyPart> ScalpsToBeGiven(List<Tradeable> tradeables)
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
                if (thing is TrophyBodyPart trophy)
                    yield return trophy;
                left -= thing.stackCount;
            }
        }
    }
    
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class PatchFeatherTrophyBiocodeManualWear
    {
        public static void Finalizer(Apparel newApparel)
        {
            if (newApparel is FeatherTrophy) FeatherTrophyUtility.EvaluatingItem = null;
        }
        
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not FeatherTrophy featherTrophy) return true;

            var pawn = __instance.pawn;

            if (featherTrophy.IsAdorned)
            {
                // re-wearing its own trophy (unlocked, stripped, ...) is fine, anyone else's is not
                if (featherTrophy.IsOwnedBy(pawn))
                {
                    // vanilla Wear asks HasPartsToWear about this exact item, see Patch_ApparelUtility_HasPartsToWear
                    FeatherTrophyUtility.EvaluatingItem = featherTrophy;
                    return true;
                }

                Messages.Message(FeatherTrophyUtility.WearBlockedReason(featherTrophy, pawn),
                    featherTrophy.AdornedPawn.Pawn,
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            // The ceremony is the only way to earn a trophy, see RitualOutcomeEffectWorkerAdornTrophy
            if (!FeatherTrophyUtility.ApplyingCeremonyAdorning)
            {
                Messages.Message("TribalTrophies_Trophy_Common_NeedsCeremony".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

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
            
            if (!FeatherTrophyUtility.IsFeatherTrophyDef(apparel))
                return;

            if (FeatherTrophyUtility.ApplyingCeremonyAdorning)
                return;
            
            if (FeatherTrophyUtility.EvaluatingItem is FeatherTrophy item && item.def == apparel && item.IsOwnedBy(p))
                return;
            
            if (FeatherTrophyUtility.PawnOwnsBiocodedFeatherTrophyDef(p, apparel))
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
            if (featherTrophy.IsOwnedBy(pawn))
            {
                // vanilla runs, HasPartsToWear lets the owner through
                FeatherTrophyUtility.EvaluatingItem = featherTrophy;
                return true;
            }

            __result = FeatherTrophyUtility.BlockedWearOption(featherTrophy, FeatherTrophyUtility.WearBlockedReason(featherTrophy, pawn), false);
            return false;
        }

        public static void Finalizer() => FeatherTrophyUtility.EvaluatingItem = null;
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DressOtherPawn), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class PatchDressOtherPawnFeatherTrophy
    {
        public static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedThing is not FeatherTrophy { IsAdorned: true } featherTrophy) return true;

            __result = FeatherTrophyUtility.ForceWearOption(featherTrophy, context.FirstSelectedPawn);
            return false;
        }

        public static void Postfix(Thing clickedThing, ref FloatMenuOption __result)
        {
            if (__result == null || !FeatherTrophyUtility.NeedsCeremony(clickedThing)) return;
            __result = FeatherTrophyUtility.CeremonyOnlyForceWearOption((Apparel)clickedThing);
        }
    }
    
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
    public static class PatchFeatherTrophyConflict
    {
        public static void Postfix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            if (__result)
                return;
            
            if (FeatherTrophyUtility.IsFeatherTrophyDef(A) && FeatherTrophyUtility.IsFeatherTrophyDef(B)) 
                __result = true;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    public static class Patch_PawnGenerator_GenerateGearFor
    {
        public static void Postfix(Pawn pawn)
        {
            NpcScalpGenerator.OnGearGenerated(pawn);
            NpcScalpBeltGenerator.OnGearGenerated(pawn);
            NpcSkullBeltGenerator.OnGearGenerated(pawn);
            NpcTrophyGenerator.OnGearGenerated(pawn);
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
            if (anyThing is not TrophyBodyPart trophy)
                return true;

            if (previewRequest != null && previewRequest.Wants(trophy))
            {
                __result = 0f;
                return false;
            }

            if (ScalpGiftUtility.ReasonsFor(trophy, theirFaction).Count == 0)
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

            var trophies = new List<TrophyBodyPart>();
            foreach (ActiveTransporterInfo pod in pods)
            {
                ThingOwner contents = pod.innerContainer;
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contents[i] is TrophyBodyPart trophy)
                        trophies.Add(trophy);
                }
            }
            __state = ScalpGiftUtility.Evaluate(trophies, giveTo.Faction);
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