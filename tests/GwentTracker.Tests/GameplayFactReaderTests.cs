using GwentTracker.Services;
using System.Text;
using W3SavegameEditor.Core.Savegame;
using W3SavegameEditor.Core.Savegame.Values;
using W3SavegameEditor.Core.Savegame.Variables;

namespace GwentTracker.Tests;

public class GameplayFactReaderTests
{
    [Fact]
    public void PersistentFactsCanBeReadFromTheReferencedSaveVariableTree()
    {
        var facts = new BsVariable
        {
            Name = "facts",
            Variables = new Variable[]
            {
                Value("count", 1u),
                Value("id", "merchant_card_fergus_graem_card_already_given"),
                Value("expiringCount", 0u),
                Value("entryCount", 1u),
                Value("value", 1),
                Value("time", 1234d),
                Value("expiryTime", 0d)
            }
        };
        var save = new SavegameFile
        {
            Variables = new Variable[] { facts, new BsVariable { Name = "remainingRootData" } }
        };

        var result = GameplayFactReader.ReadFacts(save);

        var fact = Assert.Single(result);
        Assert.Equal("merchant_card_fergus_graem_card_already_given", fact.Id);
        Assert.Equal(1, Assert.Single(fact.Entries).Value);
    }

    [Fact]
    public void ValuesUseTheSameSumSemanticsAsFactsQuerySum()
    {
        var facts = new[]
        {
            new Fact
            {
                Id = "won",
                Entries = new[]
                {
                    new FactEntry { Value = 2 },
                    new FactEntry { Value = -1 }
                }
            },
            new Fact { Id = "not_won", Entries = new[] { new FactEntry { Value = 0 } } }
        };

        var values = GameplayFactReader.GetValues(facts);

        Assert.Equal(1, values["won"]);
        Assert.Equal(0, values["not_won"]);
    }

    [Fact]
    public void AggregateSbdfFactsCanBeReadFromNestedUnknownSaveData()
    {
        var save = new SavegameFile
        {
            Variables = new Variable[]
            {
                new BsVariable
                {
                    Name = "facts",
                    Variables = new Variable[]
                    {
                        new SsVariable
                        {
                            Variables = new Variable[]
                            {
                                new UnknownVariable
                                {
                                    Data = Sbdf(
                                        ("merchant_card_fergus_graem_card_already_given", (short)1),
                                        ("unrelated_fact", (short)0))
                                }
                            }
                        }
                    }
                }
            }
        };

        var values = GameplayFactReader.GetValues(save);
        var facts = GameplayFactReader.ReadFacts(save);

        Assert.Equal(1, values["merchant_card_fergus_graem_card_already_given"]);
        Assert.Equal(0, values["unrelated_fact"]);
        var merchantFact = Assert.Single(facts, fact => fact.Id == "merchant_card_fergus_graem_card_already_given");
        Assert.Equal(1234d, Assert.Single(merchantFact.Entries).Time);
        Assert.Equal(-2d, Assert.Single(merchantFact.Entries).ExpiryTime);
    }

    private static byte[] Sbdf(params (string Id, short Value)[] records)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("SBDF"));
        writer.Write(records.Length);
        foreach (var record in records)
        {
            var id = Encoding.UTF8.GetBytes(record.Id);
            Assert.InRange(id.Length, 0, 127);
            writer.Write((byte)(0x80 | id.Length));
            writer.Write(id);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write(record.Value);
            writer.Write(1234f);
            writer.Write(-2f);
        }
        writer.Write(Encoding.ASCII.GetBytes("EBDF"));

        return stream.ToArray();
    }

    private static VlVariable Value<T>(string name, T value)
    {
        return new VlVariable { Name = name, Value = VariableValue<T>.Create(value) };
    }
}
