using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GwentTracker.Services
{
    public static class OpponentPortraitLoader
    {
        private const string PortraitDirectory = "Assets/OpponentPortraits/";
        private static readonly ConcurrentDictionary<string, Lazy<IBitmap>> Cache = new();

        public static Uri ResolveResourceUri(string portraitFile)
        {
            if (string.IsNullOrWhiteSpace(portraitFile))
                return null;

            var relativePath = portraitFile.Replace('\\', '/').TrimStart('/');
            if (!relativePath.StartsWith(PortraitDirectory, StringComparison.OrdinalIgnoreCase) ||
                relativePath.Contains("../", StringComparison.Ordinal))
            {
                Log.Warning("Ignoring opponent portrait outside the packaged portrait directory: {path}", portraitFile);
                return null;
            }

            return new Uri($"avares://GwentTracker/{relativePath}", UriKind.Absolute);
        }

        public static IBitmap Load(string portraitFile)
        {
            var uri = ResolveResourceUri(portraitFile);
            if (uri == null)
                return null;

            return Cache.GetOrAdd(
                uri.AbsoluteUri,
                key => new Lazy<IBitmap>(
                    () => LoadResource(new Uri(key, UriKind.Absolute)),
                    LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        private static IBitmap LoadResource(Uri uri)
        {
            try
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                if (assets == null)
                {
                    Log.Warning("Unable to load opponent portrait because the Avalonia asset loader is unavailable: {uri}", uri);
                    return null;
                }

                using var stream = assets.Open(uri);
                var bitmap = new Bitmap(stream);
                Log.Debug("Using packaged opponent portrait at {uri}", uri);
                return bitmap;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Unable to load opponent portrait at {uri}", uri);
                return null;
            }
        }
    }
}
