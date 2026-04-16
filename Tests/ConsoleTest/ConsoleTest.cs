using System;
using System.Collections.Generic;
using RS.SimpleJsonUnity;

namespace RS.SimpleJsonUnity.Tests
{
    class Program
    {
        static int passed = 0;
        static int failed = 0;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("SimpleJson-Unity 控制台测试");
            Console.WriteLine("========================================\n");

            TestBasicTypes();
            TestCollections();
            TestNestedObjects();
            TestJsonAlias();
            TestJsonIgnore();
            TestCircularReference();
            TestDataContract();
            TestEdgeCases();

            Console.WriteLine("\n========================================");
            Console.WriteLine($"结果: {passed} 通过, {failed} 失败, {passed + failed} 总计");
            Console.WriteLine("========================================");

            Environment.Exit(failed > 0 ? 1 : 0);
        }

        static void Assert(string name, bool condition)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {name}");
                passed++;
            }
            else
            {
                Console.WriteLine($"[FAIL] {name}");
                failed++;
            }
        }

        static void TestBasicTypes()
        {
            Console.WriteLine("\n── 基础类型测试 ──────────────────────");

            var obj = new BasicPoco
            {
                IntValue = 42,
                StringValue = "Hello",
                DoubleValue = 3.14159,
                BoolValue = true,
                NullableInt = null,
                DateTimeValue = new DateTime(2025, 4, 15, 12, 30, 0)
            };

            string json = SimpleJson.SerializeObject(obj);
            Console.WriteLine($"序列化: {json}");

            var result = SimpleJson.DeserializeObject<BasicPoco>(json);
            Assert("基础类型序列化/反序列化", result.IntValue == 42 && result.StringValue == "Hello");
            Assert("Nullable<int> 为 null", result.NullableInt == null);
            Assert("DateTime 格式正确", result.DateTimeValue.Year == 2025);
        }

        static void TestCollections()
        {
            Console.WriteLine("\n── 集合类型测试 ──────────────────────");

            var list = new List<int> { 1, 2, 3, 4, 5 };
            string listJson = SimpleJson.SerializeObject(list);
            var listResult = SimpleJson.DeserializeObject<List<int>>(listJson);
            Assert("List<int> 序列化/反序列化", listResult.Count == 5 && listResult[2] == 3);

            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            string dictJson = SimpleJson.SerializeObject(dict);
            var dictResult = SimpleJson.DeserializeObject<Dictionary<string, int>>(dictJson);
            Assert("Dictionary<string,int> 序列化/反序列化", dictResult["a"] == 1 && dictResult["b"] == 2);

            var arr = new int[] { 10, 20, 30 };
            string arrJson = SimpleJson.SerializeObject(arr);
            var arrResult = SimpleJson.DeserializeObject<int[]>(arrJson);
            Assert("int[] 序列化/反序列化", arrResult.Length == 3 && arrResult[1] == 20);
        }

        static void TestNestedObjects()
        {
            Console.WriteLine("\n── 嵌套对象测试 ──────────────────────");

            var person = new Person
            {
                Name = "张三",
                Age = 30,
                Address = new Address
                {
                    Street = "长安街1号",
                    City = "北京",
                    Country = "中国"
                }
            };

            string json = SimpleJson.SerializeObject(person);
            Console.WriteLine($"序列化: {json}");

            var result = SimpleJson.DeserializeObject<Person>(json);
            Assert("嵌套对象序列化/反序列化", result.Name == "张三" && result.Address.City == "北京");
        }

        static void TestJsonAlias()
        {
            Console.WriteLine("\n── JsonAlias 测试 ──────────────────────");

            string json1 = @"{""user_name"": ""李四"", ""user_id"": 100}";
            string json2 = @"{""userName"": ""王五"", ""userId"": 200}";
            string json3 = @"{""UserName"": ""赵六"", ""UserId"": 300}";

            var result1 = SimpleJson.DeserializeObject<UserProfile>(json1);
            var result2 = SimpleJson.DeserializeObject<UserProfile>(json2);
            var result3 = SimpleJson.DeserializeObject<UserProfile>(json3);

            Assert("JsonAlias 别名 user_name", result1.UserName == "李四" && result1.UserId == 100);
            Assert("JsonAlias 别名 userName", result2.UserName == "王五" && result2.UserId == 200);
            Assert("JsonAlias 原始名称", result3.UserName == "赵六" && result3.UserId == 300);

            var strategy = new DefaultJsonSerializationStrategy(false, true);
            string aliasJson = SimpleJson.SerializeObject(new UserProfile { UserName = "测试", UserId = 400 }, strategy);
            Assert("序列化使用别名", aliasJson.Contains("user_name"));
        }

        static void TestJsonIgnore()
        {
            Console.WriteLine("\n── JsonIgnore 测试 ──────────────────────");

            var obj = new IgnorePoco
            {
                PublicValue = "可见",
                SecretValue = "秘密"
            };

            string json = SimpleJson.SerializeObject(obj);
            Assert("JsonIgnore 字段不被序列化", !json.Contains("Secret") && json.Contains("PublicValue"));

            var result = SimpleJson.DeserializeObject<IgnorePoco>(@"{""PublicValue"": ""新值"", ""SecretValue"": ""不应该设置""}");
            Assert("JsonIgnore 字段不被反序列化", result.SecretValue == null);
        }

        static void TestCircularReference()
        {
            Console.WriteLine("\n── 循环引用测试 ──────────────────────");

            var parent = new Node { Name = "Parent" };
            var child = new Node { Name = "Child", Parent = parent };
            parent.Children = new List<Node> { child };

            string json = SimpleJson.SerializeObject(parent);
            Console.WriteLine($"循环引用 JSON: {json}");

            Assert("循环引用不会导致栈溢出", json != null && json.Length > 0);
        }

        static void TestDataContract()
        {
            Console.WriteLine("\n── DataContract 测试 ──────────────────────");

            var strategy = new DataContractSerializationStrategy();
            var obj = new ContractPoco
            {
                Id = 1,
                Name = "测试",
                InternalField = "不应序列化"
            };

            string json = SimpleJson.SerializeObject(obj, strategy);
            Console.WriteLine($"DataContract JSON: {json}");

            Assert("DataContract 只序列化 DataMember", json.Contains("Id") && json.Contains("name") && !json.Contains("InternalField"));
        }

        static void TestEdgeCases()
        {
            Console.WriteLine("\n── 边界情况测试 ──────────────────────");

            try
            {
                SimpleJson.DeserializeObject<BasicPoco>("");
                Assert("空字符串处理", false);
            }
            catch
            {
                Assert("空字符串处理", true);
            }

            try
            {
                SimpleJson.DeserializeObject<BasicPoco>(null);
                Assert("null 字符串处理", false);
            }
            catch
            {
                Assert("null 字符串处理", true);
            }

            var emptyList = new List<int>();
            string emptyJson = SimpleJson.SerializeObject(emptyList);
            Assert("空列表序列化", emptyJson == "[]");

            var nullDict = new Dictionary<string, object> { { "key", null } };
            string nullJson = SimpleJson.SerializeObject(nullDict);
            Assert("null 值序列化", nullJson.Contains("null"));

            Assert("long.MaxValue 往返", SimpleJson.DeserializeObject<long>(SimpleJson.SerializeObject(long.MaxValue)) == long.MaxValue);
            Assert("double 精度", Math.Abs(SimpleJson.DeserializeObject<double>(SimpleJson.SerializeObject(3.14159265358979)) - 3.14159265358979) < 0.0001);
        }
    }

    #region 测试数据类型

    public class BasicPoco
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        public int? NullableInt { get; set; }
        public DateTime DateTimeValue { get; set; }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class UserProfile
    {
        [JsonAlias("user_name", "userName")]
        public string UserName { get; set; }

        [JsonAlias("user_id", "userId")]
        public int UserId { get; set; }
    }

    public class IgnorePoco
    {
        public string PublicValue { get; set; }

        [JsonIgnore]
        public string SecretValue { get; set; }
    }

    public class Node
    {
        public string Name { get; set; }
        public Node Parent { get; set; }
        public List<Node> Children { get; set; }
    }

    [System.Runtime.Serialization.DataContract]
    public class ContractPoco
    {
        [System.Runtime.Serialization.DataMember]
        public int Id { get; set; }

        [System.Runtime.Serialization.DataMember(Name = "name")]
        public string Name { get; set; }

        public string InternalField { get; set; }
    }

    #endregion
}
