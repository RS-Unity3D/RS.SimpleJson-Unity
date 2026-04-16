using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using RS.SimpleJsonUnity;

namespace RS.SimpleJsonUnity.Tests
{
    class AotTest
    {
        static int m_passed = 0;
        static int m_failed = 0;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("AOT 兼容性全面测试");
            Console.WriteLine("========================================\n");

            TestAotRegistration();
            TestAotTypeCreation();
            TestSafeCreateDictionary();
            TestBasicPocoDeserialization();
            TestEnumDeserialization();
            TestNullableDeserialization();
            TestDateTimeDeserialization();
            TestCollectionDeserialization();
            TestDictionaryDeserialization();
            TestNestedPocoDeserialization();
            TestComplexNestedDeserialization();
            TestSerialization();
            TestRoundTrip();
            TestEdgeCases();
            TestFactoryByTypeLookup();
            TestNonGenericDeserialize();
            TestDataContractSerialization();
            TestJsonIgnoreAttribute();
            TestJsonAliasAttribute();
            TestArrayDeserialization();

            Console.WriteLine("\n========================================");
            Console.WriteLine($"结果: {m_passed} 通过, {m_failed} 失败, {m_passed + m_failed} 总计");
            Console.WriteLine("========================================");
            //Console.ReadLine();
            Environment.Exit(m_failed > 0 ? 1 : 0);
        }

        static void Assert(string name, bool condition)
        {
            if (condition)
            {
                Console.WriteLine($"  [PASS] {name}");
                m_passed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {name}");
                m_failed++;
            }
        }

        static void AssertEqual<T>(string name, T expected, T actual)
        {
            if (object.Equals(expected, actual))
            {
                Console.WriteLine($"  [PASS] {name}");
                m_passed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {name} (期望: {expected}, 实际: {actual})");
                m_failed++;
            }
        }

