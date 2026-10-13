using GwentTracker.Services;
using Xunit;

namespace GwentTracker.Tests;

public class GwentCardInventoryTests
{
    [Theory]
    [InlineData(8, "gwint_card_vesemir")]
    [InlineData(9, "gwint_card_yennefer")]
    [InlineData(16, "gwint_card_avallach")]
    [InlineData(109, "gwint_card_dijkstra")]
    [InlineData(201, "gwint_card_menno")]
    [InlineData(203, "gwint_card_tibor")]
    public void OwnedQuestRewardsAreMappedFromSaveIndexesToInternalIds(int index, string expectedCardId)
    {
        var ownedCardIds = GwentCardInventory.GetOwnedCardIds(
            new[] { new KeyValuePair<int, int>(index, 1) });

        Assert.Contains(expectedCardId, ownedCardIds);
    }

    [Fact]
    public void ZeroCopyAndUnrelatedCardsAreNotReportedAsOwnedQuestRewards()
    {
        var ownedCardIds = GwentCardInventory.GetOwnedCardIds(new[]
        {
            new KeyValuePair<int, int>(109, 0),
            new KeyValuePair<int, int>(9999, 1)
        });

        Assert.Empty(ownedCardIds);
    }
}
