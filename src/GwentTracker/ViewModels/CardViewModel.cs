using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GwentTracker.Localization;
using GwentTracker.Model;
using ReactiveUI;
using Serilog;

namespace GwentTracker.ViewModels
{
    public class CardViewModel : ReactiveObject
    {
        private readonly TextureInfo _textureInfo;
        private static readonly Lazy<IBitmap> FallbackTexture = new Lazy<IBitmap>(() => LoadEmbeddedBitmap("avares://GwentTracker/Assets/fallback.png"));
        private readonly Translate _t;

        public CardViewModel(TextureInfo textureInfo)
        {
            _textureInfo = textureInfo;
            _t = new Translate();
            LoadTexture = ReactiveCommand.CreateFromTask(LoadImage);
            LoadTexture.Subscribe(image =>
            {
                Texture = image;
            });
            LoadTexture.ThrownExceptions.Subscribe(e =>
            {
                Log.Error(e, "Error loading bitmap for {url}", LocalTextureUrl);
            });
        }

        private string LocalTextureUrl => string.Format(CultureInfo.InvariantCulture, _textureInfo.LocalPathFormat, Index);

        private Task<IBitmap> LoadImage(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(LoadEmbeddedBitmap(LocalTextureUrl) ?? FallbackTexture.Value);
        }

        private static IBitmap LoadEmbeddedBitmap(string url)
        {
            try
            {
                var uri = new Uri(url);
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                using var stream = assets.Open(uri);
                var bitmap = new Bitmap(stream);

                Log.Debug("Using embedded texture at {uri}", uri);
                return bitmap;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error loading bitmap from {url}", url);
                return null;
            }
        }
        
        public ReactiveCommand<Unit, IBitmap> LoadTexture { get; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string Flavor { get; set; }
        public int MaxCopies { get; set; }
    
        private int _copies;
        public int Copies
        {
            get => _copies;
            set => this.RaiseAndSetIfChanged(ref _copies, value);
        }
        private bool _obtained;
        public bool Obtained
        {
            get => _obtained;
            set => this.RaiseAndSetIfChanged(ref _obtained, value);
        }
        private bool _isVisible;
        public bool IsHidden
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        private IBitmap _texture;
        public IBitmap Texture
        {
            get => _texture;
            set => this.RaiseAndSetIfChanged(ref _texture, value);
        }
        public string Deck { get; set; }
        public string Type { get; set; }
        public string Location => string.Join(", ", Locations.Select(l => l.Type).Distinct());
        public string Region => string.Join(", ", Locations.Select(l => l.Type == _t["Base Deck"] ? l.Type : l.Region).Distinct());
        public IEnumerable<string> DetailedLocations => Locations.Select(DetailedLocation).Where(l => l != "");
        public CombatDetails Combat { get; set; }
        public Location[] Locations { get; set; }
        public CardSource Source { get; set; }
        
        private string DetailedLocation(Location l)
        {
            if (l.Type == _t["Base Deck"] || l.Region == _t["Random"])
                return "";
            
            var details = new[] {l.Npc, l.Area, l.Territory ?? l.Region}.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(", ", details);
        }
    }
}
