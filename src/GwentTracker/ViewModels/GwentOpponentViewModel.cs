using GwentTracker.Model;
using GwentTracker.Services;
using ReactiveUI;
using System;
using Avalonia.Media.Imaging;

namespace GwentTracker.ViewModels
{
    public class GwentOpponentViewModel : ReactiveObject
    {
        private readonly Action<GwentOpponentViewModel, bool> _manualChange;
        private bool _detectedPlayed;
        private string _detectedCardWon;
        private bool? _manualOverride;
        private readonly Lazy<IBitmap> _portraitImage;

        public GwentOpponentViewModel(
            GwentOpponent opponent,
            Action<GwentOpponentViewModel, bool> manualChange = null)
        {
            Opponent = opponent;
            _manualChange = manualChange;
            _portraitImage = new Lazy<IBitmap>(() => OpponentPortraitLoader.Load(Opponent.Portrait?.File));
        }

        public GwentOpponent Opponent { get; }
        public string Id => Opponent.Id;
        public string Region => Opponent.Region;
        public string Location => Opponent.Location ?? "Location unverified";
        public string DisplayName => Opponent.DisplayName;
        public string NpcName => DisplayName;
        public string Profession => Opponent.Profession;
        public string PreviewDisplayName
        {
            get
            {
                var locationSuffix = $" — {Opponent.Location}";
                return !string.IsNullOrWhiteSpace(Opponent.Location) &&
                       DisplayName.EndsWith(locationSuffix, StringComparison.CurrentCulture)
                    ? DisplayName.Substring(0, DisplayName.Length - locationSuffix.Length)
                    : DisplayName;
            }
        }
        public string PreviewProfession => string.Equals(PreviewDisplayName, Profession, StringComparison.CurrentCultureIgnoreCase)
            ? string.Empty
            : Profession;
        public string LocationAndRegion => string.Join(", ", new[] { Location, Region });
        public Uri PortraitResourceUri => OpponentPortraitLoader.ResolveResourceUri(Opponent.Portrait?.File);
        public IBitmap PortraitImage => _portraitImage.Value;
        public bool IsPortraitMissing => PortraitImage == null;
        public string CardWon => Played ? _detectedCardWon ?? string.Empty : string.Empty;
        public bool HasCardWon => !string.IsNullOrWhiteSpace(CardWon);
        public bool? ManualOverride => _manualOverride;

        public bool Played
        {
            get => _manualOverride ?? _detectedPlayed;
            set
            {
                if (Played == value && _manualOverride.HasValue)
                    return;

                var previousCardWon = CardWon;
                _manualOverride = value;
                this.RaisePropertyChanged();
                if (!string.Equals(previousCardWon, CardWon, StringComparison.Ordinal))
                {
                    this.RaisePropertyChanged(nameof(CardWon));
                    this.RaisePropertyChanged(nameof(HasCardWon));
                }
                _manualChange?.Invoke(this, value);
            }
        }

        public void ApplyState(bool detectedPlayed, bool? manualOverride, string detectedCardWon = null)
        {
            var previous = Played;
            var previousCardWon = CardWon;
            _detectedPlayed = detectedPlayed;
            _manualOverride = manualOverride;
            _detectedCardWon = detectedPlayed ? detectedCardWon : null;
            if (previous != Played)
                this.RaisePropertyChanged(nameof(Played));
            if (!string.Equals(previousCardWon, CardWon, StringComparison.Ordinal))
            {
                this.RaisePropertyChanged(nameof(CardWon));
                this.RaisePropertyChanged(nameof(HasCardWon));
            }
        }

        public void ClearManualOverride()
        {
            ApplyState(_detectedPlayed, null, _detectedCardWon);
        }
    }
}
