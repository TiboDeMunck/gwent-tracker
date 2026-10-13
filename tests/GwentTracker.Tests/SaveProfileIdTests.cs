using GwentTracker.Services;
using W3SavegameEditor.Core.Savegame.Values;

namespace GwentTracker.Tests;

public class SaveProfileIdTests
{
    [Fact]
    public void RelatedSaveNamesShareAProfileWhileDifferentFactHistoryDoesNot()
    {
        var firstHistory = FactHistory(1000);
        var secondHistory = FactHistory(2000);
        var values = GameplayFactReader.GetValues(firstHistory);

        var first = SaveProfileId.Create(Path.Combine("saves", "AutoSave_a.sav"), firstHistory, values);
        var later = SaveProfileId.Create(Path.Combine("saves", "ManualSave_b.sav"), firstHistory, values);
        var unrelated = SaveProfileId.Create(Path.Combine("saves", "ManualSave_c.sav"), secondHistory, GameplayFactReader.GetValues(secondHistory));

        Assert.Equal(first, later);
        Assert.NotEqual(first, unrelated);
    }

    [Fact]
    public void NewGamePlusUsesASeparateProfile()
    {
        var history = FactHistory(1000).ToList();
        var normalValues = GameplayFactReader.GetValues(history);
        var normal = SaveProfileId.Create(Path.Combine("saves", "save-a.sav"), history, normalValues);

        history.Add(new Fact { Id = "NewGamePlus", Entries = new[] { new FactEntry { Value = 1 } } });
        var newGamePlus = SaveProfileId.Create(
            Path.Combine("saves", "save-b.sav"),
            history,
            GameplayFactReader.GetValues(history));

        Assert.NotEqual(normal, newGamePlus);
    }

    private static IEnumerable<Fact> FactHistory(double firstTime)
    {
        return Enumerable.Range(0, 64).Select(index => new Fact
        {
            Id = $"initial_fact_{index:D2}",
            Entries = new[]
            {
                new FactEntry { Value = 1, Time = firstTime + index, ExpiryTime = 0 }
            }
        });
    }
}
