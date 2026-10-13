using GwentTracker.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using W3SavegameEditor.Core.Savegame.Values;

namespace GwentTracker.Services
{
    public static class GwentOpponentRewardResolver
    {
        private const string QuestFixedCardCategory = "quest-fixed-card";
        private const string MerchantRandomPoolCategory = "merchant-random-pool";
        private const string RandomRewardCategory = "random-reward";
        private const string RewardFactPrefix = "merchant_card_";
        private const string RewardFactSuffix = "_already_given";
        private const string CardIdPrefix = "gwint_card_";

        public static string GetCardId(GwentOpponent opponent, IEnumerable<Fact> facts)
        {
            if (opponent == null)
                return null;

            if (string.Equals(opponent.Category, QuestFixedCardCategory, StringComparison.Ordinal))
                return string.IsNullOrWhiteSpace(opponent.CardId) ? null : opponent.CardId;

            if (!string.Equals(opponent.Category, MerchantRandomPoolCategory, StringComparison.Ordinal) &&
                !string.Equals(opponent.Category, RandomRewardCategory, StringComparison.Ordinal))
            {
                return null;
            }

            return GetRandomRewardCardId(opponent, facts ?? Enumerable.Empty<Fact>());
        }

        private static string GetRandomRewardCardId(GwentOpponent opponent, IEnumerable<Fact> facts)
        {
            var factsList = facts.Where(fact => fact != null && !string.IsNullOrEmpty(fact.Id)).ToList();
            // The Gwent Players Map marker confirms completion but does not prove which
            // vanilla reward event awarded a card, so it is intentionally excluded here.
            var winFactNames = GwentOpponentDetector.GetWinFactNames(opponent)
                .Where(name => !name.StartsWith("gwm_won_", StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
            var positiveWinTimes = factsList
                .Where(fact => winFactNames.Contains(fact.Id))
                .SelectMany(fact => fact.Entries ?? Array.Empty<FactEntry>())
                .Where(entry => entry.Value > 0 && entry.Time > 0)
                .Select(entry => entry.Time)
                .ToHashSet();

            if (positiveWinTimes.Count == 0)
                return null;

            var candidates = factsList
                .Select(fact => new
                {
                    CardId = ExtractCardId(fact.Id),
                    Entries = fact.Entries ?? Array.Empty<FactEntry>()
                })
                .Where(fact => fact.CardId != null &&
                               fact.Entries.Any(entry => entry.Value > 0 && positiveWinTimes.Contains(entry.Time)))
                .Select(fact => fact.CardId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return candidates.Count == 1 ? candidates[0] : null;
        }

        private static string ExtractCardId(string factId)
        {
            if (!factId.StartsWith(RewardFactPrefix + CardIdPrefix, StringComparison.Ordinal) ||
                !factId.EndsWith(RewardFactSuffix, StringComparison.Ordinal))
            {
                return null;
            }

            return factId.Substring(
                RewardFactPrefix.Length,
                factId.Length - RewardFactPrefix.Length - RewardFactSuffix.Length);
        }
    }
}
