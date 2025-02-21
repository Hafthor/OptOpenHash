using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OptOpenHash;

BenchmarkRunner.Run<OptOpenHashBenchmarks>();

[MemoryDiagnoser(false)]
public class OptOpenHashBenchmarks {
    private const int N = 1000;
    private static readonly string[] keys = Enumerable.Range(0, N).Select(i=> $"key{i}").ToArray();
    private static readonly string[] values = Enumerable.Range(0, N).Select(i=> $"value{i}").ToArray();
    
    [Benchmark] // 342.2us - 24.23KB
    public void ElasticHashTable() {
        int capacity = N;
        double delta = 0.1;
        var table = new ElasticHashTable<string, string>(capacity, delta, new Random(0));
        int nInsert = table.Remaining;
        for (int i = 0; i < nInsert; i++) {
            table.Insert(keys[i], values[i]);
        }
        for (int i = 0; i < nInsert; i++) {
            _ = table.Search(keys[i]);
        }
        _ = table.Search("nonexistent");
    }
    
    [Benchmark] // 110.3us - 24.37KB
    public void FunnelHashTable() {
        int capacity = N;
        double delta = 0.1;
        var table = new FunnelHashTable<string, string>(capacity, delta, new Random(0));
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