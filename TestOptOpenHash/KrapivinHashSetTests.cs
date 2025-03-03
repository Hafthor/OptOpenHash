using OptOpenHash;

namespace TestOptOpenHash;

[TestClass]
public class KrapivinHashSetTests {
    [TestMethod]
    public void Test() {
        var table = new KrapivinHashSet<string>();
        int nInsert = 2000;
        for (int i = 0; i < nInsert; i++) {
            table.Add($"key{i}");
        }
        for (int i = 0; i < nInsert; i++) {
            bool value = table.Contains($"key{i}");
            Assert.IsTrue(value, $"Value for key{i} should be true");
        }
        
        Assert.IsFalse(table.Contains("nonexistent"));
        
        for (int i = 0; i < nInsert; i += 3) {
            // TODO: Currently fails randomly
            Assert.IsTrue(table.Remove($"key{i}"), $"Failed removing key{i}");
        }

        for (int i = 0; i < nInsert; i++) {
            bool value = table.Contains($"key{i}");
            Assert.AreEqual(i % 3 > 0, value, $"Value for key{i}");
        }
    }
}