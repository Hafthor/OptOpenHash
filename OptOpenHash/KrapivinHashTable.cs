namespace OptOpenHash;

public class KrapivinHashTable<TKey, TValue>(int capacity = 1024) {
    private (TKey key, TValue value)?[] table = new(TKey, TValue)?[capacity];
    private int count;

    private int CalcIndex(uint hash, int index) {
        uint mul = (index & 1) == 0 ? (uint)(hash / table.Length + 1) : (uint)index;
        return (int)((uint)(hash + index * mul) % table.Length);
    }

    public bool Add(TKey key, TValue value) {
        if (count >= table.Length * 3 / 4) Resize(); // at 75% full, double size
        int index = FindSlot(key);
        if (index == -1) return false; // Table is full
        bool isNew = !table[index].HasValue;
        if (!isNew) return false;
        table[index] = (key, value);
        count++;
        return true;
    }
    
    public bool AddOrUpdate(TKey key, TValue value) {
        if (count >= table.Length * 3 / 4) Resize(); // at 75% full, double size
        int index = FindSlot(key);
        if (index == -1) return false; // Table is full
        bool isNew = !table[index].HasValue;
        table[index] = (key, value);
        if (isNew) count++;
        return isNew;
    }

    private void Resize() {
        var oldTable = table;
        table = new (TKey, TValue)?[oldTable.Length << 1];
        count = 0;
        foreach (var entry in oldTable) {
            if (entry.HasValue) AddOrUpdate(entry.Value.key, entry.Value.value);
        }
    }

    private int FindSlot(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!table[index].HasValue || table[index].Value.key.Equals(key)) return index;
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
            if (table[index].HasValue && table[index].Value.key.Equals(key)) return index;
        }
        return -1;
    }

    public bool Remove(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!table[index].HasValue) return false;
            if (table[index].Value.key.Equals(key)) {
                table[index] = null;
                count--;
                RekeyAfterHole(hash, i);
                return true;
            }
        }
        return false;
    }

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
}