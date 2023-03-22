using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GW2_Addon_Manager.App.Configuration.Model
{
    public class InstalledAddonData
    {
        public string Name { get; }

        public string Version { get; }

        public bool Enabled { get; }

        [JsonConverter(typeof(ImmutableStackConverter))]
        public ImmutableStack<string> AddonFiles { get; }

        public InstalledAddonData(string name, string version, bool enabled, string[] addonFiles)
        {
            Name = name;
            Version = version;
            Enabled = enabled;
            AddonFiles = ImmutableStack.Create(addonFiles);
        }

        [JsonConstructor]
        public InstalledAddonData(string name, string version, bool enabled, ImmutableStack<string> addonFiles)
        {
            Name = name;
            Version = version;
            Enabled = enabled;
            AddonFiles = addonFiles;
        }

        public InstalledAddonData SetEnabled(bool enabled)
        {
            return new InstalledAddonData(Name, Version, enabled, AddonFiles);
        }

        public InstalledAddonData SetVersion(string version)
        {
            return new InstalledAddonData(Name, version, Enabled, AddonFiles);
        }

        public InstalledAddonData SetAddonFiles(string[] files)
        {
            return new InstalledAddonData(Name, Version, Enabled, files);
        }

        public class ImmutableStackConverter : JsonConverter<ImmutableStack<string>>
        {
            public override ImmutableStack<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var array = JsonSerializer.Deserialize<string[]>(ref reader, options);
                return ImmutableStack.Create(array);
            }

            public override void Write(Utf8JsonWriter writer, ImmutableStack<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToArray(), options);
            }
        }
    }
}