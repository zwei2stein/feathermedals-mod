using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace TribalTrophies;

// Quest nodes shared by trophy request quests (scalps, dried tongues).
public abstract class QuestNode_TrophyRequest_GetAsker : QuestNode
{
    [NoTranslate] public SlateRef<string> storeAs;
    [NoTranslate] public SlateRef<string> storeFactionAs;
    [NoTranslate] public SlateRef<string> storeFactionLeaderAs;
    [NoTranslate] public SlateRef<string> storeAskerHostileAs;

    protected abstract bool QuestsEnabled { get; }

    protected abstract TrophyRequestComp RequestOf(Settlement settlement);

    private Settlement RandomAskerSettlement(PlanetTile originTile)
    {
        return Find.WorldObjects.SettlementBases.Where(settlement => IsGoodAsker(settlement, originTile)).RandomElementWithFallback();
    }

    private bool IsGoodAsker(Settlement settlement, PlanetTile originTile)
    {
        if (RequestOf(settlement) == null || settlement.Tile.LayerDef.isSpace)
            return false;
        if (!TrophyRequestUtility.CanAsk(settlement.Faction))
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
        if (!QuestsEnabled)
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

public class QuestNode_TrophyRequest_GetTarget : QuestNode
{
    public SlateRef<Faction> asker;
    [NoTranslate] public SlateRef<string> storeAs;

    protected override bool TestRunInt(Slate slate)
    {
        var askerFaction = asker.GetValue(slate);
        if (askerFaction == null || !TrophyRequestUtility.TryFindTarget(askerFaction, out var target))
            return false;
        slate.Set(storeAs.GetValue(slate), target);
        return true;
    }

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var askerFaction = asker.GetValue(slate);
        if (askerFaction == null || !TrophyRequestUtility.TryFindTarget(askerFaction, out var target))
            return;
        slate.Set(storeAs.GetValue(slate), target);
    }
}

public class QuestNode_TrophyRequest_GetRequest : QuestNode
{
    public SlateRef<Faction> targetFaction;
    public SlateRef<IntRange> countRange;
    public SlateRef<int> extraStarAboveCount;
    public SlateRef<float> rewardValuePerTrophy;
    public SlateRef<float> rewardFactorPerStar;
    public SlateRef<int> goodwillPerTrophy;
    public SlateRef<int> goodwillLossPerTrophy;

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
        var stars = TrophyRequestUtility.Stars(target, count, extraStarAboveCount.GetValue(slate));

        slate.Set(storeCountAs.GetValue(slate), count);
        slate.Set(storeRewardValueAs.GetValue(slate),
            count * rewardValuePerTrophy.GetValue(slate) * Mathf.Pow(rewardFactorPerStar.GetValue(slate), stars));
        slate.Set(storeGoodwillAs.GetValue(slate), count * goodwillPerTrophy.GetValue(slate));

        var goodwillLoss = count * goodwillLossPerTrophy.GetValue(slate);
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

public abstract class QuestNode_TrophyRequest_Initiate : QuestNode
{
    public SlateRef<Settlement> settlement;
    public SlateRef<Faction> targetFaction;
    public SlateRef<int> requestedCount;
    public SlateRef<int> duration;

    protected abstract QuestPart_InitiateTrophyRequest MakePart();

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
        var part = MakePart();
        part.settlement = settlement.GetValue(slate);
        part.targetFaction = targetFaction.GetValue(slate);
        part.requestedCount = requestedCount.GetValue(slate);
        part.requestDuration = duration.GetValue(slate);
        part.keepAfterQuestEnds = false;
        part.inSignal = slate.Get<string>("inSignal");
        QuestGen.quest.AddPart(part);
    }
}

// Target faction learns the player took the request: goodwill loss on accept.
public abstract class QuestNode_TrophyRequest_TargetGoodwillLoss : QuestNode
{
    public SlateRef<Faction> targetFaction;
    public SlateRef<int> goodwillLoss;

    protected abstract QuestPart_TrophyRequestTargetGoodwillLoss MakePart();

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var target = targetFaction.GetValue(slate);
        var loss = goodwillLoss.GetValue(slate);
        if (target == null || loss <= 0)
            return;

        var part = MakePart();
        part.inSignal = slate.Get<string>("inSignal");
        part.targetFaction = target;
        part.goodwillLoss = loss;
        QuestGen.quest.AddPart(part);
    }
}

// Reward of a hostile asker: goodwill only.
public class QuestNode_TrophyRequest_GoodwillReward : QuestNode
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
