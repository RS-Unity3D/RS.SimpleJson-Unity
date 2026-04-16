using System;
using System.Collections.Generic;
using RS.SimpleJsonUnity;

// 命名空间1
namespace Game.Data.V1 {
    public class PlayerData {
        public string Name { get; set; }
        public int Level { get; set; }
        public float Health { get; set; }
        public List<string> Items { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Inventory {
        public Dictionary<string, int> Items { get; set; }
        public int Gold { get; set; }
    }
}

// 命名空间2 - 具有相同字段/属性名称和类型
namespace Game.Data.V2 {
    public class PlayerData {
        public string Name { get; set; }
        public int Level { get; set; }
        public float Health { get; set; }
        public List<string> Items { get; set; }
        public DateTime CreatedAt { get; set; }
        // V2 新增字段
        public string PlayerId { get; set; }
    }

    public class Inventory {
        public Dictionary<string, int> Items { get; set; }
        public int Gold { get; set; }
        // V2 新增字段
        public int Silver { get; set; }
    }
}

// 命名空间3 - 部分字段名称相同，但有不同类型的字段
namespace Game.Data.V3 {
    public class PlayerData {
        public string Name { get; set; }
        public int Level { get; set; }
        public double Health { get; set; } // 类型不同：float vs double
        public List<string> Items { get; set; }
        public string CreatedAt { get; set; } // 类型不同：DateTime vs string
    }
}

class CrossNamespaceTest {
    static void Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("========================================");
        Console.WriteLine("跨命名空间类转换测试");
        Console.WriteLine("========================================\n");

        // 测试1: 相同字段的类之间转换
        TestSameFieldsConversion();
        
        // 测试2: 新增字段的类之间转换（V1 -> V2）
        TestWithExtraFieldsV1ToV2();
        
        // 测试3: 新增字段的类之间转换（V2 -> V1）
        TestWithExtraFieldsV2ToV1();
        
        // 测试4: 不同类型字段的类之间转换
        TestDifferentTypesConversion();

        Console.WriteLine("\n========================================");
        Console.WriteLine("测试完成！");
        Console.WriteLine("========================================");
    }

