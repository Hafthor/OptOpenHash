using OptOpenHash;

namespace TestOptOpenHash;

[TestClass]
public class KrapivinDictionaryTests {
    [TestMethod]
    public void Test() {
        var table = new KrapivinDictionary<string, string>();
        int nInsert = 2000;
        for (int i = 0; i < nInsert; i++) {
            table.Add($"key{i}", $"value{i}");
        }
        for (int i = 0; i < nInsert; i++) {
            string? value = table.GetValueOrDefault($"key{i}");
            Assert.IsNotNull(value, $"Value for key{i} should not be null");
            Assert.AreEqual($"value{i}", value!);
        }
        
        Assert.IsNull(table.GetValueOrDefault("nonexistent"));
        
        Assert.AreEqual(false, table.AddOrUpdate("key99", "new value99"));
        Assert.AreEqual("new value99", table.GetValueOrDefault("key99"));

        for (int i = 0; i < nInsert; i += 3) {
            // TODO: Currently fails randomly
            Assert.IsTrue(table.Remove($"key{i}"), $"Failed removing key{i}");
        }

        for (int i = 0; i < nInsert; i++) {
            string? value = table.GetValueOrDefault($"key{i}");
            if (i % 3 == 0) {
                Assert.IsNull(value, $"Value for key{i} should be null");
            } else {
                Assert.IsNotNull(value, $"Value for key{i} should not be null");
                Assert.AreEqual($"value{i}", value!);
            }
        }
    }
}