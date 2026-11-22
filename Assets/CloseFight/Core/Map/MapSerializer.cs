using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloseFight.Core.Map
{
    /// <summary>Reads and writes map JSON files. Enums are stored as names so files stay readable.</summary>
    public static class MapSerializer
    {
        static readonly JsonSerializerSettings Settings = CreateSettings();

        static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        public static MapDefinition FromJson(string json)
        {
            var definition = JsonConvert.DeserializeObject<MapDefinition>(json, Settings);
            if (definition == null)
            {
                throw new FormatException("Map JSON is empty or invalid.");
            }

            return definition;
        }

        public static string ToJson(MapDefinition definition)
        {
            return JsonConvert.SerializeObject(definition, Settings);
        }
    }
}
