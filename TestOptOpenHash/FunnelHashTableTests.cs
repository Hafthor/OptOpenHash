using OptOpenHash;

namespace TestOptOpenHash;

[TestClass]
public class FunnelHashTableTests {
    [TestMethod]
    public void Test() {
        int capacity = 1000;
        var table = new FunnelHashTable<string, string>(capacity);
        int nInsert = table.Remaining;
        for (int i = 0; i < nInsert; i++) {
            table.Insert($"key{i}", $"value{i}");
        }
        for (int i = 0; i < nInsert; i++) {
            Assert.AreEqual($"value{i}", table.Search($"key{i}"));
        }
        Assert.IsNull(table.Search("nonexistent"));
    }
}