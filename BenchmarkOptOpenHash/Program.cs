using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OptOpenHash;

BenchmarkRunner.Run<OptOpenHashBenchmarks>();

[MemoryDiagnoser(false)]
public class OptOpenHashBenchmarks {
    private const int N = 1000;
    private static readonly string[] keys = Enumerable.Range(0, N).Select(i=> $"key{i}").ToArray();
    private static readonly string[] values = Enumerable.Range(0, N).Select(i=> $"value{i}").ToArray();
    
    [Benchmark] // 329.9us - 23.84KB
    public void ElasticHashTable() {
        int capacity = N;
        var table = new ElasticHashTable<string, string>(capacity);
        int nInsert = table.Remaining;
        for (int i = 0; i < nInsert; i++) {
            table.Insert(keys[i], values[i]);
        }
        for (int i = 0; i < nInsert; i++) {
            _ = table.Search(keys[i]);
        }
        _ = table.Search("nonexistent");
    }
    
    [Benchmark] // 107.7us - 23.94KB
    public void FunnelHashTable() {
        int capacity = N;
        var table = new FunnelHashTable<string, string>(capacity);
        int nInsert = table.Remaining;
        for (int i = 0; i < nInsert; i++) {
            table.Insert(keys[i], values[i]);
        }
        for (int i = 0; i < nInsert; i++) {
            _ = table.Search(keys[i]);
        }
        _ = table.Search("nonexistent");
    }
}