using OptOpenHash;

namespace TestOptOpenHash;

[TestClass]
public class KrapivinHashTableTests {
    [TestMethod]
    public void Test() {
        var table = new KrapivinHashTable<string, string>();
        int nInsert = 2000;
        for (int i = 0; i < nInsert; i++) {
            Assert.IsTrue(table.Add($"key{i}", $"value{i}"));
        }
        for (int i = 0; i < nInsert; i++) {
            string? value = table.GetValueOrDefault($"key{i}");
            Assert.IsNotNull(value, $"Value for key{i} should not be null");
            Assert.AreEqual($"value{i}", value!);
        }
        
        Assert.IsNull(table.GetValueOrDefault("nonexistent"));
        
        Assert.AreEqual(false, table.AddOrUpdate("key99", "new value99"));
        Assert.AreEqual("new value99", table.GetValueOrDefault("key99"));
    }
}