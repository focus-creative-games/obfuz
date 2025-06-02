using System;
using System.Collections.Generic;

namespace Obfuz.Utils
{
    public class CachedDictionary<K, V>
    {
        private readonly Func<K, V> _valueFactory;
        private readonly Dictionary<K, V> _cache = new Dictionary<K, V>();

        public CachedDictionary(Func<K, V> valueFactory)
        {
            _valueFactory = valueFactory;
        }

        public V GetValue(K key)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                value = _valueFactory(key);
                _cache[key] = value;
            }
            return value;
        }
    }
}
