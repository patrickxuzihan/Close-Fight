using System.Collections.Generic;
using Newtonsoft.Json;

namespace CloseFight.Core.Text
{
    /// <summary>
    /// All player-facing text lives in one JSON table, never in code (D46):
    /// { "order.move": { "zh": "移动", "en": "Move" }, ... }
    /// A missing translation falls back to English, then to the key itself,
    /// so a gap is visible instead of crashing.
    /// </summary>
    public sealed class LocalizationTable
    {
        public const string Chinese = "zh";
        public const string English = "en";

        readonly Dictionary<string, Dictionary<string, string>> _entries;

        LocalizationTable(Dictionary<string, Dictionary<string, string>> entries)
        {
            _entries = entries;
        }

        public static LocalizationTable FromJson(string json)
        {
            var entries = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
                ?? new Dictionary<string, Dictionary<string, string>>();
            return new LocalizationTable(entries);
        }

        public static LocalizationTable Empty() => new LocalizationTable(new Dictionary<string, Dictionary<string, string>>());

        public int Count => _entries.Count;
        public bool Contains(string key) => _entries.ContainsKey(key);

        public string Get(string key, string language)
        {
            if (key != null && _entries.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(language, out var text) && !string.IsNullOrEmpty(text))
                {
                    return text;
                }

                if (translations.TryGetValue(English, out text) && !string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            return key ?? string.Empty;
        }
    }
}