        static void AssertNoThrow(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"  [PASS] {name}");
                m_passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name} ({ex.GetType().Name}: {ex.Message})");
                m_failed++;
            }
        }

        #region AOT 注册与工厂测试

        static void TestAotRegistration()
        {
            Console.WriteLine("\n── 1. AOT 类型注册测试 ──────────────────────");

            SimpleJson.InitializeCommonAotTypes();

            var dictFactory = SimpleJson.GetRegisteredAotFactory(typeof(Dictionary<string, object>));
            Assert("Dictionary<string,object> 工厂已注册", dictFactory != null);

            var listFactory = SimpleJson.GetRegisteredAotFactory(typeof(List<int>));
            Assert("List<int> 工厂已注册", listFactory != null);

            var dictType = SimpleJson.GetRegisteredAotType(typeof(Dictionary<string, object>).FullName);
            Assert("通过 FullName 查找 Dictionary 类型成功", dictType != null);

            SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
            var pocoFactory = SimpleJson.GetRegisteredAotFactory(typeof(AotTestPoco));
            Assert("自定义 POCO 工厂注册成功", pocoFactory != null);

            var unknownFactory = SimpleJson.GetRegisteredAotFactory(typeof(UnknownPoco));
            Assert("未注册类型返回 null", unknownFactory == null);

            var nestedListFactory = SimpleJson.GetRegisteredAotFactory(typeof(List<List<string>>));
            Assert("List<List<string>> 工厂已注册", nestedListFactory != null);

            var nestedDictFactory = SimpleJson.GetRegisteredAotFactory(typeof(Dictionary<string, List<string>>));
            Assert("Dictionary<string,List<string>> 工厂已注册", nestedDictFactory != null);
        }

        static void TestAotTypeCreation()
        {
            Console.WriteLine("\n── 2. AOT 类型创建测试 ──────────────────────");

            AssertNoThrow("SafeCreateInstance 创建已注册 POCO", () =>
            {
                var instance = SimpleJson.SafeCreateInstance(typeof(AotTestPoco));
                if (instance == null || !(instance is AotTestPoco))
                    throw new Exception("创建结果不是 AotTestPoco");
            });

            AssertNoThrow("SafeCreateList 创建 List<int>", () =>
            {
                var list = SimpleJson.SafeCreateList(typeof(int));
                if (list == null) throw new Exception("创建结果为 null");
                list.Add(42);
                if (list.Count != 1 || (int)list[0] != 42)
                    throw new Exception("List 功能异常");
            });

            AssertNoThrow("SafeCreateInstance 创建已注册 Dictionary", () =>
            {
                var dict = SimpleJson.SafeCreateInstance(typeof(Dictionary<string, string>));
                if (dict == null || !(dict is Dictionary<string, string>))
                    throw new Exception("创建结果不是 Dictionary<string,string>");
            });

            AssertNoThrow("SafeCreateList 创建 List<string>", () =>
            {
                var list = SimpleJson.SafeCreateList(typeof(string));
                if (list == null) throw new Exception("创建结果为 null");
            });

            AssertNoThrow("SafeCreateList 创建 List<double>", () =>
            {
                var list = SimpleJson.SafeCreateList(typeof(double));
                if (list == null) throw new Exception("创建结果为 null");
            });
        }

        static void TestSafeCreateDictionary()
        {
            Console.WriteLine("\n── 3. SafeCreateDictionary 测试 ──────────────────────");

            AssertNoThrow("SafeCreateDictionary 创建 Dictionary<string,int>", () =>
            {
                var dict = SimpleJson.SafeCreateDictionary(typeof(string), typeof(int));
                if (dict == null) throw new Exception("创建结果为 null");
                dict["test"] = 42;
                if ((int)dict["test"] != 42) throw new Exception("字典功能异常");
            });

            AssertNoThrow("SafeCreateDictionary 创建 Dictionary<string,string>", () =>
            {
                var dict = SimpleJson.SafeCreateDictionary(typeof(string), typeof(string));
                if (dict == null) throw new Exception("创建结果为 null");
            });

            Assert("SafeCreateDictionary 创建未注册泛型组合返回非null",
                SimpleJson.SafeCreateDictionary(typeof(string), typeof(object)) != null);
        }

        #endregion

        #region 基础类型反序列化测试

        static void TestBasicPocoDeserialization()
        {
            Console.WriteLine("\n── 4. 基础 POCO 反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());

            AssertNoThrow("简单 POCO 反序列化", () =>
            {
                string json = @"{""Name"":""测试"",""Value"":123}";
                var result = SimpleJson.DeserializeObject<AotTestPoco>(json);
                if (result == null || result.Name != "测试" || result.Value != 123)
                    throw new Exception($"Name={result?.Name}, Value={result?.Value}");
            });

            SimpleJson.RegisterAotType(typeof(TypedPoco), () => new TypedPoco());
            AssertNoThrow("多类型 POCO 反序列化", () =>
            {
                string json = @"{""IntVal"":42,""DoubleVal"":3.14,""BoolVal"":true,""StringVal"":""hello"",""NullableVal"":null}";
                var result = SimpleJson.DeserializeObject<TypedPoco>(json);
                if (result == null || result.IntVal != 42 ||
                    Math.Abs(result.DoubleVal - 3.14) > 0.001 ||
                    result.BoolVal != true || result.StringVal != "hello" ||
                    result.NullableVal != null)
                    throw new Exception("字段值不匹配");
            });

            AssertNoThrow("多类型 POCO 含非空 Nullable", () =>
            {
                string json = @"{""IntVal"":0,""DoubleVal"":0,""BoolVal"":false,""StringVal"":null,""NullableVal"":99}";
                var result = SimpleJson.DeserializeObject<TypedPoco>(json);
                if (result == null || result.NullableVal != 99)
                    throw new Exception($"NullableVal={result?.NullableVal}");
            });
        }

        static void TestEnumDeserialization()
        {
            Console.WriteLine("\n── 5. 枚举反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(EnumPoco), () => new EnumPoco());

            AssertNoThrow("枚举值反序列化（整数值）", () =>
            {
                string json = @"{""Status"":1}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                if (result == null || result.Status != TestStatus.Active)
                    throw new Exception($"Status={result?.Status}");
            });

            AssertNoThrow("枚举值反序列化（字符串值）", () =>
            {
                string json = @"{""Status"":""Active""}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                if (result == null || result.Status != TestStatus.Active)
                    throw new Exception($"Status={result?.Status}");
            });

            AssertNoThrow("枚举值反序列化（默认值0）", () =>
            {
                string json = @"{""Status"":0}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                if (result == null || result.Status != TestStatus.None)
                    throw new Exception($"Status={result?.Status}");
            });
        }

        static void TestNullableDeserialization()
        {
            Console.WriteLine("\n── 6. Nullable 类型反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(NullablePoco), () => new NullablePoco());

            AssertNoThrow("Nullable int 有值", () =>
            {
                string json = @"{""IntVal"":42,""DoubleVal"":3.14,""BoolVal"":true,""DateTimeVal"":null}";
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                if (result == null || result.IntVal != 42 || result.DoubleVal != 3.14 || result.BoolVal != true)
                    throw new Exception("Nullable 有值不匹配");
            });

            AssertNoThrow("Nullable 全部为 null", () =>
            {
                string json = @"{""IntVal"":null,""DoubleVal"":null,""BoolVal"":null,""DateTimeVal"":null}";
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                if (result == null || result.IntVal != null || result.DoubleVal != null || result.BoolVal != null)
                    throw new Exception("Nullable null 不匹配");
            });
        }

        static void TestDateTimeDeserialization()
        {
            Console.WriteLine("\n── 7. DateTime 反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(DateTimePoco), () => new DateTimePoco());

            AssertNoThrow("DateTime ISO8601 反序列化", () =>
            {
                string json = @"{""CreatedAt"":""2024-01-15T10:30:00""}";
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                if (result == null || result.CreatedAt.Value.Year != 2024 || result.CreatedAt.Value.Month != 1)
                    throw new Exception($"CreatedAt={result?.CreatedAt}");
            });

            AssertNoThrow("DateTime 为 null", () =>
            {
                string json = @"{""CreatedAt"":null}";
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                if (result == null || result.CreatedAt != null)
                    throw new Exception("应为 null");
            });
        }

        #endregion

        #region 集合反序列化测试

        static void TestCollectionDeserialization()
        {
            Console.WriteLine("\n── 8. 集合反序列化测试 ──────────────────────");

            AssertNoThrow("List<int> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<List<int>>("[1,2,3,4,5]");
                if (result == null || result.Count != 5 || result[2] != 3)
                    throw new Exception("List<int> 不匹配");
            });

            AssertNoThrow("List<string> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<List<string>>(@"[""a"",""b"",""c""]");
                if (result == null || result.Count != 3 || result[1] != "b")
                    throw new Exception("List<string> 不匹配");
            });

            AssertNoThrow("List<double> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<List<double>>("[1.1,2.2,3.3]");
                if (result == null || result.Count != 3)
                    throw new Exception("List<double> 不匹配");
            });

            AssertNoThrow("List<bool> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<List<bool>>("[true,false,true]");
                if (result == null || result.Count != 3 || result[1] != false)
                    throw new Exception("List<bool> 不匹配");
            });

            AssertNoThrow("List<POCO> 反序列化", () =>
            {
                SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
                SimpleJson.RegisterAotType(typeof(List<AotTestPoco>), () => new List<AotTestPoco>());
                string json = @"[{""Name"":""A"",""Value"":1},{""Name"":""B"",""Value"":2}]";
                var result = SimpleJson.DeserializeObject<List<AotTestPoco>>(json);
                if (result == null || result.Count != 2 || result[0].Name != "A" || result[1].Value != 2)
                    throw new Exception("List<POCO> 不匹配");
            });

            AssertNoThrow("List<List<int>> 反序列化", () =>
            {
                string json = "[[1,2],[3,4,5]]";
                var result = SimpleJson.DeserializeObject<List<List<int>>>(json);
                if (result == null || result.Count != 2 || result[0].Count != 2 || result[1][2] != 5)
                    throw new Exception("List<List<int>> 不匹配");
            });
        }

        static void TestDictionaryDeserialization()
        {
            Console.WriteLine("\n── 9. 字典反序列化测试 ──────────────────────");

            AssertNoThrow("Dictionary<string,string> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, string>>(@"{""key1"":""value1"",""key2"":""value2""}");
                if (result == null || result.Count != 2 || result["key1"] != "value1")
                    throw new Exception("Dictionary<string,string> 不匹配");
            });

            AssertNoThrow("Dictionary<string,int> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, int>>(@"{""a"":1,""b"":2,""c"":3}");
                if (result == null || result["b"] != 2)
                    throw new Exception("Dictionary<string,int> 不匹配");
            });

            AssertNoThrow("Dictionary<string,object> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, object>>(@"{""name"":""test"",""count"":42,""active"":true}");
                if (result == null || result["name"].ToString() != "test")
                    throw new Exception("Dictionary<string,object> 不匹配");
            });

            AssertNoThrow("Dictionary<string,double> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, double>>(@"{""pi"":3.14,""e"":2.71}");
                if (result == null || Math.Abs(result["pi"] - 3.14) > 0.01)
                    throw new Exception("Dictionary<string,double> 不匹配");
            });

            AssertNoThrow("Dictionary<string,bool> 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, bool>>(@"{""a"":true,""b"":false}");
                if (result == null || result["a"] != true || result["b"] != false)
                    throw new Exception("Dictionary<string,bool> 不匹配");
            });

            AssertNoThrow("Dictionary<string,List<string>> 反序列化", () =>
            {
                string json = @"{""tags"":[""a"",""b""],""ids"":[""1"",""2""]}";
                var result = SimpleJson.DeserializeObject<Dictionary<string, List<string>>>(json);
                if (result == null || result["tags"].Count != 2 || result["tags"][0] != "a")
                    throw new Exception("Dictionary<string,List<string>> 不匹配");
            });
        }

        #endregion

        #region 嵌套对象反序列化测试

        static void TestNestedPocoDeserialization()
        {
            Console.WriteLine("\n── 10. 嵌套 POCO 反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(AotNestedPoco), () => new AotNestedPoco());
            SimpleJson.RegisterAotType(typeof(AotParentPoco), () => new AotParentPoco());

            AssertNoThrow("两层嵌套 POCO 反序列化", () =>
            {
                string json = @"{""Name"":""父对象"",""Child"":{""Name"":""子对象"",""Value"":99}}";
                var result = SimpleJson.DeserializeObject<AotParentPoco>(json);
                if (result == null || result.Name != "父对象" ||
                    result.Child == null || result.Child.Name != "子对象" || result.Child.Value != 99)
                    throw new Exception("两层嵌套不匹配");
            });

            SimpleJson.RegisterAotType(typeof(DeepChild), () => new DeepChild());
            SimpleJson.RegisterAotType(typeof(DeepParent), () => new DeepParent());
            SimpleJson.RegisterAotType(typeof(DeepGrandParent), () => new DeepGrandParent());

            AssertNoThrow("三层嵌套 POCO 反序列化", () =>
            {
                string json = @"{""Name"":""G"",""Child"":{""Name"":""P"",""Child"":{""Name"":""C"",""Value"":77}}}";
                var result = SimpleJson.DeserializeObject<DeepGrandParent>(json);
                if (result == null || result.Name != "G" ||
                    result.Child == null || result.Child.Name != "P" ||
                    result.Child.Child == null || result.Child.Child.Name != "C" || result.Child.Child.Value != 77)
                    throw new Exception("三层嵌套不匹配");
            });
        }

        static void TestComplexNestedDeserialization()
        {
            Console.WriteLine("\n── 11. 复杂嵌套反序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(ComplexPoco), () => new ComplexPoco());
            SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
            SimpleJson.RegisterAotType(typeof(List<AotTestPoco>), () => new List<AotTestPoco>());
            SimpleJson.RegisterAotType(typeof(Dictionary<string, AotTestPoco>), () => new Dictionary<string, AotTestPoco>());

            AssertNoThrow("含 List 和 Dictionary 的复杂 POCO", () =>
            {
                string json = @"{""Name"":""complex"",""Items"":[{""Name"":""A"",""Value"":1}],""Map"":{""key"":{""Name"":""B"",""Value"":2}}}";
                var result = SimpleJson.DeserializeObject<ComplexPoco>(json);
                if (result == null || result.Name != "complex")
                    throw new Exception("复杂 POCO Name 不匹配");
                if (result.Items == null || result.Items.Count != 1 || result.Items[0].Name != "A")
                    throw new Exception("复杂 POCO Items 不匹配");
                if (result.Map == null || !result.Map.ContainsKey("key") || result.Map["key"].Value != 2)
                    throw new Exception("复杂 POCO Map 不匹配");
            });

            SimpleJson.RegisterAotType(typeof(PocoWithList), () => new PocoWithList());
            SimpleJson.RegisterAotType(typeof(List<int>), () => new List<int>());

            AssertNoThrow("含 List<int> 的 POCO", () =>
            {
                string json = @"{""Name"":""listPoco"",""Numbers"":[10,20,30]}";
                var result = SimpleJson.DeserializeObject<PocoWithList>(json);
                if (result == null || result.Name != "listPoco" ||
                    result.Numbers == null || result.Numbers.Count != 3 || result.Numbers[1] != 20)
                    throw new Exception("含 List<int> 的 POCO 不匹配");
            });

            SimpleJson.RegisterAotType(typeof(PocoWithDict), () => new PocoWithDict());
            SimpleJson.RegisterAotType(typeof(Dictionary<string, string>), () => new Dictionary<string, string>());

            AssertNoThrow("含 Dictionary<string,string> 的 POCO", () =>
            {
                string json = @"{""Name"":""dictPoco"",""Props"":{""color"":""red"",""size"":""large""}}";
                var result = SimpleJson.DeserializeObject<PocoWithDict>(json);
                if (result == null || result.Name != "dictPoco" ||
                    result.Props == null || result.Props["color"] != "red")
                    throw new Exception("含 Dictionary<string,string> 的 POCO 不匹配");
            });
        }

        #endregion

        #region 序列化测试

        static void TestSerialization()
        {
            Console.WriteLine("\n── 12. 序列化测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());

            AssertNoThrow("POCO 序列化", () =>
            {
                var poco = new AotTestPoco { Name = "序列化测试", Value = 999 };
                string json = SimpleJson.SerializeObject(poco);
                if (json == null || !json.Contains("序列化测试") || !json.Contains("999"))
                    throw new Exception($"json={json}");
            });

            AssertNoThrow("List<int> 序列化", () =>
            {
                var list = new List<int> { 1, 2, 3 };
                string json = SimpleJson.SerializeObject(list);
                if (json == null || !json.Contains("1") || !json.Contains("3"))
                    throw new Exception($"json={json}");
            });

            AssertNoThrow("Dictionary<string,int> 序列化", () =>
            {
                var dict = new Dictionary<string, int> { { "x", 10 }, { "y", 20 } };
                string json = SimpleJson.SerializeObject(dict);
                if (json == null || !json.Contains("x") || !json.Contains("10"))
                    throw new Exception($"json={json}");
            });

            AssertNoThrow("枚举序列化", () =>
            {
                SimpleJson.RegisterAotType(typeof(EnumPoco), () => new EnumPoco());
                var poco = new EnumPoco { Status = TestStatus.Active };
                string json = SimpleJson.SerializeObject(poco);
                var poco2 = SimpleJson.DeserializeObject<EnumPoco>(json);
                if (json == null || poco2.Status!= TestStatus.Active)
                    throw new Exception($"json={json}");
            });

            AssertNoThrow("null 值序列化", () =>
            {
                SimpleJson.RegisterAotType(typeof(NullablePoco), () => new NullablePoco());
                var poco = new NullablePoco();
                string json = SimpleJson.SerializeObject(poco);
                if (json == null)
                    throw new Exception("序列化结果为 null");
            });
        }

        static void TestRoundTrip()
        {
            Console.WriteLine("\n── 13. 往返测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(TypedPoco), () => new TypedPoco());

            AssertNoThrow("TypedPoco 往返测试", () =>
            {
                var original = new TypedPoco { IntVal = 42, DoubleVal = 3.14, BoolVal = true, StringVal = "hello", NullableVal = 99 };
                string json = SimpleJson.SerializeObject(original);
                var roundtrip = SimpleJson.DeserializeObject<TypedPoco>(json);
                if (roundtrip == null || roundtrip.IntVal != 42 ||
                    Math.Abs(roundtrip.DoubleVal - 3.14) > 0.001 ||
                    roundtrip.BoolVal != true || roundtrip.StringVal != "hello" ||
                    roundtrip.NullableVal != 99)
                    throw new Exception("往返数据不匹配");
            });

            SimpleJson.RegisterAotType(typeof(AotParentPoco), () => new AotParentPoco());
            SimpleJson.RegisterAotType(typeof(AotNestedPoco), () => new AotNestedPoco());

            AssertNoThrow("嵌套 POCO 往返测试", () =>
            {
                var original = new AotParentPoco
                {
                    Name = "parent",
                    Child = new AotNestedPoco { Name = "child", Value = 42 }
                };
                string json = SimpleJson.SerializeObject(original);
                var roundtrip = SimpleJson.DeserializeObject<AotParentPoco>(json);
                if (roundtrip == null || roundtrip.Name != "parent" ||
                    roundtrip.Child == null || roundtrip.Child.Name != "child" || roundtrip.Child.Value != 42)
                    throw new Exception("嵌套往返数据不匹配");
            });

            AssertNoThrow("List<int> 往返测试", () =>
            {
                var original = new List<int> { 1, 2, 3, 4, 5 };
                string json = SimpleJson.SerializeObject(original);
                var roundtrip = SimpleJson.DeserializeObject<List<int>>(json);
                if (roundtrip == null || roundtrip.Count != 5 || roundtrip[2] != 3)
                    throw new Exception("List<int> 往返不匹配");
            });

            AssertNoThrow("Dictionary<string,int> 往返测试", () =>
            {
                var original = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
                string json = SimpleJson.SerializeObject(original);
                var roundtrip = SimpleJson.DeserializeObject<Dictionary<string, int>>(json);
                if (roundtrip == null || roundtrip.Count != 2 || roundtrip["a"] != 1)
                    throw new Exception("Dictionary<string,int> 往返不匹配");
            });

            SimpleJson.RegisterAotType(typeof(EnumPoco), () => new EnumPoco());

            AssertNoThrow("枚举往返测试", () =>
            {
                var original = new EnumPoco { Status = TestStatus.Active };
                string json = SimpleJson.SerializeObject(original);
                var roundtrip = SimpleJson.DeserializeObject<EnumPoco>(json);
                if (roundtrip == null || roundtrip.Status != TestStatus.Active)
                    throw new Exception("枚举往返不匹配");
            });
        }

        #endregion

        #region 边界情况测试

        static void TestEdgeCases()
        {
            Console.WriteLine("\n── 14. 边界情况测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(EmptyPoco), () => new EmptyPoco());

            AssertNoThrow("空对象反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<EmptyPoco>("{}");
                if (result == null) throw new Exception("结果为 null");
            });

            AssertNoThrow("空列表反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<List<int>>("[]");
                if (result == null || result.Count != 0) throw new Exception("空列表不匹配");
            });

            AssertNoThrow("空字典反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<Dictionary<string, string>>("{}");
                if (result == null || result.Count != 0) throw new Exception("空字典不匹配");
            });

            SimpleJson.RegisterAotType(typeof(NullPoco), () => new NullPoco());

            AssertNoThrow("null 值字段反序列化", () =>
            {
                string json = @"{""Name"":null,""Value"":null}";
                var result = SimpleJson.DeserializeObject<NullPoco>(json);
                if (result == null || result.Name != null) throw new Exception("null 值不匹配");
            });

            AssertNoThrow("JSON 多余字段忽略", () =>
            {
                SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
                string json = @"{""Name"":""test"",""Value"":1,""Extra"":""ignored""}";
                var result = SimpleJson.DeserializeObject<AotTestPoco>(json);
                if (result == null || result.Name != "test" || result.Value != 1)
                    throw new Exception("多余字段处理异常");
            });

            AssertNoThrow("Unicode 字符串", () =>
            {
                SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
                string json = @"{""Name"":""中文测试🚀"",""Value"":0}";
                var result = SimpleJson.DeserializeObject<AotTestPoco>(json);
                if (result == null || !result.Name.Contains("中文") || !result.Name.Contains("🚀"))
                    throw new Exception($"Unicode 不匹配: {result?.Name}");
            });

            AssertNoThrow("大整数反序列化", () =>
            {
                SimpleJson.RegisterAotType(typeof(TypedPoco), () => new TypedPoco());
                string json = @"{""IntVal"":2147483647,""DoubleVal"":0,""BoolVal"":false,""StringVal"":null,""NullableVal"":null}";
                var result = SimpleJson.DeserializeObject<TypedPoco>(json);
                if (result == null || result.IntVal != int.MaxValue)
                    throw new Exception($"大整数不匹配: {result?.IntVal}");
            });

            AssertNoThrow("负数反序列化", () =>
            {
                string json = @"{""IntVal"":-42,""DoubleVal"":-3.14,""BoolVal"":false,""StringVal"":null,""NullableVal"":-99}";
                var result = SimpleJson.DeserializeObject<TypedPoco>(json);
                if (result == null || result.IntVal != -42 || result.NullableVal != -99)
                    throw new Exception("负数不匹配");
            });
        }

        #endregion

        #region Type 键查找测试

        static void TestFactoryByTypeLookup()
        {
            Console.WriteLine("\n── 15. Type 键查找测试 ──────────────────────");

            var dictFactory = SimpleJson.GetRegisteredAotFactory(typeof(Dictionary<string, int>));
            Assert("通过 Type 键查找 Dictionary<string,int> 工厂成功", dictFactory != null);

            var listFactory = SimpleJson.GetRegisteredAotFactory(typeof(List<double>));
            Assert("通过 Type 键查找 List<double> 工厂成功", listFactory != null);

            AssertNoThrow("通过 Type 键创建 Dictionary<string,int>", () =>
            {
                var dictObj = SimpleJson.SafeCreateInstance(typeof(Dictionary<string, int>));
                if (dictObj == null || !(dictObj is Dictionary<string, int>))
                    throw new Exception("创建结果不是 Dictionary<string,int>");
            });

            AssertNoThrow("通过 Type 键创建 List<double>", () =>
            {
                var listObj = SimpleJson.SafeCreateInstance(typeof(List<double>));
                if (listObj == null || !(listObj is List<double>))
                    throw new Exception("创建结果不是 List<double>");
            });
        }

        #endregion

        #region 非泛型 API 测试

        static void TestNonGenericDeserialize()
        {
            Console.WriteLine("\n── 16. 非泛型 DeserializeObject 测试 ──────────────────────");

            AssertNoThrow("非泛型反序列化基础类型", () =>
            {
                var result = SimpleJson.DeserializeObject("42", typeof(int));
                if (result == null || !result.Equals(42))
                    throw new Exception($"结果: {result}");
            });

            AssertNoThrow("非泛型反序列化字符串", () =>
            {
                var result = SimpleJson.DeserializeObject(@"""hello""", typeof(string));
                if (result == null || result.ToString() != "hello")
                    throw new Exception($"结果: {result}");
            });

            AssertNoThrow("非泛型反序列化 List", () =>
            {
                var result = SimpleJson.DeserializeObject("[1,2,3]", typeof(List<int>));
                if (result == null) throw new Exception("结果为 null");
                var list = result as List<int>;
                if (list == null || list.Count != 3 || list[1] != 2)
                    throw new Exception("List 不匹配");
            });

            AssertNoThrow("非泛型反序列化 Dictionary", () =>
            {
                var result = SimpleJson.DeserializeObject(@"{""a"":1}", typeof(Dictionary<string, int>));
                if (result == null) throw new Exception("结果为 null");
                var dict = result as Dictionary<string, int>;
                if (dict == null || dict["a"] != 1)
                    throw new Exception("Dictionary 不匹配");
            });

            AssertNoThrow("非泛型反序列化 POCO", () =>
            {
                SimpleJson.RegisterAotType(typeof(AotTestPoco), () => new AotTestPoco());
                var result = SimpleJson.DeserializeObject(@"{""Name"":""test"",""Value"":42}", typeof(AotTestPoco));
                if (result == null) throw new Exception("结果为 null");
                var poco = result as AotTestPoco;
                if (poco == null || poco.Name != "test" || poco.Value != 42)
                    throw new Exception("POCO 不匹配");
            });
        }

        #endregion

        #region DataContract 测试

        static void TestDataContractSerialization()
        {
            Console.WriteLine("\n── 17. DataContract 序列化测试 ──────────────────────");

            var savedStrategy = SimpleJson.CurrentJsonSerializerStrategy;
            SimpleJson.CurrentJsonSerializerStrategy = new DataContractSerializationStrategy();
            SimpleJson.RegisterAotType(typeof(DataContractPoco), () => new DataContractPoco());

            AssertNoThrow("DataContract 序列化只输出 DataMember", () =>
            {
                var poco = new DataContractPoco { Id = 1, Name = "test", Secret = "hidden" };
                string json = SimpleJson.SerializeObject(poco);
                if (json == null || !json.Contains("Id") || !json.Contains("Name"))
                    throw new Exception($"DataContract 序列化失败: {json}");
                if (json.Contains("Secret"))
                    throw new Exception($"Secret 不应出现在序列化结果中: {json}");
            });

            AssertNoThrow("DataContract 反序列化只读取 DataMember", () =>
            {
                string json = @"{""Id"":1,""Name"":""test"",""Secret"":""should_ignore""}";
                var result = SimpleJson.DeserializeObject<DataContractPoco>(json);
                if (result == null || result.Id != 1 || result.Name != "test")
                    throw new Exception("DataContract 反序列化不匹配");
                if (result.Secret != null)
                    throw new Exception($"Secret 应为 null（无 DataMember）: {result.Secret}");
            });

            SimpleJson.CurrentJsonSerializerStrategy = savedStrategy;
        }

        #endregion

        #region JsonIgnore 测试

        static void TestJsonIgnoreAttribute()
        {
            Console.WriteLine("\n── 18. JsonIgnore 特性测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(JsonIgnorePoco), () => new JsonIgnorePoco());

            AssertNoThrow("JsonIgnore 字段不序列化", () =>
            {
                var poco = new JsonIgnorePoco { Name = "test", Password = "secret", Age = 25 };
                string json = SimpleJson.SerializeObject(poco);
                if (json == null || !json.Contains("Name") || !json.Contains("Age"))
                    throw new Exception($"JsonIgnore 序列化失败: {json}");
                if (json.Contains("Password"))
                    throw new Exception($"Password 不应出现在序列化结果中: {json}");
            });

            AssertNoThrow("JsonIgnore 字段不反序列化", () =>
            {
                string json = @"{""Name"":""test"",""Password"":""hacked"",""Age"":30}";
                var result = SimpleJson.DeserializeObject<JsonIgnorePoco>(json);
                if (result == null || result.Name != "test" || result.Age != 30)
                    throw new Exception("JsonIgnore 反序列化不匹配");
                if (result.Password != null)
                    throw new Exception($"Password 应为 null: {result.Password}");
            });
        }

        #endregion

        #region JsonAlias 测试

        static void TestJsonAliasAttribute()
        {
            Console.WriteLine("\n── 19. JsonAlias 特性测试 ──────────────────────");

            SimpleJson.RegisterAotType(typeof(JsonAliasPoco), () => new JsonAliasPoco());

            AssertNoThrow("JsonAlias 反序列化使用别名", () =>
            {
                string json = @"{""n"":""alias_test"",""v"":42}";
                var result = SimpleJson.DeserializeObject<JsonAliasPoco>(json);
                if (result == null || result.Name != "alias_test" || result.Value != 42)
                    throw new Exception($"JsonAlias 反序列化不匹配: Name={result?.Name}, Value={result?.Value}");
            });

            AssertNoThrow("JsonAlias 反序列化使用原始名", () =>
            {
                string json = @"{""Name"":""original"",""Value"":99}";
                var result = SimpleJson.DeserializeObject<JsonAliasPoco>(json);
                if (result == null || result.Name != "original" || result.Value != 99)
                    throw new Exception("JsonAlias 原始名反序列化不匹配");
            });
        }

        #endregion

        #region 数组反序列化测试

        static void TestArrayDeserialization()
        {
            Console.WriteLine("\n── 20. 数组反序列化测试 ──────────────────────");

            AssertNoThrow("int[] 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<int[]>("[10,20,30]");
                if (result == null || result.Length != 3 || result[1] != 20)
                    throw new Exception("int[] 不匹配");
            });

            AssertNoThrow("string[] 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<string[]>(@"[""a"",""b"",""c""]");
                if (result == null || result.Length != 3 || result[1] != "b")
                    throw new Exception("string[] 不匹配");
            });

            AssertNoThrow("double[] 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<double[]>("[1.1,2.2,3.3]");
                if (result == null || result.Length != 3)
                    throw new Exception("double[] 不匹配");
            });

            AssertNoThrow("bool[] 反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<bool[]>("[true,false]");
                if (result == null || result.Length != 2 || result[1] != false)
                    throw new Exception("bool[] 不匹配");
            });

            AssertNoThrow("空数组反序列化", () =>
            {
                var result = SimpleJson.DeserializeObject<int[]>("[]");
                if (result == null || result.Length != 0)
                    throw new Exception("空数组不匹配");
            });
        }

        #endregion
    }

    #region 测试数据类型

    public class AotTestPoco
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class TypedPoco
    {
        public int IntVal { get; set; }
        public double DoubleVal { get; set; }
        public bool BoolVal { get; set; }
        public string StringVal { get; set; }
        public int? NullableVal { get; set; }
    }

    public enum TestStatus
    {
        None = 0,
        Active = 1,
        Inactive = 2
    }

    public class EnumPoco
    {
        public TestStatus Status { get; set; }
    }

    public class NullablePoco
    {
        public int? IntVal { get; set; }
        public double? DoubleVal { get; set; }
        public bool? BoolVal { get; set; }
        public DateTime? DateTimeVal { get; set; }
    }

    public class DateTimePoco
    {
        public DateTime? CreatedAt { get; set; }
    }

    public class AotNestedPoco
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class AotParentPoco
    {
        public string Name { get; set; }
        public AotNestedPoco Child { get; set; }
    }

    public class DeepChild
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class DeepParent
    {
        public string Name { get; set; }
        public DeepChild Child { get; set; }
    }

    public class DeepGrandParent
    {
        public string Name { get; set; }
        public DeepParent Child { get; set; }
    }

    public class ComplexPoco
    {
        public string Name { get; set; }
        public List<AotTestPoco> Items { get; set; }
        public Dictionary<string, AotTestPoco> Map { get; set; }
    }

    public class PocoWithList
    {
        public string Name { get; set; }
        public List<int> Numbers { get; set; }
    }

    public class PocoWithDict
    {
        public string Name { get; set; }
        public Dictionary<string, string> Props { get; set; }
    }

    public class EmptyPoco
    {
    }

    public class NullPoco
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class UnknownPoco
    {
        public int Id { get; set; }
    }

    [DataContract]
    public class DataContractPoco
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        public string Secret { get; set; }
    }

    public class JsonIgnorePoco
    {
        public string Name { get; set; }

        [JsonIgnore]
        public string Password { get; set; }

        public int Age { get; set; }
    }

    public class JsonAliasPoco
    {
        [JsonAlias("n")]
        public string Name { get; set; }

        [JsonAlias("v")]
        public int Value { get; set; }
    }

    #endregion
}
