using GwentTracker.Model;
using ReactiveUI;
using System;

namespace GwentTracker.ViewModels
{
    public class GwentOpponentViewModel : ReactiveObject
    {
        private readonly Action<GwentOpponentViewModel, bool> _manualChange;
        private bool _detectedPlayed;
        private string _detectedCardWon;
        private bool? _manualOverride;

        public GwentOpponentViewModel(
            GwentOpponent opponent,
            Action<GwentOpponentViewModel, bool> manualChange = null)
        {
            Opponent = opponent;
            _manualChange = manualChange;
        }

        public GwentOpponent Opponent { get; }
        public string Id => Opponent.Id;
        public string Region => Opponent.Region;
        public string Location => Opponent.Location ?? "Location unverified";
        public string NpcName => Opponent.DisplayName;
        public string Profession => Opponent.Profession;
        public string CardWon => Played ? _detectedCardWon ?? string.Empty : string.Empty;
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
                    this.RaisePropertyChanged(nameof(CardWon));
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
                this.RaisePropertyChanged(nameof(CardWon));
        }

        public void ClearManualOverride()
        {
            ApplyState(_detectedPlayed, null, _detectedCardWon);
        }
    }
}
