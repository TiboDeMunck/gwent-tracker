using GwentTracker.Model;
using GwentTracker.Services;
using GwentTracker.ViewModels;

namespace GwentTracker.Tests;

public class OpponentOverrideStoreTests
{
    [Fact]
    public void ManualCheckAndUncheckPersistWithoutLeakingToAnotherProfile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GwentTracker.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "overrides.json");

        try
        {
            var writer = new OpponentOverrideStore(path);
            writer.Set("playthrough-a", "gwm-pin-31", true);
            writer.Set("playthrough-a", "gwm-pin-35", false);

            var reader = new OpponentOverrideStore(path);
            Assert.True(reader.Load("playthrough-a")["gwm-pin-31"]);
            Assert.False(reader.Load("playthrough-a")["gwm-pin-35"]);
            Assert.Empty(reader.Load("playthrough-b"));

            reader.Clear("playthrough-a");
            Assert.Empty(new OpponentOverrideStore(path).Load("playthrough-a"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ManualOverridesWinOverAutomaticCompletionUntilCleared()
    {
        var opponent = new GwentOpponentViewModel(new GwentOpponent { Id = "gwm-pin-35" });

        opponent.ApplyState(true, null);
        Assert.True(opponent.Played);

        opponent.Played = false;
        opponent.ApplyState(true, opponent.ManualOverride);
        Assert.False(opponent.Played);

        opponent.ClearManualOverride();
        Assert.True(opponent.Played);

        opponent.Played = true;
        opponent.ApplyState(false, opponent.ManualOverride);
        Assert.True(opponent.Played);
    }
}
