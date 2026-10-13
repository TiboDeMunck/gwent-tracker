using System;
using System.Collections.Generic;
using System.Linq;

namespace GwentTracker.Services
{
    public static class GwentCardInventory
    {
        private static readonly IReadOnlyDictionary<string, int> IndexesByInternalId =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["gwint_card_frost"] = 3,
                ["gwint_card_fog"] = 4,
                ["gwint_card_rain"] = 5,
                ["gwint_card_clear_sky"] = 6,
                ["gwint_card_vesemir"] = 8,
                ["gwint_card_yennefer"] = 9,
                ["gwint_card_emiel"] = 14,
                ["gwint_card_villen"] = 15,
                ["gwint_card_avallach"] = 16,
                ["gwint_card_philippa"] = 103,
                ["gwint_card_dijkstra"] = 109,
                ["gwint_card_siege_tower"] = 170,
                ["gwint_card_menno"] = 201,
                ["gwint_card_tibor"] = 203,
                ["gwint_card_assire"] = 206,
                ["gwint_card_renuald"] = 211,
                ["gwint_card_rotten"] = 212,
                ["gwint_card_shilard"] = 213,
                ["gwint_card_stefan"] = 214,
                ["gwint_card_vanhemar"] = 217,
                ["gwint_card_vattier"] = 218,
                ["gwint_card_vreemde"] = 219,
                ["gwint_card_cahir"] = 220,
                ["gwint_card_dennis"] = 305,
                ["gwint_card_ida"] = 307,
                ["gwint_card_filavandrel"] = 308,
                ["gwint_card_toruviel"] = 310,
                ["gwint_card_riordain"] = 311,
                ["gwint_card_ciaran"] = 312,
                ["gwint_card_havekar_support2"] = 321,
                ["gwint_card_dol_infantry2"] = 331,
                ["gwint_card_dol_infantry3"] = 332,
                ["gwint_card_dol_dwarf2"] = 336,
                ["gwint_card_dol_dwarf3"] = 337,
                ["gwint_card_elf_skirmisher2"] = 351,
                ["gwint_card_elf_skirmisher3"] = 352,
                ["gwint_card_vrihedd_cadet"] = 355,
                ["gwint_card_havekar_nurse"] = 365,
                ["gwint_card_kayran"] = 401,
                ["gwint_card_imlerith"] = 402,
                ["gwint_card_forktail"] = 405,
                ["gwint_card_plague_maiden"] = 413,
                ["gwint_card_griffin"] = 415,
                ["gwint_card_frightener"] = 423,
                ["gwint_card_endrega"] = 427,
                ["gwint_card_cockatrice"] = 433,
                ["gwint_card_gargoyle"] = 435,
                ["gwint_card_celaeno_harpy"] = 437,
                ["gwint_card_grave_hag"] = 440,
                ["gwint_card_fire_elemental"] = 443,
                ["gwint_card_wyvern"] = 447,
                ["gwint_card_arachas_behemoth"] = 450,
                ["gwint_card_nekker3"] = 457,
                ["gwint_card_garkain"] = 462,
                ["gwint_card_ghoul2"] = 471,
                ["gwint_card_ghoul3"] = 472,
                ["gwint_card_crone_brewess"] = 475
            };

        private static readonly IReadOnlyDictionary<int, string> InternalIdsByIndex =
            IndexesByInternalId.ToDictionary(pair => pair.Value, pair => pair.Key);

        public static bool TryGetCardIndex(string internalId, out int index)
        {
            if (string.IsNullOrWhiteSpace(internalId))
            {
                index = default;
                return false;
            }

            return IndexesByInternalId.TryGetValue(internalId, out index);
        }

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
