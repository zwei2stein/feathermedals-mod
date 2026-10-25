using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class HarmonyPatches
{
    
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
    public static class PatchMedalBiocodeManual
    {
        public static void Finalizer(Apparel newApparel)
        {
            if (newApparel is FeatherMedal) FeatherMedalUtil.EvaluatingItem = null;
        }
        
        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (newApparel is not FeatherMedal medal) return true;
            var comp = medal.BiocodeComp;
            if (comp == null) return true;

            var pawn = __instance.pawn;

            if (comp.Biocoded)
            {
                // re-wearing its own trophy (unlocked, stripped, ...) is fine, anyone else's is not
                if (comp.CodedPawn == pawn)
                {
                    // vanilla Wear asks HasPartsToWear about this exact item, see Patch_ApparelUtility_HasPartsToWear
                    FeatherMedalUtil.EvaluatingItem = medal;
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
            if (!RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward)
            {
                Messages.Message("TribalTrophies_Trophy_Common_NeedsCeremony".Translate(), MessageTypeDefOf.RejectInput, false);
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
            if (!__result || p == null)
                return;
            
            if (!FeatherMedalUtil.IsMedalDef(apparel))
                return;

            if (RitualOutcomeEffectWorkerAwardTrophy.ApplyingCeremonyAward)
                return;
            
            if (FeatherMedalUtil.EvaluatingItem is { } item && item.def == apparel && FeatherMedalUtil.IsOwnedBy(item, p))
                return;
            
            if (PawnOwnsBiocodedMedalDef(p, apparel))
                return;

            // only the ceremony can put a trophy on a pawn
            __result = false;
        }
        
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class Patch_Wear_Medal
    {
        public static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedThing is not FeatherMedal medal) return true;

            var pawn = context.FirstSelectedPawn;
            if (FeatherMedalUtil.IsOwnedBy(medal, pawn))
            {
                // vanilla runs, HasPartsToWear lets the owner through
                FeatherMedalUtil.EvaluatingItem = medal;
                return true;
            }

            __result = FeatherMedalUtil.BlockedWearOption(medal, FeatherMedalUtil.WearBlockedReason(medal, pawn), false);
            return false;
        }

        public static void Finalizer() => FeatherMedalUtil.EvaluatingItem = null;
    }
    
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DressOtherPawn), "GetSingleOptionFor", typeof(Thing), typeof(FloatMenuContext))]
    public static class Patch_DressOtherPawn_Medal
    {
        public static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedThing is not FeatherMedal { BiocodeComp: { Biocoded: true } } medal) return true;

            __result = FeatherMedalUtil.ForceWearOption(medal, context.FirstSelectedPawn);
            return false;
        }

        public static void Postfix(Thing clickedThing, ref FloatMenuOption __result)
        {
            if (__result == null || !FeatherMedalUtil.NeedsCeremony(clickedThing)) return;
            __result = FeatherMedalUtil.CeremonyOnlyForceWearOption((Apparel)clickedThing);
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