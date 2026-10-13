using GwentTracker.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GwentTracker.Services
{
    public static class GwentOpponentDetector
    {
        private const string QuestFixedCardCategory = "quest-fixed-card";
        private const string NowhereInnNpcId = "novigrad_poor_district_nowhere_01";
        private const string NowhereInnAlternateFact = "merchant_card_novigrad_poor_district_nowhere_01 _card_already_given";

        public static bool IsCompleted(
            GwentOpponent opponent,
            IReadOnlyDictionary<string, long> factValues,
            IReadOnlySet<string> ownedCardIds)
        {
            if (opponent == null)
                return false;

            if (string.Equals(opponent.Category, QuestFixedCardCategory, StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(opponent.CardId) &&
                       ownedCardIds != null &&
                       ownedCardIds.Contains(opponent.CardId);
            }

            return IsConfirmedWin(opponent, factValues);
        }

        public static bool IsConfirmedWin(GwentOpponent opponent, IReadOnlyDictionary<string, long> factValues)
        {
            if (opponent == null || factValues == null)
                return false;

            return GetWinFactNames(opponent).Any(name =>
                factValues.TryGetValue(name, out var value) && value > 0);
        }

        public static IEnumerable<string> GetWinFactNames(GwentOpponent opponent)
        {
            if (!string.IsNullOrEmpty(opponent.NpcId))
            {
                yield return $"merchant_card_{opponent.NpcId}_card_already_given";
                yield return $"{opponent.NpcId}_gwent_given";
            }

            if (string.Equals(opponent.NpcId, NowhereInnNpcId, StringComparison.Ordinal))
                yield return NowhereInnAlternateFact;

            if (opponent.AlternateWinFacts != null)
            {
                foreach (var fact in opponent.AlternateWinFacts.Where(x => !string.IsNullOrEmpty(x)))
                    yield return fact;
            }

            yield return $"gwm_won_{opponent.PinId}";
        }
    }
}
