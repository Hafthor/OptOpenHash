using OptOpenHash;

namespace TestOptOpenHash;

[TestClass]
public class ElasticHashTableTests {
    [TestMethod]
    public void Test() {
        var table = new ElasticHashTable<string, string>();
        int nInsert = 2000;
        for (int i = 0; i < nInsert; i++) {
            table.Insert($"key{i}", $"value{i}");
        }
        for (int i = 0; i < nInsert; i++) {
            string? value = table.Search($"key{i}");
            Assert.IsNotNull(value, $"Value for key{i} should not be null");
            Assert.AreEqual($"value{i}", value!);
        }
        Assert.IsNull(table.Search("nonexistent"));
    }
}