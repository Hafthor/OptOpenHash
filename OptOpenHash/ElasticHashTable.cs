using System.Diagnostics.Contracts;

namespace OptOpenHash;

public class ElasticHashTable<TKey, TValue> {
    private const int C = 4;
    private const double Threshold = 1.0 / C, Delta = 0.1, HalfDelta = Delta / 2;
    private static double Log2Delta = -Math.Log2(Delta);
    private int numInserts, capacity, maxInserts;
    private readonly List<(TKey key, TValue value)?[]> levels = new();
    private readonly List<int> occupancies = new();

    public ElasticHashTable() {
        capacity = 0;
        for (int i = 0, size = 1; i < 10; i++, size <<= 1) {
            levels.Add(new (TKey, TValue)?[size]);
            occupancies.Add(0);
            capacity += size;
        }
        maxInserts = capacity - (int)(Delta * capacity);
        numInserts = 0;
    }

    public bool Insert(TKey key, TValue value) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < levels.Count - 1; i++) {
            var level = levels[i];
            double load = (double)(level.Length - occupancies[i]) / level.Length;
            if (load > HalfDelta) {
                var nextLevel = levels[i + 1];
                int nextFree = nextLevel.Length - occupancies[i + 1];
                double nextLoad = nextLevel.Length > 0 ? (double)nextFree / nextLevel.Length : 0;
                if (nextLoad > Threshold) {
                    int probeLimit = Math.Max(1, (int)(C * Math.Min(Math.Log2(load > 0 ? 1 / load : 0), Log2Delta)));
                    if (InsertAt(probeLimit, i, level)) return true;
                } else {
                    if (InsertAt(level.Length, i, nextLevel)) return true;
                }
            }
        }
        if (InsertAt(levels[^1].Length, levels.Count - 1, levels[^1])) return true;
        // Expand
        int nextSize = levels[^1].Length << 1;
        levels.Add(new (TKey key, TValue value)?[nextSize]);
        occupancies.Add(0);
        capacity += nextSize;
        maxInserts = capacity - (int)(Delta * capacity);
        if (InsertAt(nextSize, levels.Count - 1, levels[^1])) return true;
        return false;

        bool InsertAt(int probeLimit, int i, (TKey key, TValue value)?[] level) {
            int j = 0;
            for (uint hashi = hash ^ (uint)i; j < probeLimit; hashi += (uint)(j + ++j)) {
                int idx = (int)(hashi % level.Length);
                var entry = level[idx];
                if (!entry.HasValue) {
                    level[idx] = (key, value);
                    occupancies[i]++;
                    numInserts++;
                    return true;
                }
            }
            return false;
        }
    }

    public TValue Search(TKey key) {
        var entry = FindEntry(key);
        return entry.HasValue ? entry.Value.value : default;
    }

    private (TKey key, TValue value)? FindEntry(TKey key) {
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < levels.Count; i++) {
            var level = levels[i];
            int size = level.Length;
            uint hashi = hash ^ (uint)i;
            for (int j = 0; j < size; hashi += (uint)(j + ++j)) {
                var entry = level[hashi % size];
                if (!entry.HasValue) break;
                if (entry.Value.key.Equals(key)) return entry;
            }
        }
        return null;
    }

    public bool Contains(TKey key) => FindEntry(key).HasValue;

    public int Remaining => maxInserts - numInserts;

    public int Count => numInserts;
}