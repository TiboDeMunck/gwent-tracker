using GwentTracker.Model;
using GwentTracker.Services;
using Xunit;

namespace GwentTracker.Tests;

public class GwentOpponentDetectorTests
{
    [Fact]
    public void PositiveVanillaMerchantFactConfirmsOnlyTheIntendedOpponent()
    {
        var intended = Opponent(35, "fergus_graem");
        var other = Opponent(31, "snots_general_store_01");
        var facts = new Dictionary<string, long>
        {
            ["merchant_card_fergus_graem_card_already_given"] = 1
        };

        Assert.True(GwentOpponentDetector.IsConfirmedWin(intended, facts));
        Assert.False(GwentOpponentDetector.IsConfirmedWin(other, facts));
        Assert.False(GwentOpponentDetector.IsConfirmedWin(intended, new Dictionary<string, long>()));
    }

    [Fact]
    public void AlternateTrailingSpaceAndModFactsAreSupported()
    {
        var nowhere = Opponent(12, "novigrad_poor_district_nowhere_01");
        var alternate = new Dictionary<string, long>
        {
            ["merchant_card_novigrad_poor_district_nowhere_01 _card_already_given"] = 1
        };
        Assert.True(GwentOpponentDetector.IsConfirmedWin(nowhere, alternate));

        var modFact = new Dictionary<string, long> { ["gwm_won_12"] = 2 };
        Assert.True(GwentOpponentDetector.IsConfirmedWin(nowhere, modFact));
    }

    [Fact]
    public void ZeroNegativeAndStaticCardOwnershipDoNotConfirmAWin()
    {
        var merchant = Opponent(35, "fergus_graem");
        var zero = new Dictionary<string, long>
        {
            ["merchant_card_fergus_graem_card_already_given"] = 0,
            ["fergus_graem_gwent_given"] = -1
        };
        Assert.False(GwentOpponentDetector.IsConfirmedWin(merchant, zero));

        var questMarker = Opponent(99, null);
        questMarker.CardId = "gwint_card_menno";
        Assert.False(GwentOpponentDetector.IsConfirmedWin(
            questMarker,
            new Dictionary<string, long> { ["gwint_card_menno"] = 1 }));
    }

    [Fact]
    public async Task BloodyBaronCompletesFromDijkstraCardWithoutAnNpcId()
    {
        var bloodyBaron = Assert.Single(
            (await GwentOpponentRepository.LoadAsync()).Opponents,
            opponent => opponent.PinId == 36);
        var ownedCards = GwentCardInventory.GetOwnedCardIds(
            new[] { new KeyValuePair<int, int>(109, 1) });

        var completed = GwentOpponentDetector.IsCompleted(
            bloodyBaron,
            new Dictionary<string, long>(),
            ownedCards);

        Assert.Null(bloodyBaron.NpcId);
        Assert.Equal("gwint_card_dijkstra", bloodyBaron.CardId);
        Assert.True(completed);
    }

    [Theory]
    [InlineData(14, 8, "gwint_card_vesemir")]
    [InlineData(23, 203, "gwint_card_tibor")]
    [InlineData(26, 9, "gwint_card_yennefer")]
    [InlineData(72, 16, "gwint_card_avallach")]
    [InlineData(99, 201, "gwint_card_menno")]
    public async Task OtherQuestOpponentsCompleteFromTheirUniqueRewardCard(
        int pinId,
        int cardIndex,
        string cardId)
    {
        var opponent = Assert.Single(
            (await GwentOpponentRepository.LoadAsync()).Opponents,
            candidate => candidate.PinId == pinId);
        var ownedCards = GwentCardInventory.GetOwnedCardIds(
            new[] { new KeyValuePair<int, int>(cardIndex, 1) });

        Assert.Equal(cardId, opponent.CardId);
        Assert.True(GwentOpponentDetector.IsCompleted(
            opponent,
            new Dictionary<string, long>(),
            ownedCards));
        Assert.False(GwentOpponentDetector.IsCompleted(
            opponent,
            new Dictionary<string, long>(),
            new HashSet<string>(StringComparer.Ordinal)));
    }

    [Fact]
    public void RandomMerchantUsesFactsAndNeverUsesCardOwnershipAsAFallback()
    {
        var merchant = Opponent(
            35,
            "fergus_graem",
            "merchant-random-pool",
            "gwint_card_dijkstra");
        var ownedCards = new HashSet<string>(StringComparer.Ordinal) { "gwint_card_dijkstra" };

        Assert.False(GwentOpponentDetector.IsCompleted(
            merchant,
            new Dictionary<string, long>(),
            ownedCards));
        Assert.True(GwentOpponentDetector.IsCompleted(
            merchant,
            new Dictionary<string, long>
            {
                ["merchant_card_fergus_graem_card_already_given"] = 1
            },
            ownedCards));
    }

    [Fact]
    public void QuestOpponentWithoutACardIdRemainsUnresolved()
    {
        var opponent = Opponent(100, "some_npc", "quest-fixed-card", null);

        Assert.False(GwentOpponentDetector.IsCompleted(
            opponent,
            new Dictionary<string, long>
            {
                ["merchant_card_some_npc_card_already_given"] = 1
            },
            new HashSet<string>(StringComparer.Ordinal) { "gwint_card_dijkstra" }));
    }

    private static GwentOpponent Opponent(
        int pinId,
        string npcId,
        string category = "merchant-random-pool",
        string cardId = null)
    {
        return new GwentOpponent
        {
            Id = $"gwm-pin-{pinId}",
            PinId = pinId,
            NpcId = npcId,
            Category = category,
            CardId = cardId
        };
    }
}
