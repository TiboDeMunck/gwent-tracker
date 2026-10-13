using GwentTracker.Model;
using GwentTracker.Services;
using W3SavegameEditor.Core.Savegame.Values;
using Xunit;

namespace GwentTracker.Tests;

public class GwentOpponentRewardResolverTests
{
    [Fact]
    public void QuestOpponentUsesItsFixedCardId()
    {
        var bloodyBaron = new GwentOpponent
        {
            PinId = 36,
            Category = "quest-fixed-card",
            CardId = "gwint_card_dijkstra"
        };

        var cardId = GwentOpponentRewardResolver.GetCardId(bloodyBaron, Array.Empty<Fact>());

        Assert.Equal("gwint_card_dijkstra", cardId);
        Assert.True(GwentCardInventory.TryGetCardIndex(cardId, out var cardIndex));
        Assert.Equal(109, cardIndex);
    }

    [Theory]
    [InlineData("merchant-random-pool")]
    [InlineData("random-reward")]
    public void RandomMerchantUsesTheCardRewardFactFromTheSameEvent(string category)
    {
        var merchant = new GwentOpponent
        {
            PinId = 35,
            NpcId = "fergus_graem",
            Category = category
        };
        var facts = new[]
        {
            Fact("merchant_card_fergus_graem_card_already_given", 334020.8125),
            Fact("Gwint_Card_Looted", 334020.8125),
            Fact("merchant_card_gwint_card_dol_infantry2_already_given", 334020.8125),
            Fact("merchant_card_gwint_card_frightener_already_given", 362253.625)
        };

        var cardId = GwentOpponentRewardResolver.GetCardId(merchant, facts);

        Assert.Equal("gwint_card_dol_infantry2", cardId);
        Assert.True(GwentCardInventory.TryGetCardIndex(cardId, out var cardIndex));
        Assert.Equal(331, cardIndex);
    }

    [Fact]
    public void AmbiguousOrUnpairedMerchantRewardsRemainBlank()
    {
        var merchant = new GwentOpponent
        {
            PinId = 35,
            NpcId = "fergus_graem",
            Category = "merchant-random-pool"
        };
        var ambiguous = new[]
        {
            Fact("merchant_card_fergus_graem_card_already_given", 100),
            Fact("merchant_card_gwint_card_frightener_already_given", 100),
            Fact("merchant_card_gwint_card_gargoyle_already_given", 100)
        };
        var unpaired = new[]
        {
            Fact("merchant_card_fergus_graem_card_already_given", 100),
            Fact("merchant_card_gwint_card_frightener_already_given", 101)
        };

        Assert.Null(GwentOpponentRewardResolver.GetCardId(merchant, ambiguous));
        Assert.Null(GwentOpponentRewardResolver.GetCardId(merchant, unpaired));
    }

    [Fact]
    public void ModOnlyCompletionFactDoesNotClaimACoincidentCardReward()
    {
        var merchant = new GwentOpponent
        {
            PinId = 35,
            Category = "merchant-random-pool",
            AlternateWinFacts = new List<string> { "gwm_won_35" }
        };
        var facts = new[]
        {
            Fact("gwm_won_35", 100),
            Fact("merchant_card_gwint_card_dol_infantry2_already_given", 100)
        };

        Assert.Null(GwentOpponentRewardResolver.GetCardId(merchant, facts));
    }

    private static Fact Fact(string id, double time)
    {
        return new Fact
        {
            Id = id,
            Entries = new[] { new FactEntry { Value = 1, Time = time, ExpiryTime = -2 } }
        };
    }
}
