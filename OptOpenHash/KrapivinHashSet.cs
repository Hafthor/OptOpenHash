using System.Collections;

namespace OptOpenHash;

public class KrapivinHashSet<TKey> : ISet<TKey> {
    private readonly Func<TKey, TKey, bool> comparer;
    private BitArray used;
    private TKey[] table;
    private int count;

    public KrapivinHashSet(int capacity = 1024, IEqualityComparer<TKey> comparer = null) {
        this.comparer = comparer == null ? (x, y) => x.Equals(y) : comparer.Equals;
        table = new TKey[capacity];
        used = new BitArray(capacity);
    }

    private int CalcIndex(uint hash, int index) {
        uint mul = (index & 1) == 0 ? (uint)(hash / table.Length + 1) : (uint)index;
        return (int)((uint)(hash + index * mul) % table.Length);
    }
    
    public IEnumerator<TKey> GetEnumerator() {
        for (int i = 0; i < table.Length; i++) {
            if (used[i]) yield return table[i];
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
        BitArray array = new(table.Length);
        foreach (var item in other) {
            int index = FindEntry(item);
            if (index < 0) return false;
            array[index] = true;
        }
        for (var i = 0; i < table.Length; i++) {
            var item = table[i];
            if (used[i] && !array[FindEntry(item)]) return false;
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
    
    public bool Add(TKey item) {
        int index = FindSlot(item);
        if (index == -1) throw new NotSupportedException("Table is full"); // Table is full
        bool isNew = !used[index];
        if (!isNew) return false;
        if (CheckResize()) index = FindSlot(item); // at 75% full, double size
        table[index] = item;
        used[index] = true;
        count++;
        return true;
    }

    public void Clear() {
        Array.Clear(table, 0, table.Length);
        used.SetAll(false);
        count = 0;
    }

    public bool Contains(TKey item) => FindEntry(item) >= 0;

    public void CopyTo(TKey[] array, int arrayIndex) {
        for (var i = 0; i < table.Length; i++) {
            if (used[i]) array[arrayIndex++] = table[i];
        }
    }

    public bool Remove(TKey item) {
        uint hash = (uint)(item?.GetHashCode() ?? 0);
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!used[index]) return false;
            if (comparer(table[index], item)) {
                used[index] = false;
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
            if (!used[index]) break;
            entries.Add(table[index]);
            used[index] = false;
        }
        count -= entries.Count;
        foreach (var entry in entries) {
            Add(entry);
        }
    }

    public int Count => count;
    
    public bool IsReadOnly => false;
    
    private int FindEntry(TKey key) {
        uint hash = (uint)(key?.GetHashCode() ?? 0);
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (used[index] && comparer(table[index], key)) return index;
            if (!used[index]) break;
        }
        return -1;
    }
    
    private int FindSlot(TKey key) {
        uint hash = (uint)(key?.GetHashCode() ?? 0);
        for (int i = 0; i < table.Length; i++) {
            int index = CalcIndex(hash, i);
            if (!used[index] || comparer(table[index], key)) return index;
        }
        return -1; // Table is full
    }
    
    private bool CheckResize() {
        // at 75% full, double size
        if (count < (table.Length * 3) >> 2) return false; // not full enough
        var oldTable = table;
        var oldUsed = used;
        table = new TKey[oldTable.Length << 1];
        used = new BitArray(table.Length);
        count = 0;
        for (var i = 0; i < oldTable.Length; i++) {
            if (oldUsed[i]) {
                Add(oldTable[i]);
            }
        }
        return true;
    }
}