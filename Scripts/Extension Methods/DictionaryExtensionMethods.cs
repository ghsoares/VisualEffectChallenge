using System.Collections.Generic;

namespace ExtensionMethods {
    namespace DictionaryExtensions {
        public static class DictionaryExtensionMethods {
            public static TV Get<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV)) {
                return dict.TryGetValue(key, out TV value) ? value : defaultValue;
            }
        }
    }
}