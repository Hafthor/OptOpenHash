using System.Diagnostics.Contracts;

namespace OptOpenHash;

public class ElasticHashTable<TKey, TValue> {
    private const int C = 4;
    private const double Threshold = 1.0 / C;
    private readonly int maxInserts;
    private int numInserts;
    private readonly double halfDelta, log2Delta;
    private readonly (TKey key, TValue value)?[][] levels;
    private readonly int[] occupancies;
    private readonly uint[] salts;

    public ElasticHashTable(int capacity, double delta = 0.1, Random random = null) {
        Contract.Assert(capacity > 0, "Capacity must be positive");
        Contract.Assert(delta is > 0 and < 1, "Delta must be between 0 and 1");
        random ??= new Random();

        halfDelta = delta / 2;
        log2Delta = -Math.Log2(delta);
        maxInserts = capacity - (int)(delta * capacity);
        numInserts = 0;

        int numLevels = Math.Max(1, Log2Floor(capacity)), remaining = capacity;
        levels = new (TKey, TValue)?[numLevels][];
        salts = new uint[numLevels];
        occupancies = new int[numLevels];
        for (int i = 0; i < numLevels - 1; i++) {
            int size = Math.Max(1, remaining / (1 << (numLevels - i)));
            levels[i] = new (TKey, TValue)?[size];
            salts[i] = (uint)random.Next(int.MinValue, int.MaxValue);
            remaining -= size;
        }
        levels[^1] = new (TKey key, TValue value)?[remaining];
        salts[^1] = (uint)random.Next(int.MinValue, int.MaxValue);
    }
    
    private static int Log2Floor(int value) {
        int log = 30;
        for (int b = 1 << 30; b > 0; b >>= 1, log--)
            if (value >= b)
                return log;
        throw new ArgumentOutOfRangeException(nameof(value), "value must be positive non-zero");
    }

    public bool Insert(TKey key, TValue value) {
        uint hash = (uint)key.GetHashCode();
        Contract.Assert(numInserts < maxInserts, "Hash table is full");
        for (int i = 0; i < levels.Length - 1; i++) {
            var level = levels[i];
            double load = (double)(level.Length - occupancies[i]) / level.Length;
            if (load > halfDelta) {
                var nextLevel = levels[i + 1];
                int nextFree = nextLevel.Length - occupancies[i + 1];
                double nextLoad = nextLevel.Length > 0 ? (double)nextFree / nextLevel.Length : 0;
                if (nextLoad > Threshold) {
                    int probeLimit = Math.Max(1, (int)(C * Math.Min(Math.Log2(load > 0 ? 1 / load : 0), log2Delta)));
                    if (InsertAt(probeLimit, i, level)) return true;
                } else {
                    if (InsertAt(level.Length, i, nextLevel)) return true;
                }
            }
        }
        if (InsertAt(levels[^1].Length, levels.Length - 1, levels[^1])) return true;
        Contract.Assert(false, "Hash table is full");
        return false;

        bool InsertAt(int probeLimit, int i, (TKey key, TValue value)?[] level) {
            int j = 0;
            for (uint hashi = hash ^ salts[i]; j < probeLimit; hashi += (uint)(j + ++j)) {
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
        for (int i = 0; i < levels.Length; i++) {
            var level = levels[i];
            int size = level.Length;
            uint hashi = hash ^ salts[i];
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