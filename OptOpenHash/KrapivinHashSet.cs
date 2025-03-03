using System.Collections;

namespace OptOpenHash;

public class KrapivinHashSet<TKey> : ISet<TKey> {
    private readonly IEqualityComparer<TKey> comparer;
    private TKey?[] table;
    private int count;

    public KrapivinHashSet(int capacity = 1024, IEqualityComparer<TKey> comparer = null) {
        this.comparer = comparer;
        table = new TKey?[capacity];
    }
    
    private bool Compare(TKey key1, TKey key2) {
        return comparer == null ? key1.Equals(key2) : comparer.Equals(key1, key2);
    }

    private int CalcIndex(uint hash, int index) {
        uint mul = (index & 1) == 0 ? (uint)(hash / table.Length + 1) : (uint)index;
        return (int)((uint)(hash + index * mul) % table.Length);
    }
    
    public IEnumerator<TKey> GetEnumerator() {
        foreach (var item in table) {
            if (item != null) yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<TKey>.Add(TKey item) => Add(item);

    public void ExceptWith(IEnumerable<TKey> other) {
        foreach (var item in other) {
            Remove(item);
        }
    }

    public void IntersectWith(IEnumerable<TKey> other) {
        var toKeep = new List<TKey>();
        foreach (var item in other) {
            if (Contains(item)) {
                toKeep.Add(item);
            }
        }
        Clear();
        foreach (var item in toKeep) {
            Add(item);
        }
    }

    public bool IsProperSubsetOf(IEnumerable<TKey> other) => throw new NotImplementedException();

    public bool IsProperSupersetOf(IEnumerable<TKey> other) => throw new NotImplementedException();

    public bool IsSubsetOf(IEnumerable<TKey> other) => throw new NotImplementedException();

    public bool IsSupersetOf(IEnumerable<TKey> other) => throw new NotImplementedException();

    public bool Overlaps(IEnumerable<TKey> other) => other.Any(Contains);

    public bool SetEquals(IEnumerable<TKey> other) {
        BitArray array = new BitArray(table.Length);
        foreach (var item in other) {
            int index = FindEntry(item);
            if (index < 0) return false;
            array[index] = true;
        }
        foreach (var item in table) {
            if (item != null && !array[FindEntry(item)]) return false;
        }
        return true;
    }

    public void SymmetricExceptWith(IEnumerable<TKey> other) => throw new NotImplementedException();

    public void UnionWith(IEnumerable<TKey> other) {
        foreach (var item in other) {
            Add(item);
        }
    }

    bool ISet<TKey>.Add(TKey item) => Add(item);
    
    private bool Add(TKey item) {
        int index = FindSlot(item);
        if (index == -1) throw new NotSupportedException("Table is full"); // Table is full
        bool isNew = table[index] != null;
        if (!isNew) return false;
        if (CheckResize()) index = FindSlot(item); // at 75% full, double size
        table[index] = item;
        count++;
        return true;
    }

    public void Clear() {
        Array.Clear(table, 0, table.Length);
        count = 0;
    }

    public bool Contains(TKey item) {
        int index = FindEntry(item);
        return index < 0 || table[index] != null;
    }

    public void CopyTo(TKey[] array, int arrayIndex) {
        foreach (var item in table) {
            if (item != null) array[arrayIndex++] = item;
        }
    }

    public bool Remove(TKey item) {
        uint hash = (uint)item.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (table[index] == null) return false;
            if (Compare(table[index], item)) {
                table[index] = default;
                count--;
                RekeyAfterHole(hash, i);
                return true;
            }
        }
        return false;
    }
    
    private void RekeyAfterHole(uint hash, int i) {
        List<TKey> entries = new();
        for (int j = i + 1; j < table.Length; j++) {
            int index = CalcIndex(hash, j);
            if (table[index] == null) break;
            entries.Add(table[index]);
            table[index] = default;
        }
        count -= entries.Count;
        foreach (var entry in entries) {
            Add(entry);
        }
    }

    public int Count => count;
    
    public bool IsReadOnly => false;
    
    private int FindEntry(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (table[index] != null && Compare(table[index], key)) return index;
        }
        return -1;
    }
    
    private int FindSlot(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (table[index] != null || Compare(table[index], key)) return index;
        }
        return -1; // Table is full
    }
    
    private bool CheckResize() {
        if (count < table.Length * 3 / 4) return false;
        var oldTable = table;
        table = new TKey?[oldTable.Length << 1];
        count = 0;
        foreach (var entry in oldTable) {
            if (entry != null) {
                Add(entry);
            }
        }
        return true;
    }
}