using System.Diagnostics.Contracts;

namespace OptOpenHash;

public class FunnelHashTable<TKey, TValue> {
    private const double Delta = 0.1;
    private static readonly double Log2Delta = -Math.Log2(Delta);
    private static readonly int Alpha = (int)Math.Ceiling(4 * Log2Delta) + 10; 
    private static readonly int Beta = (int)Math.Ceiling(2 * Log2Delta);
    private static readonly int BucketDiv = (int)(4 * (1 - Math.Pow(0.75, Alpha)));
    private readonly (TKey key, TValue value)?[][] levels;
    private readonly int[] buckets;
    private readonly int maxInserts, probeLimit;
    private int numInserts;

    public FunnelHashTable(int capacity) {
        Contract.Assert(capacity > 0, "Capacity must be positive");
        
        probeLimit = Math.Max(1, (int)Math.Ceiling(Math.Log(Math.Log(capacity + 1) + 1)));
        maxInserts = capacity - (int)(Delta * capacity);
        int specialSize = Math.Max(1, (int)Math.Floor(3 * Delta * capacity / 4));
        int remainingBuckets = (capacity - specialSize) / Beta;
        double a1 = BucketDiv != 0 ? remainingBuckets / BucketDiv : remainingBuckets;
        buckets = new int[Alpha];
        levels = new (TKey, TValue)?[Alpha + 1][];
        for (int i = 0; i < Alpha && remainingBuckets > 0; i++, a1 *= 0.75) {
            int aI = Math.Min(Math.Max(1, (int)a1), remainingBuckets);
            int extra = i < Alpha - 1 ? 0 : (remainingBuckets - aI) * Beta;
            levels[i] = new (TKey key, TValue value)?[aI * Beta + extra];
            remainingBuckets -= buckets[i] = aI + extra;
        }
        levels[^1] = new (TKey, TValue)?[specialSize];
    }
    
    public bool Insert(TKey key, TValue value) {
        Contract.Assert(numInserts < maxInserts, "Hash table is full");
        uint hash = (uint)key.GetHashCode();
        for (int i = 0; i < buckets.Length; i++) {
            if (buckets[i] > 0) {
                var level = levels[i];
                int bucketIndex = (int)((hash ^ (uint)i) % buckets[i]);
                for (int idx = bucketIndex * Beta, end = idx + Beta; idx < end; idx++) {
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
        int size = special.Length, specialIdx = (int)((hash ^ (uint)(levels.Length - 1)) % size);
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
        uint hash = (uint)key.GetHashCode();
        (TKey key, TValue value)? entry = null;
        for (int i = 0; i < buckets.Length; i++) {
            if (buckets[i] > 0) {
                var level = levels[i];
                int bucketIndex = (int)((hash ^ (uint)i) % buckets[i]);
                int idx = bucketIndex * Beta, end = idx + Beta;
                for (; idx < end && (entry = level[idx]).HasValue; idx++) {
                    if (entry.Value.key.Equals(key)) return entry;
                }
            }
        }
        var special = levels[^1];
        int size = special.Length, idx2 = (int)((hash ^ (uint)(levels.Length - 1)) % size);
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