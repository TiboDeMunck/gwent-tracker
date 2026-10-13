using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using W3SavegameEditor.Core.Savegame.Values;

namespace GwentTracker.Services
{
    public static class SaveProfileId
    {
        private const int RequiredAnchorCount = 16;
        private const int MaximumAnchorCount = 64;

        public static string Create(
            string savePath,
            IEnumerable<Fact> facts,
            IReadOnlyDictionary<string, long> factValues)
        {
            var fullPath = Path.GetFullPath(savePath);
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var anchors = (facts ?? Enumerable.Empty<Fact>())
                .Where(fact => !string.IsNullOrEmpty(fact.Id))
                .SelectMany(fact => (fact.Entries ?? Array.Empty<FactEntry>())
                    .Where(entry => entry.Time > 0 && entry.ExpiryTime <= 0)
                    .Select(entry => new { fact.Id, entry.Value, entry.Time }))
                .OrderBy(entry => entry.Time)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal)
                .Take(MaximumAnchorCount)
                .ToList();

            var builder = new StringBuilder();
            builder.Append(directory.ToUpperInvariant());
            builder.Append('|');
            builder.Append(IsNewGamePlus(factValues) ? "NG+" : "NG");

            // Early permanent fact history follows a playthrough across save filenames.
            // If it cannot be decoded safely, the exact path is a conservative fallback.
            if (anchors.Count < RequiredAnchorCount)
            {
                builder.Append('|');
                builder.Append(fullPath.ToUpperInvariant());
            }
            else
            {
                foreach (var anchor in anchors)
                {
                    builder.Append('|');
                    builder.Append(anchor.Id);
                    builder.Append(':');
                    builder.Append(anchor.Value.ToString(CultureInfo.InvariantCulture));
                    builder.Append(':');
                    builder.Append(anchor.Time.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
        }

        private static bool IsNewGamePlus(IReadOnlyDictionary<string, long> factValues)
        {
            return factValues != null &&
                   factValues.TryGetValue("NewGamePlus", out var value) &&
                   value > 0;
        }
    }
}
