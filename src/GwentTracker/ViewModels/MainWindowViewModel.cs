using GwentTracker.Model;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using GwentTracker.Localization;
using GwentTracker.Services;
using W3SavegameEditor.Core.Savegame;
using W3SavegameEditor.Core.Savegame.Variables;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace GwentTracker.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly CultureInfo _cultureInfo;
        private readonly SourceList<CardViewModel> _cards;
        private readonly ReadOnlyObservableCollection<CardViewModel> _filteredCards;
        private readonly List<GwentOpponentViewModel> _allOpponents;
        private readonly OpponentOverrideStore _opponentOverrideStore;
        private readonly Translate _t;
        private bool _initialLoadComplete = false;
        private Dictionary<int, int> _summons;
        private IReadOnlyDictionary<string, long> _currentFactValues;
        private IReadOnlySet<string> _currentOwnedCardIds;
        private string _activeOpponentProfileId;

        public ReadOnlyObservableCollection<CardViewModel> Cards => _filteredCards;
        public ObservableCollection<GwentOpponentViewModel> Opponents { get; }
        public ObservableCollection<string> OpponentStatusFilters { get; }
        public ObservableCollection<MissableInfo> Messages { get; set; }
        public Subject<string> Notifications { get; set; }
        public TimeSpan NotificationDuration => TimeSpan.FromSeconds(5);
        public ReactiveCommand<string, SaveGameInfo> Load { get; set; }
        public ReactiveCommand<Unit, (IEnumerable<Card>, IEnumerable<MissableInfo>, IEnumerable<GwentOpponent>)> LoadCards { get; set; }
        public ReactiveCommand<Unit, Unit> AddFilter { get; set; }
        public ReactiveCommand<string, Unit> RemoveFilter { get; set; }
        public ReactiveCommand<Unit, Unit> ClearOpponentOverrides { get; set; }
        public ObservableCollection<string> Filters { get; set; }

        private ObservableAsPropertyHelper<bool> _loaderVisibility;
        public bool LoaderVisibility => _loaderVisibility.Value;

        private ObservableAsPropertyHelper<bool> _cardVisibility;
        public bool CardVisibility => _cardVisibility.Value;
        
        private CardViewModel _selectedCard;
        public CardViewModel SelectedCard
        {
            get => _selectedCard;
            set => this.RaiseAndSetIfChanged(ref _selectedCard, value);
        }

        private SaveGameInfo _model;

        private SaveGameInfo Model
        {
            get => _model;
            set => this.RaiseAndSetIfChanged(ref _model, value);
        }

        private string _filterString;
        public string FilterString
        {
            get => _filterString;
            set => this.RaiseAndSetIfChanged(ref _filterString, value);
        }

        private string _opponentFilterString;
        public string OpponentFilterString
        {
            get => _opponentFilterString;
            set
            {
                this.RaiseAndSetIfChanged(ref _opponentFilterString, value);
                RefreshOpponentFilter();
            }
        }

        private string _opponentStatusFilter;
        public string OpponentStatusFilter
        {
            get => _opponentStatusFilter;
            set
            {
                this.RaiseAndSetIfChanged(ref _opponentStatusFilter, value);
                RefreshOpponentFilter();
            }
        }

        private string _opponentSummary;
        public string OpponentSummary
        {
            get => _opponentSummary;
            private set => this.RaiseAndSetIfChanged(ref _opponentSummary, value);
        }

        private string _saveGamePath;
        public string SaveGamePath
        {
            get => _saveGamePath;
            set => this.RaiseAndSetIfChanged(ref _saveGamePath, value);
        }

        private CollectionProgress _baseGameProgress;
        public CollectionProgress BaseGameProgress
        {
            get => _baseGameProgress;
            set => this.RaiseAndSetIfChanged(ref _baseGameProgress, value);
        }
        
        private CollectionProgress _bAndWProgress;
        public CollectionProgress BandWProgress
        {
            get => _bAndWProgress;
            set => this.RaiseAndSetIfChanged(ref _bAndWProgress, value);
        }
        
        private CollectionProgress _dlcProgress;
        public CollectionProgress DlcProgress
        {
            get => _dlcProgress;
            set => this.RaiseAndSetIfChanged(ref _dlcProgress, value);
        }

        public MainWindowViewModel(
            string saveGamePath,
            TextureInfo textureInfo,
            IObservable<string> saveDirChanges,
            CultureInfo cultureInfo,
            OpponentOverrideStore opponentOverrideStore = null)
        {
            _t = new Translate();
            _cultureInfo = cultureInfo;
            _opponentOverrideStore = opponentOverrideStore ?? new OpponentOverrideStore();
            Activator = new ViewModelActivator();
            Filters = new ObservableCollection<string>();
            _cards = new SourceList<CardViewModel>();
            _allOpponents = new List<GwentOpponentViewModel>();
            _currentFactValues = new Dictionary<string, long>(StringComparer.Ordinal);
            _currentOwnedCardIds = new HashSet<string>(StringComparer.Ordinal);
            _summons = new Dictionary<int, int>();
            Opponents = new ObservableCollection<GwentOpponentViewModel>();
            OpponentStatusFilters = new ObservableCollection<string> { "All", "Played", "Not Played" };
            OpponentStatusFilter = OpponentStatusFilters[0];
            OpponentSummary = "Completed: 0/96";
            Messages = new ObservableCollection<MissableInfo>();
            Notifications = new Subject<string>();
            BaseGameProgress = new CollectionProgress();
            BandWProgress = new CollectionProgress();
            DlcProgress = new CollectionProgress();

            var filterChanged = Filters.ObserveCollectionChanges();
            filterChanged.Subscribe(_ =>
            {
                foreach (var card in _cards.Items)
                    card.IsHidden = ShouldFilterCard(card);
            });
            _cards.Connect()
                .AutoRefreshOnObservable(_ => filterChanged)
                .Filter(x => !x.IsHidden)
                .ObserveOn(AvaloniaScheduler.Instance)
                .Bind(out _filteredCards)
                .Subscribe();

            LoadCards = ReactiveCommand.CreateFromTask(LoadCardsFromFiles);
            LoadCards.ThrownExceptions.Subscribe(e =>
            {
                Log.Error(e, "Unable to load card data");
                Notifications.OnNext(_t["Unable to load card info"]);
            });
            LoadCards.Subscribe(items =>
            {
                var (cardInfo, missables, opponentInfo) = items;
                var cards = cardInfo as List<Card> ?? cardInfo.ToList();
                var mapped = cards.Select(c => new CardViewModel(textureInfo)
                {
                    Index = c.Index,
                    Copies = c.Copies,
                    MaxCopies = c.MaxCopies,
                    Name = c.Name,
                    Flavor = c.Flavor,
                    Obtained = c.Obtained,
                    Deck = c.Deck,
                    Type = c.Type,
                    Locations = c.Locations,
                    Source = c.Source
                });

                _cards.Clear();
                foreach (var card in mapped)
                    _cards.Add(card);

                Messages.Clear();
                Messages.AddRange(missables);

                _allOpponents.Clear();
                _allOpponents.AddRange(opponentInfo
                    .OrderBy(x => x.Region, StringComparer.CurrentCulture)
                    .ThenBy(x => x.Location ?? "Location unverified", StringComparer.CurrentCulture)
                    .ThenBy(x => x.DisplayName, StringComparer.CurrentCulture)
                    .Select(x => new GwentOpponentViewModel(x, OnOpponentManualChange)));
                RefreshOpponentFilter();
                UpdateOpponentSummary();
                SaveGamePath = saveGamePath;
                
                var requiredBase = cards.Where(c => c.Source == CardSource.BaseGame).Select(c => c.Name).Distinct().Count();
                var totalBase = cards.Where(c => c.Source == CardSource.BaseGame).Sum(c => c.MaxCopies);
                var requiredBandW = cards.Where(c => c.Source == CardSource.BloodAndWine).Sum(c => c.MaxCopies);
                var totalHoS = cards.Where(c => c.Source == CardSource.HeartsOfStone).Sum(c => c.MaxCopies);

                BaseGameProgress.Needed = requiredBase;
                BaseGameProgress.Total = totalBase;
                BandWProgress.Needed = requiredBandW;
                BandWProgress.Total = requiredBandW;
                DlcProgress.Total = BandWProgress.Total + totalHoS;
            });

            Load = ReactiveCommand.CreateFromTask<string, SaveGameInfo>(LoadSaveGame);
            Load.Subscribe(OnSaveGameLoaded);
            Load.ThrownExceptions.Subscribe(e =>
            {
                Log.Error(e, "Unable to load save game at {path}", SaveGamePath);
                Notifications.OnNext(_t["Unable to load save game"]);
            });
            _loaderVisibility = Load.IsExecuting
                .Select(x => x)
                .ToProperty(this, x => x.LoaderVisibility);

            this.WhenAnyValue(x => x.SaveGamePath)
                .Select(s => s?.Trim())
                .DistinctUntilChanged()
                .Where(s => !string.IsNullOrEmpty(s))
                .InvokeCommand(Load);

            this.WhenAnyValue(v => v.SelectedCard.LoadTexture)
                .SelectMany(x => x.Execute())
                .Subscribe();

            _cardVisibility = this.WhenAnyValue(x => x.SelectedCard)
                .Select(c => c != null)
                .ToProperty(this, x => x.CardVisibility);

            var canAddFilter = this.WhenAnyValue(vm => vm.FilterString, filter => !string.IsNullOrWhiteSpace(filter));
            AddFilter = ReactiveCommand.Create(OnAddFilter, canAddFilter);
            RemoveFilter = ReactiveCommand.Create<string>(OnRemoveFilter);
            ClearOpponentOverrides = ReactiveCommand.Create(OnClearOpponentOverrides);

            saveDirChanges?.Subscribe(OnSaveDirectoryChange);
        }

        private void OnSaveDirectoryChange(string path)
        {
            Log.Debug("Save directory change '{path}'", path);
            SaveGamePath = path;
        }
        
        private async Task<(IEnumerable<Card>, IEnumerable<MissableInfo>, IEnumerable<GwentOpponent>)> LoadCardsFromFiles()
        {
            var cards = new List<Card>();
            var missables = new List<MissableInfo>();
            var files = new[] { "monsters", "neutral", "nilfgaard", "northernrealms", "scoiatael", "skellige" };
            var deserializer = new DeserializerBuilder()
                                    .IgnoreUnmatchedProperties()
                                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                    .WithTypeConverter(new TranslateStringConverter())
                                    .Build();
            string filePath;
            foreach (var file in files)
            {
                filePath = Path.Combine("data", $"{file}.yml");
                try
                {
                    using (var reader = File.OpenText(filePath))
                    {
                        var contents = await reader.ReadToEndAsync();
                        var deserializedCards = deserializer.Deserialize<List<Card>>(contents);
                        
                        foreach (var card in deserializedCards.Where(c => c.AttachedTo != null))
                        {
                            card.MaxCopies = deserializedCards.Where(c => c.Index == card.AttachedTo.Value).Single().MaxCopies;
                            _summons.Add(card.AttachedTo.Value, card.Index);
                        }
                        
                        cards.AddRange(deserializedCards);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Couldn't load card data from {file}", filePath);
                    throw;
                }
            }

            filePath = Path.Combine("data", "missable.yml");
            try
            {
                using (var reader = File.OpenText(filePath))
                {
                    var contents = await reader.ReadToEndAsync();
                    missables.AddRange(deserializer.Deserialize<List<MissableInfo>>(contents));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Couldn't load missable card data from {file}", filePath);
                throw;
            }
            

            var opponents = await GwentOpponentRepository.LoadAsync();
            return (cards, missables, opponents.Opponents);
        }

        private async Task<SaveGameInfo> LoadSaveGame(string path)
        {
            var saveGame = await ReadStableSaveGame(path);
            var cardCollection = 
                ((BsVariable)saveGame.Variables.Single(v => v.Name == "CR4GwintManager"))
                .Variables
                .Skip(2)
                .TakeWhile(v => v.Name != "SBSelectedDeckIndex")
                .Where(v => v.Name == "cardIndex" || v.Name == "numCopies")
                .ToArray();
            var cards = new List<KeyValuePair<int, int>>(cardCollection.Length);
            for (var i = 0; i < cardCollection.Length; i += 2)
            {
                var index = ((VariableValue<int>)((VlVariable)cardCollection[i]).Value).Value;
                var copies = ((VariableValue<int>)((VlVariable)cardCollection[i + 1]).Value).Value;
                cards.Add(new KeyValuePair<int, int>(index, copies));
            }

            // TODO: figure out how quest info is stored and pull out the quests that cause missable cards

            var facts = GameplayFactReader.ReadFacts(saveGame);
            var factValues = GameplayFactReader.GetValues(facts);

            return new SaveGameInfo
            {
                CardCopies = cards,
                FactValues = factValues,
                OpponentProfileId = SaveProfileId.Create(path, facts, factValues)
            };
        }

        private static async Task<SavegameFile> ReadStableSaveGame(string path)
        {
            const int maximumAttempts = 8;
            Exception lastError = null;

            for (var attempt = 0; attempt < maximumAttempts; attempt++)
            {
                try
                {
                    var before = new FileInfo(path);
                    var length = before.Length;
                    var writeTime = before.LastWriteTimeUtc;
                    await Task.Delay(150);

                    before.Refresh();
                    if (before.Length != length || before.LastWriteTimeUtc != writeTime)
                        continue;

                    return await SavegameFile.ReadAsync(path);
                }
                catch (IOException e)
                {
                    lastError = e;
                    await Task.Delay(150);
                }
            }

            throw new IOException($"Save file '{path}' did not become readable and stable.", lastError);
        }

        private void OnSaveGameLoaded(SaveGameInfo info)
        {
            Model = info;
            ApplyOpponentSave(info);
            var newCards = new List<string>();
            foreach (var (index, copies) in info.CardCopies)
            {
                var card = _cards.Items.Where(c => c.Index == index).SingleOrDefault();

                if (card != null)
                {
                    CardViewModel summon = null;
                    
                    if (_summons.ContainsKey(index))
                        summon = _cards.Items.Where(c => c.Index == _summons[index]).SingleOrDefault();

                    if (_initialLoadComplete && (card.Obtained == false || card.Copies < copies))
                    {
                        newCards.Add(card.Name);
                        
                        if (summon != null)
                            newCards.Add(summon.Name);
                    }

                    card.Obtained = true;
                    card.Copies = copies;

                    if (summon != null)
                    {
                        summon.Obtained = true;
                        summon.Copies = copies;
                    }
                    
                    // Some save files report Francesca Findabair: the Beautiful as having multiple copies.
                    // Since this is a leader card and you can only effectively have one copy, manually
                    // fix it here.
                    if (card.Index == 3003 && card.Copies > 1)
                        card.Copies = 1;
                }
            }
            
            foreach (var missable in Messages.Where(m => m.State == MissableState.Active))
            {
                var obtained = _cards.Items.Where(c => missable.CardIds.Contains(c.Index)).All(c => c.Obtained);
                if (obtained)
                    missable.State = MissableState.Obtained;
                else
                {
                    // TODO: Check if quest status is completed/failed and set missable.state = Missed 
                }
            }

            if (newCards.Any())
            {
                var cardNames = string.Join(", ", newCards);
                Notifications.OnNext(_t[$"Obtained {cardNames}"]);
            }

            BaseGameProgress.Copies = GetCopies(CardSource.BaseGame);
            BaseGameProgress.Collected = GetCollected(CardSource.BaseGame);
            BandWProgress.Collected = GetCopies(CardSource.BloodAndWine);
            DlcProgress.Copies = GetCopies(CardSource.Dlc);
            DlcProgress.Collected =  GetCollected(CardSource.Dlc, false);
            _initialLoadComplete = true;
        }

        private void ApplyOpponentSave(SaveGameInfo info)
        {
            _activeOpponentProfileId = info.OpponentProfileId;
            _currentFactValues = info.FactValues ?? new Dictionary<string, long>(StringComparer.Ordinal);
            _currentOwnedCardIds = GwentCardInventory.GetOwnedCardIds(info.CardCopies);
            var overrides = _opponentOverrideStore.Load(_activeOpponentProfileId);

            foreach (var opponent in _allOpponents)
            {
                bool? manualOverride = overrides.TryGetValue(opponent.Id, out var value) ? value : null;
                var detected = GwentOpponentDetector.IsCompleted(
                    opponent.Opponent,
                    _currentFactValues,
                    _currentOwnedCardIds);
                opponent.ApplyState(detected, manualOverride);
            }

            RefreshOpponentFilter();
            UpdateOpponentSummary();
        }

        private void OnOpponentManualChange(GwentOpponentViewModel opponent, bool played)
        {
            if (!string.IsNullOrEmpty(_activeOpponentProfileId))
            {
                try
                {
                    _opponentOverrideStore.Set(_activeOpponentProfileId, opponent.Id, played);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unable to save the manual opponent override for {opponentId}", opponent.Id);
                    Notifications.OnNext(_t["Unable to save opponent checklist changes"]);
                }
            }

            RefreshOpponentFilter();
            UpdateOpponentSummary();
        }

        private void OnClearOpponentOverrides()
        {
            if (string.IsNullOrEmpty(_activeOpponentProfileId))
                return;

            try
            {
                _opponentOverrideStore.Clear(_activeOpponentProfileId);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unable to clear manual opponent overrides");
                Notifications.OnNext(_t["Unable to save opponent checklist changes"]);
                return;
            }
            foreach (var opponent in _allOpponents)
            {
                var detected = GwentOpponentDetector.IsCompleted(
                    opponent.Opponent,
                    _currentFactValues,
                    _currentOwnedCardIds);
                opponent.ApplyState(detected, null);
            }

            RefreshOpponentFilter();
            UpdateOpponentSummary();
        }

        private void RefreshOpponentFilter()
        {
            if (Opponents == null || _allOpponents == null)
                return;

            var compareInfo = _cultureInfo?.CompareInfo ?? CultureInfo.CurrentCulture.CompareInfo;
            var query = _allOpponents.Where(opponent =>
            {
                var statusMatches = OpponentStatusFilter == "Played"
                    ? opponent.Played
                    : OpponentStatusFilter == "Not Played"
                        ? !opponent.Played
                        : true;
                if (!statusMatches || string.IsNullOrWhiteSpace(OpponentFilterString))
                    return statusMatches;

                var search = OpponentFilterString.Trim();
                return compareInfo.IndexOf(opponent.Region, search, CompareOptions.IgnoreCase) >= 0 ||
                       compareInfo.IndexOf(opponent.Location, search, CompareOptions.IgnoreCase) >= 0 ||
                       compareInfo.IndexOf(opponent.NpcName, search, CompareOptions.IgnoreCase) >= 0 ||
                       compareInfo.IndexOf(opponent.Profession, search, CompareOptions.IgnoreCase) >= 0 ||
                       compareInfo.IndexOf(opponent.CardWon, search, CompareOptions.IgnoreCase) >= 0;
            });

            Opponents.Clear();
            foreach (var opponent in query)
                Opponents.Add(opponent);
        }

        private void UpdateOpponentSummary()
        {
            if (_allOpponents == null)
                return;

            var completed = _allOpponents.Count(x => x.Played);
            var regions = _allOpponents
                .GroupBy(x => x.Region)
                .OrderBy(x => x.Key, StringComparer.CurrentCulture)
                .Select(x => $"{x.Key}: {x.Count(y => y.Played)}/{x.Count()}");
            OpponentSummary = $"Completed: {completed}/{_allOpponents.Count} | {string.Join(" | ", regions)}";
        }

        private int GetCopies(CardSource source)
        {
            return _cards.Items
                         .Where(c => c.Obtained && (c.Source & source) > 0)
                         .Sum(c => c.Copies);
        }

        private int GetCollected(CardSource source, bool distinctOnly = true)
        {
            var cards = _cards.Items
                              .Where(c => c.Obtained && (c.Source & source) > 0)
                              .Select(c => c.Name);
            
            if (distinctOnly)
                cards = cards.Distinct();
                    
            return cards.Count();
        }
        
        private bool ShouldFilterCard(CardViewModel card)
        {
            var compareInfo = _cultureInfo.CompareInfo;

            return Filters.Any() &&
                   !Filters.All(f => compareInfo.IndexOf(card.Name, f, CompareOptions.IgnoreCase) >= 0 ||
                                     compareInfo.IndexOf(card.Deck, f, CompareOptions.IgnoreCase) >= 0 ||
                                     compareInfo.IndexOf(card.Type ?? "", f, CompareOptions.IgnoreCase) >= 0 ||
                                     compareInfo.IndexOf(card.Location, f, CompareOptions.IgnoreCase) >= 0 ||
                                     compareInfo.IndexOf(card.Region, f, CompareOptions.IgnoreCase) >= 0 ||
                                     compareInfo.IndexOf(_t[card.Source.GetDescription()], f, CompareOptions.IgnoreCase) >= 0);
        }

        private void OnAddFilter()
        {
            Filters.Add(FilterString);
            FilterString = null;
        }

        private void OnRemoveFilter(string filter)
        {
            Filters.Remove(filter);
        }

        public ViewModelActivator Activator { get; }
    }
}
