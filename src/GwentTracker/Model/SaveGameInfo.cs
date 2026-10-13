using System.Collections.Generic;

namespace GwentTracker.Model
{
    public class SaveGameInfo
    {
        public IEnumerable<KeyValuePair<int, int>> CardCopies { get; set; }
        public IReadOnlyCollection<W3SavegameEditor.Core.Savegame.Values.Fact> Facts { get; set; }
        public IReadOnlyDictionary<string, long> FactValues { get; set; }
        public string OpponentProfileId { get; set; }
    }
}