    static void TestSameFieldsConversion() {
        Console.WriteLine("\n── 测试1: 相同字段的类之间转换 ─────────────────");

        // 创建 V1 的 PlayerData
        var v1Player = new Game.Data.V1.PlayerData {
            Name = "Hero",
            Level = 10,
            Health = 100.5f,
            Items = new List<string> { "Sword", "Shield" },
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        // 序列化为 JSON
        string json = SimpleJson.SerializeObject(v1Player);
        Console.WriteLine($"V1 PlayerData JSON:\n{json}");

        // 反序列化为 V2 的 PlayerData
        var v2Player = SimpleJson.DeserializeObject<Game.Data.V2.PlayerData>(json);
        
        Console.WriteLine("\nV2 PlayerData (从V1 JSON反序列化):");
        Console.WriteLine($"  Name: {v2Player.Name}");
        Console.WriteLine($"  Level: {v2Player.Level}");
        Console.WriteLine($"  Health: {v2Player.Health}");
        Console.WriteLine($"  Items: [{string.Join(", ", v2Player.Items)}]");
        Console.WriteLine($"  CreatedAt: {v2Player.CreatedAt}");
        Console.WriteLine($"  PlayerId (新增字段): {v2Player.PlayerId ?? "null"}");

        // 验证转换正确性
        bool success = 
            v2Player.Name == "Hero" &&
            v2Player.Level == 10 &&
            Math.Abs(v2Player.Health - 100.5f) < 0.01f &&
            v2Player.Items.Count == 2 &&
            v2Player.Items[0] == "Sword" &&
            v2Player.Items[1] == "Shield" &&
            v2Player.CreatedAt.Year == 2024 &&
            v2Player.PlayerId == null;

        Console.WriteLine($"\n转换结果: {(success ? "✓ 成功" : "✗ 失败")}");
    }

    static void TestWithExtraFieldsV1ToV2() {
        Console.WriteLine("\n── 测试2: V1 -> V2（V2有新增字段） ─────────────");

        var v2Player = new Game.Data.V2.PlayerData {
            Name = "Warrior",
            Level = 20,
            Health = 200.0f,
            Items = new List<string> { "Axe", "Armor" },
            CreatedAt = new DateTime(2024, 2, 20, 15, 45, 0),
            PlayerId = "player_123"
        };

        string json = SimpleJson.SerializeObject(v2Player);
        Console.WriteLine($"V2 PlayerData JSON:\n{json}");

        // 反序列化为 V1
        var v1Player = SimpleJson.DeserializeObject<Game.Data.V1.PlayerData>(json);

        Console.WriteLine("\nV1 PlayerData (从V2 JSON反序列化):");
        Console.WriteLine($"  Name: {v1Player.Name}");
        Console.WriteLine($"  Level: {v1Player.Level}");
        Console.WriteLine($"  Health: {v1Player.Health}");
        Console.WriteLine($"  Items: [{string.Join(", ", v1Player.Items)}]");
        Console.WriteLine($"  CreatedAt: {v1Player.CreatedAt}");

        bool success = 
            v1Player.Name == "Warrior" &&
            v1Player.Level == 20 &&
            Math.Abs(v1Player.Health - 200.0f) < 0.01f &&
            v1Player.Items.Count == 2;

        Console.WriteLine($"\n转换结果: {(success ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine("注意: V2 的 PlayerId 字段被忽略（V1 没有此字段）");
    }

    static void TestWithExtraFieldsV2ToV1() {
        Console.WriteLine("\n── 测试3: V2 -> V1（V1字段更少） ─────────────");

        var v1Player = new Game.Data.V1.PlayerData {
            Name = "Mage",
            Level = 15,
            Health = 80.0f,
            Items = new List<string> { "Staff", "Spellbook" },
            CreatedAt = new DateTime(2024, 3, 10, 9, 0, 0)
        };

        string json = SimpleJson.SerializeObject(v1Player);

        var v2Player = SimpleJson.DeserializeObject<Game.Data.V2.PlayerData>(json);

        Console.WriteLine($"V2 PlayerData (从V1 JSON反序列化):");
        Console.WriteLine($"  Name: {v2Player.Name}");
        Console.WriteLine($"  Level: {v2Player.Level}");
        Console.WriteLine($"  Health: {v2Player.Health}");
        Console.WriteLine($"  PlayerId（新增字段）: {v2Player.PlayerId ?? "null"}");

        bool success = 
            v2Player.Name == "Mage" &&
            v2Player.Level == 15 &&
            v2Player.PlayerId == null;

        Console.WriteLine($"\n转换结果: {(success ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine("注意: V2 的 PlayerId 字段为 null（JSON中没有）");
    }

    static void TestDifferentTypesConversion() {
        Console.WriteLine("\n── 测试4: 不同类型字段的类之间转换 ─────────────");

        var v1Player = new Game.Data.V1.PlayerData {
            Name = "Archer",
            Level = 12,
            Health = 95.5f,
            Items = new List<string> { "Bow", "Arrow" },
            CreatedAt = new DateTime(2024, 4, 5, 12, 0, 0)
        };

        string json = SimpleJson.SerializeObject(v1Player);
        Console.WriteLine($"V1 PlayerData JSON:\n{json}");

        // 反序列化为 V3（Health 类型不同：float vs double）
        var v3Player = SimpleJson.DeserializeObject<Game.Data.V3.PlayerData>(json);

        Console.WriteLine("\nV3 PlayerData (从V1 JSON反序列化):");
        Console.WriteLine($"  Name: {v3Player.Name}");
        Console.WriteLine($"  Level: {v3Player.Level}");
        Console.WriteLine($"  Health (float -> double): {v3Player.Health}");
        Console.WriteLine($"  CreatedAt (DateTime -> string): {v3Player.CreatedAt}");

        // 验证类型转换
        bool success = 
            v3Player.Name == "Archer" &&
            v3Player.Level == 12 &&
            Math.Abs(v3Player.Health - 95.5) < 0.01; // float 95.5 -> double 95.5

        Console.WriteLine($"\n转换结果: {(success ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine("注意: 兼容的数值类型可以自动转换（float -> double）");
    }
}