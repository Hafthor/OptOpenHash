using System.Collections;

namespace OptOpenHash;

public class KrapivinDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
    private readonly IEqualityComparer<TKey> comparer;
    private (TKey key, TValue value)?[] table;
    private int count;

    public KrapivinDictionary(int capacity = 1024, IEqualityComparer<TKey> comparer = null) {
        this.comparer = comparer;
        table = new(TKey, TValue)?[capacity];
    }

    private bool Compare(TKey key1, TKey key2) {
        return comparer == null ? key1.Equals(key2) : comparer.Equals(key1, key2);
    }

    private int CalcIndex(uint hash, int index) {
        uint mul = (index & 1) == 0 ? (uint)(hash / table.Length + 1) : (uint)index;
        return (int)((uint)(hash + index * mul) % table.Length);
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear() {
        Array.Clear(table, 0, table.Length);
        count = 0;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        int index = FindEntry(item.Key);
        if (index < 0 || !table[index].HasValue) return false;
        return table[index].Value.value.Equals(item.Value);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        foreach (var kvp in table) {
            if (kvp.HasValue) {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(kvp.Value.key, kvp.Value.value);
            }
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        int index = FindEntry(item.Key);
        if (index < 0 || !table[index].HasValue) return false;
        if (!table[index].Value.value.Equals(item.Value)) return false;
        return Remove(item.Key);
    }

    public int Count => count;
    
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value) {
        int index = FindSlot(key);
        if (index == -1) throw new NotSupportedException("Table is full"); // Table is full
        bool isNew = !table[index].HasValue;
        if (!isNew) throw new ArgumentException("An element with the same key already exists");
        if (CheckResize()) index = FindSlot(key); // at 75% full, double size
        table[index] = (key, value);
        count++;
    }
    
    public bool AddOrUpdate(TKey key, TValue value) {
        int index = FindSlot(key);
        if (index == -1) return false; // Table is full
        bool isNew = !table[index].HasValue;
        if (isNew && CheckResize()) index = FindSlot(key); // at 75% full, double size
        table[index] = (key, value);
        if (isNew) count++;
        return isNew;
    }
    
    private bool CheckResize() {
        if (count < (table.Length * 3) >> 2) return false;
        var oldTable = table;
        table = new (TKey, TValue)?[oldTable.Length << 1];
        count = 0;
        foreach (var entry in oldTable) {
            if (entry.HasValue) AddOrUpdate(entry.Value.key, entry.Value.value);
        }
        return true;
    }

    private int FindSlot(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!table[index].HasValue || Compare(table[index].Value.key, key)) return index;
        }
        return -1; // Table is full
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue = default) {
        int index = FindEntry(key);
        return index < 0 ? defaultValue : table[index].Value.value;
    }
    
    public bool ContainsKey(TKey key) => FindEntry(key) >= 0;

    private int FindEntry(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (table[index].HasValue && Compare(table[index].Value.key, key)) return index;
            if (!table[index].HasValue) break;
        }
        return -1;
    }

    public bool Remove(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!table[index].HasValue) return false;
            if (Compare(table[index].Value.key, key)) {
                table[index] = null;
                count--;
                RekeyAfterHole(hash, i);
                return true;
            }
        }
        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) {
        int index = FindEntry(key);
        if (index < 0 || !table[index].HasValue) {
            value = default;
            return false;
        }
        value = table[index].Value.value;
        return true;
    }

    public TValue this[TKey key] {
        get {
            int index = FindEntry(key);
            if (index < 0 || !table[index].HasValue) throw new KeyNotFoundException();
            return table[index].Value.value;
        }
        set {
            int index = FindEntry(key);
            if (index < 0) throw new KeyNotFoundException();
            table[index] = (key, value);
        }
    }

    public ICollection<TKey> Keys => table.Where(e => e.HasValue).Select(e => e.Value.key).ToList();

    public ICollection<TValue> Values => table.Where(e => e.HasValue).Select(e => e.Value.value).ToList();

    private void RekeyAfterHole(uint hash, int i) {
        List<(TKey key, TValue value)> entries = new();
        for (int j = i + 1; j < table.Length; j++) {
            int index = CalcIndex(hash, j);
            if (!table[index].HasValue) break;
            entries.Add(table[index].Value);
            table[index] = null;
        }
        count -= entries.Count;
        foreach (var entry in entries) {
            AddOrUpdate(entry.key, entry.value);
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        foreach(var entry in table) {
            if (entry.HasValue) {
                yield return new KeyValuePair<TKey, TValue>(entry.Value.key, entry.Value.value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}