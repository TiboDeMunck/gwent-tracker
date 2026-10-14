using GwentTracker.Services;
using GwentTracker.ViewModels;

namespace GwentTracker.Tests;

public class GwentOpponentDatasetTests
{
    [Fact]
    public async Task EmbeddedDatasetContainsEveryUniqueSourcePin()
    {
        var dataset = await GwentOpponentRepository.LoadAsync();

        Assert.Equal(96, dataset.Opponents.Count);
        Assert.Equal(96, dataset.Opponents.Select(x => x.Id).Distinct().Count());
        Assert.Equal(96, dataset.Opponents.Select(x => x.PinId).Distinct().Count());
        Assert.All(dataset.Opponents, opponent => Assert.NotNull(opponent.Position));
        Assert.Equal(71, dataset.Opponents.Count(x => x.Category == "merchant-random-pool"));
        Assert.Equal(19, dataset.Opponents.Count(x => x.Category == "merchant-fixed-card"));
        Assert.Equal(6, dataset.Opponents.Count(x => x.Category == "quest-fixed-card"));
    }

    [Fact]
    public async Task KnownNpcMappingsAreNotDeduplicatedOrShifted()
    {
        var opponents = (await GwentOpponentRepository.LoadAsync()).Opponents;

        var shopkeeper = Assert.Single(opponents, x => x.NpcId == "snots_general_store_01");
        Assert.Equal(31, shopkeeper.PinId);
        Assert.Equal("Blackbough", shopkeeper.Location);

        var blacksmith = Assert.Single(opponents, x => x.NpcId == "snots_blacksmith_01");
        Assert.Equal(32, blacksmith.PinId);

        var fergus = Assert.Single(opponents, x => x.NpcId == "fergus_graem");
        Assert.Equal(35, fergus.PinId);
        Assert.Equal("Crow's Perch", fergus.Location);

        var marketplaceOpponents = opponents
            .Where(x => x.Location == "Novigrad — marketplace")
            .ToList();
        Assert.Equal(2, marketplaceOpponents.Count);
        Assert.Equal(new[] { 2, 13 }, marketplaceOpponents.Select(x => x.PinId).OrderBy(x => x));
        Assert.Equal(marketplaceOpponents.Count, marketplaceOpponents.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public async Task NullLocationsUseTheRequiredFallbackAndFixedCardsStayBlank()
    {
        var opponents = (await GwentOpponentRepository.LoadAsync()).Opponents;
        var unresolved = opponents.First(x => x.Location == null);
        Assert.Equal("Location unverified", new GwentOpponentViewModel(unresolved).Location);

        var fixedCard = opponents.First(x => x.Category == "quest-fixed-card");
        Assert.NotNull(fixedCard.CardId);
        Assert.Equal(string.Empty, new GwentOpponentViewModel(fixedCard).CardWon);
    }

    [Fact]
    public async Task PortraitReferencesMapEveryOpponent()
    {
        var opponents = (await GwentOpponentRepository.LoadAsync()).Opponents;

        var bloodyBaron = Assert.Single(opponents, x => x.Id == "gwm-pin-36");
        var baronViewModel = new GwentOpponentViewModel(bloodyBaron);
        Assert.Equal("Assets/OpponentPortraits/gwm-pin-36.png", bloodyBaron.Portrait.File);
        Assert.Equal("avares://gwenttracker/Assets/OpponentPortraits/gwm-pin-36.png", baronViewModel.PortraitResourceUri.AbsoluteUri);
        Assert.Equal("Bloody Baron", baronViewModel.PreviewDisplayName);
        Assert.Equal("Crow's Perch, Velen", baronViewModel.LocationAndRegion);

        var blackboughMerchant = Assert.Single(opponents, x => x.Id == "gwm-pin-31");
        var merchantViewModel = new GwentOpponentViewModel(blackboughMerchant);
        Assert.Equal("Assets/OpponentPortraits/gwm-pin-31.png", blackboughMerchant.Portrait.File);
        Assert.Equal("Shopkeeper", merchantViewModel.PreviewDisplayName);
        Assert.Equal("Blackbough, Velen", merchantViewModel.LocationAndRegion);

        var ottoBamber = Assert.Single(opponents, x => x.Id == "gwm-pin-25");
        var ottoBamberViewModel = new GwentOpponentViewModel(ottoBamber);
        Assert.Equal("Assets/OpponentPortraits/gwm-pin-25.png", ottoBamber.Portrait.File);
        Assert.Equal("avares://gwenttracker/Assets/OpponentPortraits/gwm-pin-25.png", ottoBamberViewModel.PortraitResourceUri.AbsoluteUri);

        Assert.Equal(96, opponents.Count(x => x.Portrait?.File != null));
    }
}
