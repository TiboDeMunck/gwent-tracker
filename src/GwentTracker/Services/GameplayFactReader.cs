using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using W3SavegameEditor.Core.Savegame;
using W3SavegameEditor.Core.Savegame.Values;
using W3SavegameEditor.Core.Savegame.Variables;

namespace GwentTracker.Services
{
    public static class GameplayFactReader
    {
        private static readonly byte[] SbdfMagic = Encoding.ASCII.GetBytes("SBDF");
        private static readonly byte[] EbdfMagic = Encoding.ASCII.GetBytes("EBDF");

        public static IReadOnlyDictionary<string, long> GetValues(SavegameFile saveGame)
        {
            if (saveGame == null)
                throw new ArgumentNullException(nameof(saveGame));

            return GetValues(ReadFacts(saveGame));
        }

        public static Fact[] ReadFacts(SavegameFile saveGame)
        {
            if (saveGame == null)
                throw new ArgumentNullException(nameof(saveGame));

            var sbdf = Descendants(saveGame.Variables ?? Array.Empty<Variable>())
                .OfType<UnknownVariable>()
                .Select(variable => variable.Data)
                .FirstOrDefault(IsSbdfBlock);
            if (sbdf != null)
                return ReadSbdfFacts(sbdf);

            var flattened = new List<Variable>();
            foreach (var variable in saveGame.Variables ?? Array.Empty<Variable>())
            {
                flattened.Add(variable);
                if (variable is VariableSet set && set.Variables != null)
                    flattened.AddRange(set.Variables);
            }

            var stack = new Stack<Variable>(flattened.AsEnumerable().Reverse());
            var root = new VariableValueParser().Parse<SavegameRoot>(stack);
            return root?.Facts?.Items ?? Array.Empty<Fact>();
        }

        public static IReadOnlyDictionary<string, long> GetValues(IEnumerable<Fact> facts)
        {
            return (facts ?? Enumerable.Empty<Fact>())
                .Where(x => !string.IsNullOrEmpty(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.SelectMany(fact => fact.Entries ?? Array.Empty<FactEntry>())
                          .Sum(entry => (long)entry.Value),
                    StringComparer.Ordinal);
        }

        private static Fact[] ReadSbdfFacts(byte[] data)
        {
            using var stream = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            var magic = reader.ReadBytes(SbdfMagic.Length);
            if (!magic.SequenceEqual(SbdfMagic))
                throw new InvalidDataException("The gameplay fact block does not start with SBDF.");

            var count = reader.ReadInt32();
            if (count < 0 || count > (stream.Length - stream.Position) / 5)
                throw new InvalidDataException($"The gameplay fact block has an invalid record count ({count}).");

            var result = new Fact[count];
            for (var index = 0; index < count; index++)
            {
                var id = ReadEncodedString(reader);
                EnsureRemaining(stream, sizeof(ushort) * 2);
                var expiringCount = reader.ReadUInt16();
                var entryCount = reader.ReadUInt16();
                if (entryCount > (stream.Length - stream.Position) / (sizeof(short) + sizeof(float) * 2))
                    throw new InvalidDataException($"Gameplay fact '{id}' has an invalid entry count ({entryCount}).");

                var entries = new FactEntry[entryCount];
                for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
                {
                    EnsureRemaining(stream, sizeof(short) + sizeof(float) * 2);
                    entries[entryIndex] = new FactEntry
                    {
                        Value = reader.ReadInt16(),
                        Time = reader.ReadSingle(),
                        ExpiryTime = reader.ReadSingle()
                    };
                }

                result[index] = new Fact
                {
                    Id = id,
                    ExpiringCount = expiringCount,
                    Entries = entries
                };
            }

            var endMagic = reader.ReadBytes(EbdfMagic.Length);
            if (!endMagic.SequenceEqual(EbdfMagic) || stream.Position != stream.Length)
                throw new InvalidDataException("The gameplay fact block does not end with EBDF.");

            return result;
        }

        private static string ReadEncodedString(BinaryReader reader)
        {
            EnsureRemaining(reader.BaseStream, sizeof(byte));
            var headerPosition = reader.BaseStream.Position;
            var header = reader.ReadByte();
            if ((header & 0x80) == 0)
                throw new InvalidDataException($"Unsupported gameplay fact string header 0x{header:X2} at offset {headerPosition}.");

            // This optional marker is also handled by the save editor's String parser.
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var next = reader.ReadByte();
                if (next != 0x01)
                    reader.BaseStream.Position--;
            }

            var length = header & 0x7f;
            EnsureRemaining(reader.BaseStream, length);
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException("The gameplay fact identifier is truncated.");

            return Encoding.UTF8.GetString(bytes);
        }

        private static void EnsureRemaining(Stream stream, int requiredBytes)
        {
            if (stream.Length - stream.Position < requiredBytes)
                throw new EndOfStreamException("The gameplay fact block is truncated.");
        }

        private static IEnumerable<Variable> Descendants(IEnumerable<Variable> variables)
        {
            foreach (var variable in variables)
            {
                yield return variable;
                if (variable is VariableSet set && set.Variables != null)
                {
                    foreach (var child in Descendants(set.Variables))
                        yield return child;
                }
            }
        }

        private static bool IsSbdfBlock(byte[] data)
        {
            return data != null &&
                   data.Length >= SbdfMagic.Length + sizeof(int) &&
                   data.Take(SbdfMagic.Length).SequenceEqual(SbdfMagic);
        }
    }
}
