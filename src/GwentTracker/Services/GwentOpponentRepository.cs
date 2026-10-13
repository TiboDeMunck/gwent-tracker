using GwentTracker.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GwentTracker.Services
{
    public static class GwentOpponentRepository
    {
        public const string ResourceName = "GwentTracker.data.gwent_opponents.yml";

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(GwentOpponentDataset))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(GwentOpponentAttribution))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(GwentOpponentCoverage))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(GwentOpponent))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(GwentOpponentPosition))]
        public static async Task<GwentOpponentDataset> LoadAsync()
        {
            var assembly = typeof(GwentOpponentRepository).Assembly;
            using var stream = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded opponent data '{ResourceName}' was not found.");
            using var reader = new StreamReader(stream);
            var yaml = await reader.ReadToEndAsync();
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var dataset = deserializer.Deserialize<GwentOpponentDataset>(yaml);

            Validate(dataset);
            return dataset;
        }

        public static void Validate(GwentOpponentDataset dataset)
        {
            if (dataset?.Opponents == null)
                throw new InvalidDataException("The opponent dataset does not contain an opponents list.");

            if (dataset.Opponents.Count != 96 || dataset.Coverage?.TotalPins != 96)
                throw new InvalidDataException("The opponent dataset must contain all 96 source pins.");

            EnsureUnique(dataset.Opponents.Select(x => x.Id), "id");
            EnsureUnique(dataset.Opponents.Select(x => x.PinId), "pinId");

            var expectedCategories = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["merchant-random-pool"] = 71,
                ["merchant-fixed-card"] = 19,
                ["quest-fixed-card"] = 6
            };
            var actualCategories = dataset.Opponents
                .GroupBy(x => x.Category, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
            if (actualCategories.Count != expectedCategories.Count ||
                expectedCategories.Any(x => !actualCategories.TryGetValue(x.Key, out var count) || count != x.Value))
            {
                throw new InvalidDataException("The opponent dataset category totals do not match its documented coverage.");
            }

            if (dataset.Opponents.Any(x => string.IsNullOrWhiteSpace(x.Id) ||
                                           string.IsNullOrWhiteSpace(x.Region) ||
                                           string.IsNullOrWhiteSpace(x.DisplayName) ||
                                           string.IsNullOrWhiteSpace(x.Profession) ||
                                           x.Position == null))
            {
                throw new InvalidDataException("One or more opponent records is missing required data.");
            }
        }

        private static void EnsureUnique<T>(IEnumerable<T> values, string fieldName)
        {
            if (values.GroupBy(x => x).Any(x => x.Count() > 1))
                throw new InvalidDataException($"The opponent dataset contains duplicate {fieldName} values.");
        }
    }
}
