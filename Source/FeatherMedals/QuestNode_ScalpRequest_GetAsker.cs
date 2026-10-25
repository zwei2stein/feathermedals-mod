using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class QuestNode_ScalpRequest_GetAsker : QuestNode
{
    [NoTranslate] public SlateRef<string> storeAs;
    [NoTranslate] public SlateRef<string> storeFactionAs;
    [NoTranslate] public SlateRef<string> storeFactionLeaderAs;
    [NoTranslate] public SlateRef<string> storeAskerHostileAs;

    private static Settlement RandomAskerSettlement(PlanetTile originTile)
    {
        return Find.WorldObjects.SettlementBases.Where(settlement => IsGoodAsker(settlement, originTile)).RandomElementWithFallback();
    }

    private static bool IsGoodAsker(Settlement settlement, PlanetTile originTile)
    {
        if (settlement.GetComponent<ScalpRequestComp>() == null || settlement.Tile.LayerDef.isSpace)
            return false;
        if (!ScalpRequestUtility.CanAsk(settlement.Faction))
            return false;
        if (GravshipUtility.PlayerHasGravEngine())
            return true;
        if (!originTile.Valid || settlement.Tile.Layer != originTile.Layer)
            return false;
        return Find.WorldGrid.ApproxDistanceInTiles(originTile, settlement.Tile) < MedalMod.Settings.ScalpRequestMaxDistance;
    }

    private void Store(Slate slate, Settlement settlement)
    {
        slate.Set(storeAs.GetValue(slate), settlement);
        if (!storeFactionAs.GetValue(slate).NullOrEmpty())
            slate.Set(storeFactionAs.GetValue(slate), settlement.Faction);
        if (!storeFactionLeaderAs.GetValue(slate).NullOrEmpty())
            slate.Set(storeFactionLeaderAs.GetValue(slate), settlement.Faction.leader);
        if (!storeAskerHostileAs.GetValue(slate).NullOrEmpty())
            slate.Set(storeAskerHostileAs.GetValue(slate), settlement.Faction.HostileTo(Faction.OfPlayer));
    }

    protected override bool TestRunInt(Slate slate)
    {
        if (!MedalMod.Settings.ScalpRequestQuests)
            return false;
        var map = slate.Get<Map>("map");
        if (map == null)
            return false;
        var settlement = RandomAskerSettlement(map.Tile);
        if (settlement == null)
            return false;
        Store(slate, settlement);
        return true;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var map = slate.Get<Map>("map");
        var settlement = RandomAskerSettlement(map.Tile);
        if (settlement == null)
            return;
        Store(slate, settlement);
    }
}

public class QuestNode_ScalpRequest_GetTarget : QuestNode
{
    public SlateRef<Faction> asker;
    [NoTranslate] public SlateRef<string> storeAs;

    protected override bool TestRunInt(Slate slate)
    {
        var askerFaction = asker.GetValue(slate);
        if (askerFaction == null || !ScalpRequestUtility.TryFindTarget(askerFaction, out var target))
            return false;
        slate.Set(storeAs.GetValue(slate), target);
        return true;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var askerFaction = asker.GetValue(slate);
        if (askerFaction == null || !ScalpRequestUtility.TryFindTarget(askerFaction, out var target))
            return;
        slate.Set(storeAs.GetValue(slate), target);
    }
}

public class QuestNode_ScalpRequest_GetRequest : QuestNode
{
    public SlateRef<Faction> targetFaction;
    public SlateRef<IntRange> countRange;
    public SlateRef<int> extraStarAboveCount;
    public SlateRef<float> rewardValuePerScalp;
    public SlateRef<float> rewardFactorPerStar;
    public SlateRef<int> goodwillPerScalp;

    [NoTranslate] public SlateRef<string> storeCountAs;
    [NoTranslate] public SlateRef<string> storeRewardValueAs;
    [NoTranslate] public SlateRef<string> storeGoodwillAs;

    private int Store(Slate slate)
    {
        var target = targetFaction.GetValue(slate);
        var count = countRange.GetValue(slate).RandomInRange;
        var stars = ScalpRequestUtility.Stars(target, count, extraStarAboveCount.GetValue(slate));

        slate.Set(storeCountAs.GetValue(slate), count);
        slate.Set(storeRewardValueAs.GetValue(slate),
            count * rewardValuePerScalp.GetValue(slate) * Mathf.Pow(rewardFactorPerStar.GetValue(slate), stars));
        slate.Set(storeGoodwillAs.GetValue(slate), count * goodwillPerScalp.GetValue(slate));
        return stars;
    }

    protected override bool TestRunInt(Slate slate)
    {
        if (targetFaction.GetValue(slate) == null || countRange.GetValue(slate).min <= 0)
            return false;
        Store(slate);
        return true;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        if (targetFaction.GetValue(slate) == null)
            return;
        QuestGen.quest.challengeRating = Store(slate);
    }
}

public class QuestNode_ScalpRequest_Initiate : QuestNode
{
    public SlateRef<Settlement> settlement;
    public SlateRef<Faction> targetFaction;
    public SlateRef<int> requestedCount;
    public SlateRef<int> duration;

    protected override bool TestRunInt(Slate slate)
    {
        return settlement.GetValue(slate) != null
               && targetFaction.GetValue(slate) != null
               && requestedCount.GetValue(slate) > 0
               && duration.GetValue(slate) > 0;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        QuestGen.quest.AddPart(new QuestPart_InitiateScalpRequest
        {
            settlement = settlement.GetValue(slate),
            targetFaction = targetFaction.GetValue(slate),
            requestedCount = requestedCount.GetValue(slate),
            requestDuration = duration.GetValue(slate),
            keepAfterQuestEnds = false,
            inSignal = slate.Get<string>("inSignal")
        });
    }
}

// Reward of a hostile asker: goodwill only.
public class QuestNode_ScalpRequest_GoodwillReward : QuestNode
{
    [NoTranslate] public SlateRef<string> inSignal;
    public SlateRef<Faction> faction;
    public SlateRef<int> change;

    protected override bool TestRunInt(Slate slate)
    {
        return faction.GetValue(slate) != null && change.GetValue(slate) > 0;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        QuestGen.quest.AddPart(new QuestPart_FactionGoodwillChange
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)),
            faction = faction.GetValue(slate),
            change = change.GetValue(slate),
            historyEvent = HistoryEventDefOf.QuestGoodwillReward
        });
    }
}