﻿using System.Collections.Generic;
using System.Globalization;

namespace NeeLaboratory.Resources
{
    /// <summary>
    /// テキストリソース
    /// </summary>
    public class TextResourceSet
    {
        private readonly CultureInfo _culture;
        private readonly Dictionary<string, TextResourceItem> _map;


        public TextResourceSet()
        {
            _culture = CultureInfo.InvariantCulture;
            _map = new();
        }

        public TextResourceSet(CultureInfo culture, Dictionary<string, TextResourceItem> map)
        {
            _culture = culture;
            _map = map;
        }


        public CultureInfo Culture => _culture;

        public Dictionary<string, TextResourceItem> Map => _map;

        public bool IsValid => !_culture.Equals(CultureInfo.InvariantCulture);


        public string? this[string name]
        {
            get { return GetString(name); }
        }

        public string? GetString(string name)
        {
            return _map.TryGetValue(name, out var value) ? value.Text : null;
        }

        public string? GetCaseString(string name, string pattern)
        {
            return _map.TryGetValue(name, out var value) ? value.GetCaseText(pattern) : null;
        }

        public void Add(Dictionary<string, TextResourceItem> map)
        {
            foreach (var item in map)
            {
                _map[item.Key] = item.Value;
            }
        }
    }
}
