using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Utils {
    public static T[] SafeArray<T>(T[] arr) {
        if (arr == null) {
            return new T[] { };
        }
        else {
            return arr;
        }
    }

    public static IEnumerable<T> SafeEnumerable<T>(T[] array) {
        if (array == null) {
            return Enumerable.Empty<T>();
        } else {
            return array;
        }
    }

    public static IEnumerable<T> SafeEnumerable<T>(List<T> list) {
        if (list == null) {
            return Enumerable.Empty<T>();
        } else {
            return list;
        }
    }

    public static void AppendToList<K,V>(Dictionary<K, List<V>> dict, K key, V value) {
        if (!dict.TryGetValue(key, out List<V>list)) {
            list = new List<V>();
            dict.Add(key, list);
        }
        list.Add(value);
    }
}
