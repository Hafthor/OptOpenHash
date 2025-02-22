using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OptOpenHash;

BenchmarkRunner.Run<OptOpenHashBenchmarks>();

[MemoryDiagnoser(false)]
public class OptOpenHashBenchmarks {
    private const int N = 1000;
    private static readonly string[] keys = Enumerable.Range(0, N).Select(i=> $"key{i}").ToArray();
    private static readonly string[] values = Enumerable.Range(0, N).Select(i=> $"value{i}").ToArray();
    
    [Benchmark] // 461.8us - 48.8KB
    public void ElasticHashTable() {
        var table = new ElasticHashTable<string, string>();
        int nInsert = N;
        for (int i = 0; i < nInsert; i++) {
            table.Add(keys[i], values[i]);
        }
        for (int i = 0; i < nInsert; i++) {
            _ = table.GetValueOrDefault(keys[i]);
        }
        _ = table.GetValueOrDefault("nonexistent");
    }
    
    [Benchmark] // 120.3us - 29.77KB
    public void FunnelHashTable() {
        int capacity = N;
        var table = new FunnelHashTable<string, string>(capacity + (capacity >> 2));
        int nInsert = N;
        for (int i = 0; i < nInsert; i++) {
            if (!table.Add(keys[i], values[i])) throw new ApplicationException();
        }
        for (int i = 0; i < nInsert; i++) {
            _ = table.GetValueOrDefault(keys[i]);
        }
        _ = table.GetValueOrDefault("nonexistent");
    }
}