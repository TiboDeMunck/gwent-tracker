using System.Collections.Generic;

namespace GwentTracker.Model
{
    public class GwentOpponentDataset
    {
        public int SchemaVersion { get; set; }
        public GwentOpponentAttribution Attribution { get; set; }
        public GwentOpponentCoverage Coverage { get; set; }
        public List<GwentOpponent> Opponents { get; set; }
    }

    public class GwentOpponentAttribution
    {
        public string AdaptedFrom { get; set; }
        public string SourceRepository { get; set; }
        public string SourceDataFile { get; set; }
        public string SourceDataUrl { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public List<string> Adaptations { get; set; }
        public string Notice { get; set; }
    }

    public class GwentOpponentCoverage
    {
        public string Description { get; set; }
        public int TotalPins { get; set; }
        public int MerchantRandomPool { get; set; }
        public int MerchantFixedCard { get; set; }
        public int QuestFixedCard { get; set; }
        public bool WhiteOrchardIncluded { get; set; }
        public string LocationCaveat { get; set; }
    }

    public class GwentOpponent
    {
        public string Id { get; set; }
        public int PinId { get; set; }
        public string GameWorld { get; set; }
        public string Region { get; set; }
        public string Location { get; set; }
        public string LocationStatus { get; set; }
        public string DisplayName { get; set; }
        public string Profession { get; set; }
        public string NpcId { get; set; }
        public string CardId { get; set; }
        public string Category { get; set; }
        public GwentOpponentPosition Position { get; set; }
        public List<string> GoneFacts { get; set; }
        public string SourceNote { get; set; }
        public List<string> AlternateWinFacts { get; set; }
        public bool VanillaWinFactKnownMissing { get; set; }
        public GwentOpponentPortrait Portrait { get; set; }
    }

    public class GwentOpponentPortrait
    {
        public string File { get; set; }
    }

    public class GwentOpponentPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
