using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GwentTracker.Services
{
    public class OpponentOverrideStore
    {
        private readonly object _gate = new object();
        private readonly string _filePath;

        public OpponentOverrideStore(string filePath = null)
        {
            _filePath = filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GwentTracker",
                "opponent-overrides.json");
        }

        public IReadOnlyDictionary<string, bool> Load(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
                return new Dictionary<string, bool>(StringComparer.Ordinal);

            lock (_gate)
            {
                var data = Read();
                if (data.Profiles.TryGetValue(profileId, out var overrides))
                    return new Dictionary<string, bool>(overrides, StringComparer.Ordinal);
                return new Dictionary<string, bool>(StringComparer.Ordinal);
            }
        }

        public void Set(string profileId, string opponentId, bool played)
        {
            if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(opponentId))
                return;

            lock (_gate)
            {
                var data = Read();
                if (!data.Profiles.TryGetValue(profileId, out var overrides))
                {
                    overrides = new Dictionary<string, bool>(StringComparer.Ordinal);
                    data.Profiles[profileId] = overrides;
                }

                overrides[opponentId] = played;
                Write(data);
            }
        }

        public void Clear(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
                return;

            lock (_gate)
            {
                var data = Read();
                if (data.Profiles.Remove(profileId))
                    Write(data);
            }
        }

        private OpponentOverrideFile Read()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new OpponentOverrideFile();

                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize(json, OpponentOverrideJsonContext.Default.OpponentOverrideFile)
                           ?? new OpponentOverrideFile();
                data.Profiles ??= new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
                return data;
            }
            catch (Exception)
            {
                // A damaged local preference file must not prevent save loading.
                return new OpponentOverrideFile();
            }
        }

        private void Write(OpponentOverrideFile data)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(data, OpponentOverrideJsonContext.Default.OpponentOverrideFile);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, true);
        }
    }

    public class OpponentOverrideFile
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, Dictionary<string, bool>> Profiles { get; set; } =
            new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
    }

    [JsonSerializable(typeof(OpponentOverrideFile))]
    public partial class OpponentOverrideJsonContext : JsonSerializerContext
    {
    }
}
