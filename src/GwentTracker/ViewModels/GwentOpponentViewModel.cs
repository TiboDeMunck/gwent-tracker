using GwentTracker.Model;
using ReactiveUI;
using System;

namespace GwentTracker.ViewModels
{
    public class GwentOpponentViewModel : ReactiveObject
    {
        private readonly Action<GwentOpponentViewModel, bool> _manualChange;
        private bool _detectedPlayed;
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
        public string CardWon => string.Empty;
        public bool? ManualOverride => _manualOverride;

        public bool Played
        {
            get => _manualOverride ?? _detectedPlayed;
            set
            {
                if (Played == value && _manualOverride.HasValue)
                    return;

                _manualOverride = value;
                this.RaisePropertyChanged();
                _manualChange?.Invoke(this, value);
            }
        }

        public void ApplyState(bool detectedPlayed, bool? manualOverride)
        {
            var previous = Played;
            _detectedPlayed = detectedPlayed;
            _manualOverride = manualOverride;
            if (previous != Played)
                this.RaisePropertyChanged(nameof(Played));
        }

        public void ClearManualOverride()
        {
            ApplyState(_detectedPlayed, null);
        }
    }
}
