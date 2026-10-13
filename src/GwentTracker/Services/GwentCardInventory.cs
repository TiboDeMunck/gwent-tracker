using System;
using System.Collections.Generic;

namespace GwentTracker.Services
{
    public static class GwentCardInventory
    {
        private static readonly IReadOnlyDictionary<int, string> InternalIdsByIndex =
            new Dictionary<int, string>
            {
                [8] = "gwint_card_vesemir",
                [9] = "gwint_card_yennefer",
                [16] = "gwint_card_avallach",
                [109] = "gwint_card_dijkstra",
                [201] = "gwint_card_menno",
                [203] = "gwint_card_tibor"
            };

        public static IReadOnlySet<string> GetOwnedCardIds(IEnumerable<KeyValuePair<int, int>> cardCopies)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (cardCopies == null)
                return result;

            foreach (var card in cardCopies)
            {
                if (card.Value > 0 && InternalIdsByIndex.TryGetValue(card.Key, out var internalId))
                    result.Add(internalId);
            }

            return result;
        }
    }
}
