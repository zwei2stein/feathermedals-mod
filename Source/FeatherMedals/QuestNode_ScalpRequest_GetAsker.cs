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
        return Find.WorldGrid.ApproxDistanceInTiles(originTile, settlement.Tile) < TribalTrophiesMod.Settings.ScalpRequestMaxDistance;
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
        if (!TribalTrophiesMod.Settings.ScalpRequestQuests)
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
    public SlateRef<int> goodwillLossPerScalp;

    [NoTranslate] public SlateRef<string> storeCountAs;
    [NoTranslate] public SlateRef<string> storeRewardValueAs;
    [NoTranslate] public SlateRef<string> storeGoodwillAs;
    [NoTranslate] public SlateRef<string> storeGoodwillLossAs;
    [NoTranslate] public SlateRef<string> storeExpectedGoodwillLossAs;
    [NoTranslate] public SlateRef<string> storeTargetLosesGoodwillAs;

    private int Store(Slate slate)
    {
        var target = targetFaction.GetValue(slate);
        var count = countRange.GetValue(slate).RandomInRange;
        var stars = ScalpRequestUtility.Stars(target, count, extraStarAboveCount.GetValue(slate));

        slate.Set(storeCountAs.GetValue(slate), count);
        slate.Set(storeRewardValueAs.GetValue(slate),
            count * rewardValuePerScalp.GetValue(slate) * Mathf.Pow(rewardFactorPerStar.GetValue(slate), stars));
        slate.Set(storeGoodwillAs.GetValue(slate), count * goodwillPerScalp.GetValue(slate));
        
        var goodwillLoss = count * goodwillLossPerScalp.GetValue(slate);
        slate.Set(storeGoodwillLossAs.GetValue(slate), goodwillLoss);

        var losesGoodwill = goodwillLoss > 0 && Faction.OfPlayer.CanChangeGoodwillFor(target, -goodwillLoss);
        var expectedLoss = losesGoodwill ? ExpectedGoodwillLoss(target, goodwillLoss) : 0;
        slate.Set(storeExpectedGoodwillLossAs.GetValue(slate), expectedLoss);
        slate.Set(storeTargetLosesGoodwillAs.GetValue(slate), expectedLoss > 0);
        return stars;
    }
    
    private static int ExpectedGoodwillLoss(Faction target, int goodwillLoss)
    {
        var player = Faction.OfPlayer;
        var adjusted = -player.CalculateAdjustedGoodwillChange(target, -goodwillLoss);
        return Mathf.Min(adjusted, player.BaseGoodwillWith(target) + 100);
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

// Target faction learns the player took the request: goodwill loss on accept.
public class QuestNode_ScalpRequest_TargetGoodwillLoss : QuestNode
{
    public SlateRef<Faction> targetFaction;
    public SlateRef<int> goodwillLoss;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var target = targetFaction.GetValue(slate);
        var loss = goodwillLoss.GetValue(slate);
        if (target == null || loss <= 0)
            return;

        QuestGen.quest.AddPart(new QuestPart_ScalpRequestTargetGoodwillLoss
        {
            inSignal = slate.Get<string>("inSignal"),
            targetFaction = target,
            goodwillLoss = loss
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
        var givingFaction = faction.GetValue(slate);
        var amount = change.GetValue(slate);
        var signal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate));

        // same part vanilla Reward_Goodwill generates, wired to our fulfilled signal
        var goodwillPart = new QuestPart_FactionGoodwillChange
        {
            inSignal = signal,
            faction = givingFaction,
            change = amount,
            historyEvent = HistoryEventDefOf.QuestGoodwillReward
        };
        QuestGen.quest.AddPart(goodwillPart);

        // single choice
        var choice = new QuestPart_Choice.Choice();
        choice.rewards.Add(new Reward_Goodwill
        {
            faction = givingFaction,
            amount = amount
        });
        choice.questParts.Add(goodwillPart);

        var choicePart = new QuestPart_Choice
        {
            inSignalChoiceUsed = signal
        };
        choicePart.choices.Add(choice);
        QuestGen.quest.AddPart(choicePart);
    }
}