using System.Diagnostics.Contracts;

namespace OptOpenHash;

public class FunnelHashTable<TKey, TValue> {
    private readonly (TKey key, TValue value)?[][] levels;
    private readonly int[] buckets, salts;
    private readonly int maxInserts, beta, probeLimit;
    private int numInserts;

    public FunnelHashTable(int capacity, double delta = 0.1, Random random = null) {
        Contract.Assert(capacity > 0, "Capacity must be positive");
        Contract.Assert(delta is > 0 and < 1, "Delta must be between 0 and 1");
        random ??= new Random();

        probeLimit = Math.Max(1, (int)Math.Ceiling(Math.Log(Math.Log(capacity + 1) + 1)));
        maxInserts = capacity - (int)(delta * capacity);
        double log2Delta = -Math.Log2(delta);
        var alpha = (int)Math.Ceiling(4 * log2Delta) + 10;
        beta = (int)Math.Ceiling(2 * log2Delta);
        int specialSize = Math.Max(1, (int)Math.Floor(3 * delta * capacity / 4));
        int remainingBuckets = (capacity - specialSize) / beta;
        double a1 = alpha > 0 ? remainingBuckets / (int)(4 * (1 - Math.Pow(0.75, alpha))) : remainingBuckets;
        buckets = new int[alpha];
        levels = new (TKey, TValue)?[alpha + 1][];
        salts = new int[alpha + 1];
        for (int i = 0; i < alpha && remainingBuckets > 0; i++, a1 *= 0.75) {
            int aI = Math.Min(Math.Max(1, (int)a1), remainingBuckets);
            int extra = i < alpha - 1 ? 0 : (remainingBuckets - aI) * beta;
            levels[i] = new (TKey key, TValue value)?[aI * beta + extra];
            remainingBuckets -= buckets[i] = aI + extra;
            salts[i] = random.Next();
        }
        levels[^1] = new (TKey, TValue)?[specialSize];
        salts[^1] = random.Next();
    }

    public bool Insert(TKey key, TValue value) {
        Contract.Assert(numInserts < maxInserts, "Hash table is full");
        int hash = key.GetHashCode() & 0x7FFFFFFF;
        for (int i = 0; i < buckets.Length; i++) {
            if (buckets[i] > 0) {
                var level = levels[i];
                int bucketIndex = (hash ^ salts[i]) % buckets[i];
                for (int idx = bucketIndex * beta, end = idx + beta; idx < end; idx++) {
                    var entry = level[idx];
                    if (!entry.HasValue || entry.Value.key.Equals(key)) {
                        if (!entry.HasValue) numInserts++;
                        level[idx] = (key, value);
                        return true;
                    }
                }
            }
        }
        var special = levels[^1];
        int size = special.Length, specialIdx = (hash ^ salts[^1]) % size;
        for (int j = 0, idx = specialIdx; j < probeLimit; j++, idx = (idx + 1) % size) {
            var entry = special[idx];
            if (!entry.HasValue || entry.Value.key.Equals(key)) {
                if (!entry.HasValue) numInserts++;
                special[idx] = (key, value);
                return true;
            }
        }
        Contract.Assert(false, "Hash table is full");
        return false;
    }

    private (TKey key, TValue value)? FindEntry(TKey key) {
        int hash = key.GetHashCode() & 0x7FFFFFFF;
        (TKey key, TValue value)? entry = null;
        for (int i = 0; i < buckets.Length; i++) {
            if (buckets[i] > 0) {
                var level = levels[i];
                int bucketIndex = (hash ^ salts[i]) % buckets[i];
                int idx = bucketIndex * beta, end = idx + beta;
                for (; idx < end && (entry = level[idx]).HasValue; idx++) {
                    if (entry.Value.key.Equals(key)) return entry;
                }
            }
        }
        var special = levels[^1];
        int size = special.Length, idx2 = (hash ^ salts[^1]) % size;
        for (int j = 0; j < probeLimit && (entry = special[idx2]).HasValue; j++, idx2 = (idx2 + 1) % size) {
            if (entry.Value.key.Equals(key)) return entry;
        }
        return entry;
    }

    public TValue Search(TKey key) {
        var entry = FindEntry(key);
        return entry.HasValue ? entry.Value.value : default;
    }

    public bool Contains(TKey key) => FindEntry(key).HasValue;

    public int Count => numInserts;

    public int Remaining => maxInserts - numInserts;
}