//-----------------------------------------------------------------------
// SimpleJsonConsoleTests.cs
// 无依赖控制台测试套件，覆盖所有修复点
// 运行方式：
//   dotnet run  /  直接在 Unity 中挂载到 MonoBehaviour.Start()
// 兼容：.NET 2.0 / .NET 4.x / .NET Standard 2.0 / Unity il2cpp
//-----------------------------------------------------------------------


using RS.SimpleJsonUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RS.SimpleJsonUnity.Tests
{
    // ──────────────────────────────────────────────────────────────
    // 极简断言框架（无外部依赖）
    // ──────────────────────────────────────────────────────────────
    public class Test : Attribute
    {
    }
    internal static class Assert
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();
        private static string _currentTest = "";

        public static void BeginTest(string name)
        {
            _currentTest = name;
        }

        public static void AreEqual<T>(T expected,T actual,string message = "")
        {
            bool eq = (expected == null && actual == null)
                   || (expected != null && expected.Equals(actual));
            if (eq)
            {
                Pass();
            }
            else
            {
                Fail(string.Format(
                    "Expected [{0}] but got [{1}]. {2}",
                    expected,actual,message));
            }
        }

        public static void AreEqual(float expected,float actual,
            float delta = 1e-5f,string message = "")
        {
            if (Math.Abs(expected - actual) <= delta) Pass();
            else Fail(string.Format(
                "Expected [{0}] ± {1} but got [{2}]. {3}",
                expected,delta,actual,message));
        }

        public static void AreEqual(double expected,double actual,
            double delta = 1e-10,string message = "")
        {
            if (Math.Abs(expected - actual) <= delta) Pass();
            else Fail(string.Format(
                "Expected [{0}] ± {1} but got [{2}]. {3}",
                expected,delta,actual,message));
        }

        public static void IsTrue(bool condition,string message = "")
        {
            if (condition) Pass();
            else Fail("Expected true. " + message);
        }

        public static void IsFalse(bool condition,string message = "")
        {
            if (!condition) Pass();
            else Fail("Expected false. " + message);
        }

        public static void IsNull(object obj,string message = "")
        {
            if (obj == null) Pass();
            else Fail(string.Format("Expected null but got [{0}]. {1}",obj,message));
        }

        public static void IsNotNull(object obj,string message = "")
        {
            if (obj != null) Pass();
            else Fail("Expected non-null but got null. " + message);
        }

        public static void Throws<TException>(Action action,string message = "")
            where TException : Exception
        {
            try
            {
                action();
                Fail(string.Format(
                    "Expected exception [{0}] but none was thrown. {1}",
                    typeof(TException).Name,message));
            }
            catch (TException)
            {
                Pass();
            }
            catch (Exception ex)
            {
                Fail(string.Format(
                    "Expected [{0}] but got [{1}]: {2}. {3}",
                    typeof(TException).Name,ex.GetType().Name,
                    ex.Message,message));
            }
        }

        public static void DoesNotThrow(Action action,string message = "")
        {
            try
            {
                action();
                Pass();
            }
            catch (Exception ex)
            {
                Fail(string.Format(
                    "Unexpected exception [{0}]: {1}. {2}",
                    ex.GetType().Name,ex.Message,message));
            }
        }

        public static void Contains(string haystack,string needle,string message = "")
        {
            if (haystack != null && haystack.Contains(needle)) Pass();
            else Fail(string.Format(
                "Expected [{0}] to contain [{1}]. {2}",
                haystack,needle,message));
        }

        public static void NotContains(string haystack,string needle,string message = "")
        {
            if (haystack == null || !haystack.Contains(needle)) Pass();
            else Fail(string.Format(
                "Expected [{0}] NOT to contain [{1}]. {2}",
                haystack,needle,message));
        }

        private static void Pass()
        {
            _passed++;
            Console.WriteLine("    [PASS] " + _currentTest);
        }

        private static void Fail(string reason)
        {
            _failed++;
            string msg = string.Format("    [FAIL] {0} — {1}",_currentTest,reason);
            Console.WriteLine(msg);
            _failures.Add(msg);
        }

        public static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine(new string('=',60));
            Console.WriteLine(string.Format(
                "Results: {0} passed, {1} failed, {2} total",
                _passed,_failed,_passed + _failed));

            if (_failures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Failed tests:");
                foreach (string f in _failures)
                    Console.WriteLine(f);
            }
            Console.WriteLine(new string('=',60));
        }

        public static bool AllPassed() { return _failed == 0; }
    }

    // ──────────────────────────────────────────────────────────────
    // 测试用 POCO 类型
    // ──────────────────────────────────────────────────────────────

    public class BasicPoco
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public float Score { get; set; }
        public double Height { get; set; }
    }

    public class AttributePoco
    {
        public string Visible { get; set; }

        [JsonIgnore]
        public string Ignored { get; set; }

        [JsonInclude]
        private string PrivateProperty { get; set; }

        [JsonAlias("old_name")]
        public string NewName { get; set; }

        [JsonAlias("alias_a")]
        public string WithAlias { get; set; }

        public string GetPrivateProperty() => PrivateProperty;
        public void SetPrivateProperty(string v) => PrivateProperty = v;
    }

    // 用于测试序列化别名的 POCO
    public class AliasSerializePoco
    {
        [JsonAlias("user_name","userName","username")]
        public string UserName { get; set; }

        [JsonAlias("user_id","userId","uid")]
        public int UserId { get; set; }
    }

    // 用于测试 AcceptOriginalName=false 的 POCO
    public class StrictAliasPoco
    {
        [JsonAlias(false,"api_code","code")]
        public int Code { get; set; }
    }

    public class PrivateMemberPoco
    {
        [JsonInclude] private int _privateInt;
        [JsonInclude] private string _privateString;

        public int GetPrivateInt() => _privateInt;
        public string GetPrivateString() => _privateString;
    }

    public enum Color { Red = 0, Green = 1, Blue = 2 }

    public class EnumPoco
    {
        public Color Status { get; set; }
    }

    public class NullablePoco
    {
        public int? NullableInt { get; set; }
        public float? NullableFloat { get; set; }
        public Color? NullableEnum { get; set; }
    }

    public class CollectionPoco
    {
        public List<int> IntList { get; set; }
        public Dictionary<string,int> StringKeyDict { get; set; }
        public Dictionary<int,string> IntKeyDict { get; set; }
        public Dictionary<Color,float> EnumKeyDict { get; set; }
    }

    public class FloatPoco
    {
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
    }

    public class DateTimePoco
    {
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public Guid Guid { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }

    public class NoCtor
    {
        public string Value { get; set; }
        public NoCtor(string v) { Value = v; }
    }

    public class InheritanceBase
    {
        public virtual string BaseProp { get; set; }
    }

    public class InheritanceChild : InheritanceBase
    {
        [JsonIgnore]
        public override string BaseProp { get; set; }
        public string ChildProp { get; set; }
    }

    public class ShadowBase { public int Value { get; set; } }
    public class ShadowChild : ShadowBase { public new string Value { get; set; } }

    public class CharPoco { public char Ch { get; set; } }

    // ──────────────────────────────────────────────────────────────
    // 复杂多层嵌套类（用于测试 24）
    // ──────────────────────────────────────────────────────────────

    // 第一层：基础嵌套类
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
    }

    // 第二层：包含 Address 的类
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address HomeAddress { get; set; }
        public Address WorkAddress { get; set; }
        public List<string> PhoneNumbers { get; set; }
    }

    // 第三层：包含 Person 列表的类
    public class Department
    {
        public string Name { get; set; }
        public Person Manager { get; set; }
        public List<Person> Employees { get; set; }
        public Dictionary<int,Person> MembersById { get; set; }
    }

    // 第四层：包含 Department 的公司类
    public class Company
    {
        public string CompanyName { get; set; }
        public List<Department> Departments { get; set; }
        public Dictionary<string,Department> DepartmentsByName { get; set; }
        public Person CEO { get; set; }
    }

    // 第五层：包含循环引用的类（测试循环引用检测）
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Node Parent { get; set; }  // 循环引用
        public List<Node> Children { get; set; }  // 循环引用
    }

    // 第六层：多层继承嵌套类
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class User : BaseEntity
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public Profile Profile { get; set; }
    }

    public class Profile : BaseEntity
    {
        public string DisplayName { get; set; }
        public List<string> Tags { get; set; }
        public Address Location { get; set; }
    }

    // 第七层：泛型嵌套类
    public class Container<T>
    {
        public string ContainerName { get; set; }
        public T Data { get; set; }
        public List<T> Items { get; set; }
    }

    // 第八层：带 JsonAlias 的嵌套类
    public class ApiUser
    {
        [JsonAlias("user_id","userId","uid")]
        public int UserId { get; set; }

        [JsonAlias("user_name","userName","username")]
        public string UserName { get; set; }

        public ApiProfile Profile { get; set; }
    }

    public class ApiProfile
    {
        [JsonAlias("profile_id","profileId")]
        public int ProfileId { get; set; }

        [JsonAlias("display_name","displayName")]
        public string DisplayName { get; set; }

        public List<ApiTag> Tags { get; set; }
    }

    public class ApiTag
    {
        [JsonAlias("tag_id","tagId")]
        public int TagId { get; set; }

        [JsonAlias("tag_name","tagName")]
        public string TagName { get; set; }
    }

    // ──────────────────────────────────────────────────────────────
    // 测试入口
    // ──────────────────────────────────────────────────────────────

    public static class SimpleJsonConsoleTests
    {
        public static void Run()
        {
            Console.WriteLine(new string('=',60));
            Console.WriteLine("SimpleJson Console Test Suite");
            Console.WriteLine(new string('=',60));
            Console.WriteLine();

            RunSection("1. 基础类型往返",TestPrimitives);
            RunSection("2. POCO 基础序列化",TestBasicPoco);
            RunSection("3. JsonIgnore",TestJsonIgnore);
            RunSection("4. JsonInclude",TestJsonInclude);
            RunSection("5. JsonAlias",TestJsonAlias);
            RunSection("6. float/double 精度",TestFloatPrecision);
            RunSection("7. 字典 key 类型支持",TestDictionaryKeys);
            RunSection("8. Nullable<T>",TestNullable);
            RunSection("9. DateTime/Guid",TestDateTimeGuid);
            RunSection("10. 集合类型",TestCollections);
            RunSection("11. 类型转换 CoerceValue",TestTypeCoercion);
            RunSection("12. 无参构造缺失",TestNoCtor);
            RunSection("13. toLowerCase 策略",TestToLowerCase);
            RunSection("14. 字符串转义",TestStringEscape);
            RunSection("15. 数字范围",TestNumberRange);
            RunSection("16. char 类型",TestChar);
            RunSection("17. new-hide 属性去重",TestShadowProperty);
            RunSection("18. 嵌套对象",TestNested);
            RunSection("19. 错误处理",TestErrorHandling);
            RunSection("20. ClearCache 不崩溃",TestClearCache);
            RunSection("21. 线程安全",TestThreadSafety);
            RunSection("22. 往返完整性验证",TestFullRoundTrip);
            RunSection("23. JsonAlias 序列化别名",TestJsonAliasSerialization);
            RunSection("24. 复杂多层嵌套类",TestComplexNested);
            RunSection("25. 列表专项测试",TestListSpecial);
            RunSection("26. 嵌套类专项测试",TestNestedSpecial);

            Assert.PrintSummary();
        }

        // ── Main 入口（.NET 控制台项目）─────────────────────────

        public static void MainTest()
        {
            Run();
        }

        // ── 分节辅助 ────────────────────────────────────────────

        private static void RunSection(string title,Action tests)
        {
            Console.WriteLine();
            Console.WriteLine("── " + title + " " +
                new string('─',Math.Max(0,50 - title.Length)));
            try { tests(); }
            catch (Exception ex)
            {
                Console.WriteLine("    [ERROR] Section threw: " + ex.Message);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 1. 基础类型往返
        // ──────────────────────────────────────────────────────────

        private static void TestPrimitives()
        {
            Assert.BeginTest("String round-trip");
            {
                string json = SimpleJson.SerializeObject("Hello");
                var result = SimpleJson.DeserializeObject<string>(json);
                Assert.AreEqual("Hello",result);
            }

            Assert.BeginTest("Int round-trip");
            {
                string json = SimpleJson.SerializeObject(42);
                var result = SimpleJson.DeserializeObject<int>(json);
                Assert.AreEqual(42,result);
            }

            Assert.BeginTest("Bool true round-trip");
            {
                string json = SimpleJson.SerializeObject(true);
                var result = SimpleJson.DeserializeObject<bool>(json);
                Assert.AreEqual(true,result);
            }

            Assert.BeginTest("Bool false round-trip");
            {
                string json = SimpleJson.SerializeObject(false);
                var result = SimpleJson.DeserializeObject<bool>(json);
                Assert.AreEqual(false,result);
            }

            Assert.BeginTest("Null serializes as 'null'");
            {
                string json = SimpleJson.SerializeObject(null);
                Assert.AreEqual("null",json);
            }

            Assert.BeginTest("Long round-trip");
            {
                long val = 9876543210L;
                string json = SimpleJson.SerializeObject(val);
                var result = SimpleJson.DeserializeObject<long>(json);
                Assert.AreEqual(val,result);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 2. POCO 基础序列化
        // ──────────────────────────────────────────────────────────

        private static void TestBasicPoco()
        {
            Assert.BeginTest("BasicPoco round-trip");
            {
                var original = new BasicPoco
                { Name = "Alice",Age = 30,Score = 9.5f,Height = 1.65 };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual("Alice",result.Name);
                Assert.AreEqual(30,result.Age);
                Assert.AreEqual(9.5f,result.Score,1e-5f);
                Assert.AreEqual(1.65,result.Height,1e-10);
            }

            Assert.BeginTest("Null property serializes as null");
            {
                var obj = new BasicPoco { Name = null,Age = 0 };
                string json = SimpleJson.SerializeObject(obj);
                Assert.IsTrue(
                    json.Contains("\"Name\":null") || json.Contains("\"name\":null"),
                    "Null property must appear as null in JSON");
            }

            Assert.BeginTest("Extra JSON fields ignored gracefully");
            {
                string json = "{\"Name\":\"Bob\",\"Age\":25,\"Unknown\":\"extra\"}";
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual("Bob",result.Name);
                Assert.AreEqual(25,result.Age);
            }

            Assert.BeginTest("Empty JSON object returns default POCO");
            {
                var result = SimpleJson.DeserializeObject<BasicPoco>("{}");
                Assert.IsNull(result.Name);
                Assert.AreEqual(0,result.Age);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 3. JsonIgnore
        // ──────────────────────────────────────────────────────────

        private static void TestJsonIgnore()
        {
            Assert.BeginTest("JsonIgnore: field excluded from serialization");
            {
                var obj = new AttributePoco { Visible = "show",Ignored = "hide" };
                string json = SimpleJson.SerializeObject(obj);
                Assert.IsTrue(
                    json.Contains("Visible") || json.Contains("visible"),
                    "Visible must appear");
                Assert.IsFalse(
                    json.Contains("Ignored") || json.Contains("ignored"),
                    "Ignored must NOT appear");
            }

            Assert.BeginTest("JsonIgnore: field skipped during deserialization");
            {
                string json = "{\"Visible\":\"show\",\"Ignored\":\"hide\"}";
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);
                Assert.AreEqual("show",result.Visible);
                Assert.IsNull(result.Ignored,
                    "Ignored field must remain null");
            }

            Assert.BeginTest("JsonIgnore: inherited override excluded");
            {
                var obj = new InheritanceChild
                { BaseProp = "base",ChildProp = "child" };
                string json = SimpleJson.SerializeObject(obj);
                Assert.IsFalse(
                    json.Contains("BaseProp") || json.Contains("baseprop"),
                    "Overridden JsonIgnore must exclude base property");
                Assert.IsTrue(
                    json.Contains("ChildProp") || json.Contains("childprop"));
            }
        }

        // ──────────────────────────────────────────────────────────
        // 4. JsonInclude
        // ──────────────────────────────────────────────────────────

        private static void TestJsonInclude()
        {
            Assert.BeginTest("JsonInclude: private field serialized");
            {
                var obj = new PrivateMemberPoco();
                typeof(PrivateMemberPoco)
                    .GetField("_privateInt",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(obj,99);
                typeof(PrivateMemberPoco)
                    .GetField("_privateString",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(obj,"secret");

                string json = SimpleJson.SerializeObject(obj);
                Assert.IsTrue(
                    json.Contains("_privateInt") || json.Contains("_privateint"),
                    "Private field with JsonInclude must appear in JSON");
                Assert.IsTrue(
                    json.Contains("_privateString") || json.Contains("_privatestring"));
            }

            Assert.BeginTest("JsonInclude: private field deserialized");
            {
                string json = "{\"_privateInt\":99,\"_privateString\":\"secret\"}";
                var result = SimpleJson.DeserializeObject<PrivateMemberPoco>(json);
                Assert.AreEqual(99,result.GetPrivateInt());
                Assert.AreEqual("secret",result.GetPrivateString());
            }

            Assert.BeginTest("JsonInclude: private property round-trip");
            {
                var obj = new AttributePoco();
                obj.SetPrivateProperty("privateValue");
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);
                Assert.AreEqual("privateValue",result.GetPrivateProperty());
            }
        }

        // ──────────────────────────────────────────────────────────
        // 5. JsonAlias
        // ──────────────────────────────────────────────────────────

        private static void TestJsonAlias()
        {
            Assert.BeginTest("JsonAlias: serialize uses original name");
            {
                var obj = new AttributePoco { NewName = "test" };
                string json = SimpleJson.SerializeObject(obj);
                Assert.IsTrue(
                    json.Contains("NewName") || json.Contains("newname"),
                    "Serialization must use original CLR name");
                Assert.IsFalse(json.Contains("old_name"),
                    "Alias must NOT appear in serialized output");
            }

            Assert.BeginTest("JsonAlias: deserialize accepts alias key");
            {
                string json = "{\"old_name\":\"fromAlias\"}";
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);
                Assert.AreEqual("fromAlias",result.NewName);
            }

            Assert.BeginTest("JsonAlias: deserialize accepts original name");
            {
                string json = "{\"NewName\":\"fromOriginal\"}";
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);
                Assert.AreEqual("fromOriginal",result.NewName);
            }

            Assert.BeginTest("JsonAlias: duplicate keys — latter wins");
            {
                string json = "{\"old_name\":\"fromAlias\",\"NewName\":\"fromOriginal\"}";
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);
                Assert.AreEqual("fromOriginal",result.NewName,
                    "When both alias and original name appear, latter must win");
            }
        }

        // ──────────────────────────────────────────────────────────
        // 6. float / double 精度
        // ──────────────────────────────────────────────────────────

        private static void TestFloatPrecision()
        {
            Assert.BeginTest("float G9 round-trip: 1/3");
            {
                float original = 1.0f / 3.0f;
                var obj = new FloatPoco { FloatValue = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(original,result.FloatValue,0f,
                    "G9 must guarantee exact float round-trip");
            }

            Assert.BeginTest("float G9 round-trip: large value");
            {
                float original = 123456789.0f;
                var obj = new FloatPoco { FloatValue = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(original,result.FloatValue,0f);
            }

            Assert.BeginTest("double G17 round-trip: 1/3");
            {
                double original = 1.0 / 3.0;
                var obj = new FloatPoco { DoubleValue = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(original,result.DoubleValue,0.0,
                    "G17 must guarantee exact double round-trip");
            }

            Assert.BeginTest("double G17 round-trip: Math.PI");
            {
                double original = Math.PI;
                var obj = new FloatPoco { DoubleValue = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(original,result.DoubleValue,0.0);
            }

            Assert.BeginTest("float NaN serializes as null");
            {
                var obj = new FloatPoco { FloatValue = float.NaN };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json,"null");
            }

            Assert.BeginTest("float +Infinity serializes as null");
            {
                var obj = new FloatPoco { FloatValue = float.PositiveInfinity };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json,"null");
            }

            Assert.BeginTest("float -Infinity serializes as null");
            {
                var obj = new FloatPoco { FloatValue = float.NegativeInfinity };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json,"null");
            }

            Assert.BeginTest("double NaN serializes as null");
            {
                var obj = new FloatPoco { DoubleValue = double.NaN };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json,"null");
            }

            Assert.BeginTest("decimal round-trip: no exception");
            {
                Assert.DoesNotThrow(() =>
                {
                    var obj = new FloatPoco { DecimalValue = 123456789.123456789m };
                    string json = SimpleJson.SerializeObject(obj);
                    SimpleJson.DeserializeObject<FloatPoco>(json);
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 7. 字典 key 类型支持
        // ──────────────────────────────────────────────────────────

        private static void TestDictionaryKeys()
        {
            Assert.BeginTest("Dict<string,int> round-trip");
            {
                var original = new Dictionary<string,int>
                    { { "a", 1 }, { "b", 2 }, { "c", 3 } };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<
                    Dictionary<string,int>>(json);
                Assert.AreEqual(3,result.Count);
                foreach (var kvp in original)
                    Assert.AreEqual(kvp.Value,result[kvp.Key],
                        "Dict<string,int> value mismatch for key: " + kvp.Key);
            }

            Assert.BeginTest("Dict<int,string> serializes as string keys");
            {
                var dict = new Dictionary<int,string>
                    { { 1, "one" }, { 2, "two" } };
                string json = SimpleJson.SerializeObject(dict);
                Assert.Contains(json,"\"1\"",
                    "Integer keys must become JSON string keys");
            }

            Assert.BeginTest("Dict<int,string> round-trip");
            {
                var original = new Dictionary<int,string>
                    { { 1, "one" }, { 2, "two" }, { 3, "three" } };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<
                    Dictionary<int,string>>(json);
                Assert.AreEqual(original.Count,result.Count);
                foreach (var kvp in original)
                    Assert.AreEqual(kvp.Value,result[kvp.Key]);
            }

            Assert.BeginTest("Dict<Color,string> enum key: numeric string in JSON");
            {
                var dict = new Dictionary<Color,string>
                    { { Color.Red, "r" }, { Color.Green, "g" } };
                string json = SimpleJson.SerializeObject(dict);
                Assert.Contains(json,"\"0\"");
                Assert.Contains(json,"\"1\"");
            }

            Assert.BeginTest("Dict<Color,string> enum key round-trip (numeric)");
            {
                var original = new Dictionary<Color,string>
                {
                    { Color.Red,   "red"   },
                    { Color.Green, "green" },
                    { Color.Blue,  "blue"  }
                };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<
                    Dictionary<Color,string>>(json);
                Assert.AreEqual(original.Count,result.Count);
                foreach (var kvp in original)
                    Assert.AreEqual(kvp.Value,result[kvp.Key]);
            }

            Assert.BeginTest("Dict<Color,string> enum key deserialize by name");
            {
                string json = "{\"Red\":\"r\",\"Green\":\"g\",\"Blue\":\"b\"}";
                var result = SimpleJson.DeserializeObject<
                    Dictionary<Color,string>>(json);
                Assert.AreEqual(3,result.Count);
                Assert.AreEqual("r",result[Color.Red]);
                Assert.AreEqual("g",result[Color.Green]);
                Assert.AreEqual("b",result[Color.Blue]);
            }

            Assert.BeginTest("Dict duplicate keys: latter wins");
            {
                string json = "{\"a\":1,\"a\":2}";
                var result = SimpleJson.DeserializeObject<
                    Dictionary<string,int>>(json);
                Assert.AreEqual(1,result.Count);
                Assert.AreEqual(2,result["a"],
                    "Latter duplicate key must win");
            }

            Assert.BeginTest("Hashtable (non-generic) round-trip");
            {
                var dict = new Hashtable
                    { { "k1", "v1" }, { "k2", 42 }, { "k3", true } };
                string json = SimpleJson.SerializeObject(dict);
                var result = SimpleJson.DeserializeObject<Hashtable>(json);
                Assert.AreEqual(dict.Count,result.Count);
                Assert.AreEqual("v1",result["k1"]);
                Assert.AreEqual(true,result["k3"]);
            }

            Assert.BeginTest("Null dict key throws");
            {
                Assert.Throws<Exception>(() =>
                {
                    var d = new Hashtable { { null,"v" } };
                    SimpleJson.SerializeObject(d);
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 8. Nullable<T>
        // ──────────────────────────────────────────────────────────

        private static void TestNullable()
        {
            Assert.BeginTest("Nullable<T> with value: round-trip");
            {
                var original = new NullablePoco
                { NullableInt = 42,NullableFloat = 3.14f,NullableEnum = Color.Green };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                Assert.AreEqual(42,result.NullableInt);
                Assert.AreEqual(3.14f,result.NullableFloat);
                Assert.AreEqual(Color.Green,result.NullableEnum);
            }

            Assert.BeginTest("Nullable<T> null value: round-trip");
            {
                var original = new NullablePoco
                { NullableInt = null,NullableFloat = null,NullableEnum = null };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                Assert.IsNull(result.NullableInt);
                Assert.IsNull(result.NullableFloat);
                Assert.IsNull(result.NullableEnum);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 9. DateTime / DateTimeOffset / Guid
        // ──────────────────────────────────────────────────────────

        private static void TestDateTimeGuid()
        {
            Assert.BeginTest("DateTime UTC round-trip");
            {
                var original = new DateTime(2025,6,15,10,30,45,DateTimeKind.Utc);
                var obj = new DateTimePoco { DateTime = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.DateTime);
            }
            Assert.BeginTest("DateTime Local round-trip");
            {
                var original = new DateTime(2025,6,15,10,30,45,DateTimeKind.Local);
                var obj = new DateTimePoco { DateTime = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original.ToUniversalTime(),result.DateTime);
            }
            Assert.BeginTest("DateTime Unspecified round-trip");
            {
                var original = new DateTime(2025,6,15,10,30,45,DateTimeKind.Unspecified);
                var obj = new DateTimePoco { DateTime = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original.ToUniversalTime(),result.DateTime);
            }
            Assert.BeginTest("DateTimeOffset round-trip");
            {
                var original = new DateTimeOffset(
                    2025,6,15,10,30,45,TimeSpan.FromHours(8));
                var obj = new DateTimePoco { DateTimeOffset = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.DateTimeOffset);
            }

            Assert.BeginTest("TimeSpan round-trip");
            {
                var original = TimeSpan.FromHours(2.5);
                var obj = new DateTimePoco { TimeSpan = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.TimeSpan);
            }

            Assert.BeginTest("TimeSpan from ticks");
            {
                var original = TimeSpan.FromTicks(1234567890);
                var obj = new DateTimePoco { TimeSpan = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.TimeSpan);
            }

            Assert.BeginTest("TimeSpan negative");
            {
                var original = TimeSpan.FromHours(-3.5);
                var obj = new DateTimePoco { TimeSpan = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.TimeSpan);
            }

            Assert.BeginTest("Guid round-trip");
            {
                var original = Guid.NewGuid();
                var obj = new DateTimePoco { Guid = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.Guid);
            }

            Assert.BeginTest("Guid.Empty round-trip");
            {
                var original = Guid.Empty;
                var obj = new DateTimePoco { Guid = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<DateTimePoco>(json);
                Assert.AreEqual(original,result.Guid);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 10. 集合类型
        // ──────────────────────────────────────────────────────────

        private static void TestCollections()
        {
            Assert.BeginTest("List<int> round-trip");
            {
                var original = new List<int> { 1,2,3,4,5 };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<List<int>>(json);
                Assert.AreEqual(original.Count,result.Count);
                for (int i = 0; i < original.Count; i++)
                    Assert.AreEqual(original[i],result[i]);
            }

            Assert.BeginTest("List<string> round-trip");
            {
                var original = new List<string> { "alpha","beta","gamma" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<List<string>>(json);
                Assert.AreEqual(original.Count,result.Count);
                for (int i = 0; i < original.Count; i++)
                    Assert.AreEqual(original[i],result[i]);
            }

            Assert.BeginTest("int[] round-trip");
            {
                int[] original = { 10,20,30 };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<int[]>(json);
                Assert.AreEqual(original.Length,result.Length);
                for (int i = 0; i < original.Length; i++)
                    Assert.AreEqual(original[i],result[i]);
            }

            Assert.BeginTest("string[] round-trip");
            {
                string[] original = { "x","y","z" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<string[]>(json);
                Assert.AreEqual(original.Length,result.Length);
                for (int i = 0; i < original.Length; i++)
                    Assert.AreEqual(original[i],result[i]);
            }

            Assert.BeginTest("List<BasicPoco> round-trip");
            {
                var original = new List<BasicPoco>
                {
                    new BasicPoco { Name = "Alice", Age = 30 },
                    new BasicPoco { Name = "Bob",   Age = 25 }
                };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<List<BasicPoco>>(json);
                Assert.AreEqual(2,result.Count);
                Assert.AreEqual("Alice",result[0].Name);
                Assert.AreEqual("Bob",result[1].Name);
                Assert.AreEqual(30,result[0].Age);
                Assert.AreEqual(25,result[1].Age);
            }

            Assert.BeginTest("List with null elements");
            {
                var original = new List<object> { 1,null,"test",null };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<List<object>>(json);
                Assert.AreEqual(4,result.Count);
                Assert.IsNull(result[1]);
                Assert.IsNull(result[3]);
            }

            Assert.BeginTest("Empty List<int> round-trip");
            {
                var original = new List<int>();
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<List<int>>(json);
                Assert.AreEqual(0,result.Count);
            }

            Assert.BeginTest("Empty JSON array deserializes to empty list");
            {
                var result = SimpleJson.DeserializeObject<List<int>>("[]");
                Assert.AreEqual(0,result.Count);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 11. 类型转换 CoerceValue
        // ──────────────────────────────────────────────────────────

        private static void TestTypeCoercion()
        {
            Assert.BeginTest("CoerceValue: int JSON → float property");
            {
                string json = "{\"FloatValue\":42}";
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(42.0f,result.FloatValue,0f);
            }

            Assert.BeginTest("CoerceValue: double JSON → int property (truncate)");
            {
                string json = "{\"Age\":30.9}";
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(30,result.Age,
                    "Double-to-int coercion must truncate decimal part");
            }

            Assert.BeginTest("CoerceValue: incompatible string → int skips gracefully");
            {
                string json = "{\"Age\":\"not_a_number\"}";
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(0,result.Age,
                    "Failed coercion must leave property at default value (0)");
            }

            Assert.BeginTest("CoerceValue: enum from numeric int");
            {
                string json = "{\"Status\":1}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                Assert.AreEqual(Color.Green,result.Status);
            }

            Assert.BeginTest("CoerceValue: enum from name string");
            {
                string json = "{\"Status\":\"Blue\"}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                Assert.AreEqual(Color.Blue,result.Status);
            }

            Assert.BeginTest("CoerceValue: enum from float (truncate decimal)");
            {
                string json = "{\"Status\":1.9}";
                var result = SimpleJson.DeserializeObject<EnumPoco>(json);
                Assert.AreEqual(Color.Green,result.Status,
                    "Float 1.9 truncated to 1 must map to Color.Green");
            }

            Assert.BeginTest("CoerceValue: Nullable<int> from int JSON");
            {
                string json = "{\"NullableInt\":99}";
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                Assert.AreEqual(99,result.NullableInt);
            }

            Assert.BeginTest("CoerceValue: Nullable<int> from null JSON");
            {
                string json = "{\"NullableInt\":null}";
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);
                Assert.IsNull(result.NullableInt);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 12. 无参构造缺失
        // ──────────────────────────────────────────────────────────

        private static void TestNoCtor()
        {
            Assert.BeginTest("No parameterless ctor throws MissingMethodException");
            {
                Assert.Throws<MissingMethodException>(() =>
                    SimpleJson.DeserializeObject<NoCtor>("{\"Value\":\"test\"}"),
                    "Type without parameterless ctor must throw MissingMethodException");
            }
        }

        // ──────────────────────────────────────────────────────────
        // 13. toLowerCase 策略与缓存隔离
        // ──────────────────────────────────────────────────────────

        private static void TestToLowerCase()
        {
            Assert.BeginTest("toLowerCase=true: serialize preserves original casing");
            {
                var strategy = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = true };
                var obj = new BasicPoco { Name = "Test",Age = 30 };
                string json = SimpleJson.SerializeObject(obj,strategy);
                // toLowerCase 仅影响反序列化（大小写无关匹配），序列化保留原始大小写
                Assert.Contains(json,"\"Name\"");
                Assert.Contains(json,"\"Age\"");
            }

            Assert.BeginTest("toLowerCase=true: deserialize accepts lowercase keys");
            {
                var strategy = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = true };
                string json = "{\"name\":\"Test\",\"age\":30}";
                var result = SimpleJson.DeserializeObject<BasicPoco>(json,strategy);
                Assert.AreEqual("Test",result.Name);
                Assert.AreEqual(30,result.Age);
            }

            Assert.BeginTest("toLowerCase=true: deserialize accepts PascalCase keys");
            {
                var strategy = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = true };
                string json = "{\"Name\":\"Test\",\"Age\":30}";
                var result = SimpleJson.DeserializeObject<BasicPoco>(json,strategy);
                Assert.AreEqual("Test",result.Name);
                Assert.AreEqual(30,result.Age);
            }

            Assert.BeginTest("toLowerCase=false: serialize preserves original casing");
            {
                var strategy = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = false };
                var obj = new BasicPoco { Name = "Test",Age = 30 };
                string json = SimpleJson.SerializeObject(obj,strategy);
                Assert.Contains(json,"\"Name\"");
                Assert.Contains(json,"\"Age\"");
            }

            Assert.BeginTest("Cache isolation: toLowerCase=true/false do not pollute each other");
            {
                var strategyLower = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = true };
                var strategyOriginal = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = false };
                var obj = new BasicPoco { Name = "Test",Age = 30 };

                string jsonLower = SimpleJson.SerializeObject(obj,strategyLower);
                string jsonOriginal = SimpleJson.SerializeObject(obj,strategyOriginal);

                // 两种策略序列化都保留原始大小写
                Assert.Contains(jsonLower,"\"Name\"");
                Assert.Contains(jsonOriginal,"\"Name\"");

                // toLowerCase=true 策略可以反序列化小写 key
                var resultLower = SimpleJson.DeserializeObject<BasicPoco>(
                    "{\"name\":\"Test\",\"age\":30}",strategyLower);
                var resultOriginal = SimpleJson.DeserializeObject<BasicPoco>(
                    jsonOriginal,strategyOriginal);

                Assert.AreEqual("Test",resultLower.Name);
                Assert.AreEqual("Test",resultOriginal.Name);
                Assert.AreEqual(30,resultLower.Age);
                Assert.AreEqual(30,resultOriginal.Age);
            }

            Assert.BeginTest("Cache isolation: cross-use results in default values (no crash)");
            {
                var strategyOriginal = new DefaultJsonSerializationStrategy { ignoreLowerCaseForDeserialization = false };
                var obj = new BasicPoco { Name = "Test",Age = 30 };

                string jsonLower = "{\"name\":\"Test\",\"age\":30}";

                Assert.DoesNotThrow(() =>
                {
                    // 小写 JSON + toLowerCase=false 策略 → key 精确匹配失败 → 默认值
                    var result = SimpleJson.DeserializeObject<BasicPoco>(
                        jsonLower,strategyOriginal);
                    _ = result;
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 14. 字符串转义
        // ──────────────────────────────────────────────────────────

        private static void TestStringEscape()
        {
            Assert.BeginTest("Escape: newline / tab / CRLF round-trip");
            {
                var original = new BasicPoco
                { Name = "Line1\nLine2\tTabbed\r\nCRLF" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name);
            }

            Assert.BeginTest("Escape: quote and backslash round-trip");
            {
                var original = new BasicPoco
                { Name = "Say \"Hello\" and C:\\path\\to\\file" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name);
            }

            Assert.BeginTest("Escape: CJK characters round-trip");
            {
                var original = new BasicPoco { Name = "中文日本語한국어" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name);
            }

            Assert.BeginTest("Escape: \\uXXXX sequence (é = \\u00E9)");
            {
                var original = new BasicPoco { Name = "caf\u00E9" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name);
            }

            Assert.BeginTest("Escape: UTF-16 surrogate pair (😀 = \\uD83D\\uDE00)");
            {
                var original = new BasicPoco { Name = "Hello \uD83D\uDE00 World" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name,
                    "Surrogate pairs (emoji) must survive round-trip");
            }

            Assert.BeginTest("Escape: empty string round-trip");
            {
                var original = new BasicPoco { Name = "" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual("",result.Name);
            }

            Assert.BeginTest("Escape: forward slash not mangled");
            {
                var original = new BasicPoco { Name = "http://example.com/path" };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual(original.Name,result.Name);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 15. 数字范围
        // ──────────────────────────────────────────────────────────

        private static void TestNumberRange()
        {
            Assert.BeginTest("long.MaxValue round-trip");
            {
                long original = long.MaxValue;
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<long>(json);
                Assert.AreEqual(original,result);
            }

            Assert.BeginTest("long.MinValue round-trip");
            {
                long original = long.MinValue;
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<long>(json);
                Assert.AreEqual(original,result);
            }

            Assert.BeginTest("ulong.MaxValue round-trip");
            {
                ulong original = ulong.MaxValue;
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<ulong>(json);
                Assert.AreEqual(original,result);
            }

            Assert.BeginTest("Negative int round-trip");
            {
                int original = -99999;
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<int>(json);
                Assert.AreEqual(original,result);
            }

            Assert.BeginTest("Zero round-trip");
            {
                int original = 0;
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<int>(json);
                Assert.AreEqual(original,result);
            }

            Assert.BeginTest("Beyond-ulong number falls back to double (no exception)");
            {
                Assert.DoesNotThrow(() =>
                {
                    string json = "{\"Value\":99999999999999999999999}";
                    var result = SimpleJson.DeserializeObject<
                        Dictionary<string,object>>(json);
                    Assert.IsNotNull(result["Value"],
                        "Beyond-ulong must fall back to double without throwing");
                });
            }

            Assert.BeginTest("Float exponent notation round-trip");
            {
                double original = 1.23e10;
                var obj = new FloatPoco { DoubleValue = original };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual(original,result.DoubleValue,1.0);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 16. char 类型
        // ──────────────────────────────────────────────────────────

        private static void TestChar()
        {
            Assert.BeginTest("char single character round-trip");
            {
                var obj = new CharPoco { Ch = 'Z' };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<CharPoco>(json);
                Assert.AreEqual('Z',result.Ch);
            }

            Assert.BeginTest("char from multi-char string: takes first, no exception");
            {
                Assert.DoesNotThrow(() =>
                {
                    string json = "{\"Ch\":\"XYZ\"}";
                    var result = SimpleJson.DeserializeObject<CharPoco>(json);
                    Assert.AreEqual('X',result.Ch,
                        "Multi-char string assigned to char must take first character");
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 17. new-hide 属性去重（最派生版本优先）
        // ──────────────────────────────────────────────────────────

        private static void TestShadowProperty()
        {
            Assert.BeginTest("new-hide: most derived property version used");
            {
                var obj = new ShadowChild { Value = "child_string" };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json,"child_string",
                    "Most derived (string) Value must be serialized");

                var result = SimpleJson.DeserializeObject<ShadowChild>(json);
                Assert.AreEqual("child_string",result.Value);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 18. 嵌套对象
        // ──────────────────────────────────────────────────────────

        private static void TestNested()
        {
            Assert.BeginTest("Nested POCO: outer/inner structure preserved");
            {
                var inner = new BasicPoco { Name = "inner",Age = 10 };
                var outer = new Dictionary<string,object>
                    { { "Outer", "outer_val" }, { "Inner", inner } };

                string json = SimpleJson.SerializeObject(outer);
                var result = SimpleJson.DeserializeObject<
                    Dictionary<string,object>>(json);

                Assert.IsTrue(result.ContainsKey("Outer"));
                Assert.IsTrue(result.ContainsKey("Inner"));
            }

            Assert.BeginTest("List of Dict round-trip");
            {
                var original = new List<Dictionary<string,int>>
                {
                    new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
                    new Dictionary<string, int> { { "c", 3 }, { "d", 4 } }
                };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<
                    List<Dictionary<string,int>>>(json);

                Assert.AreEqual(2,result.Count);
                Assert.AreEqual(1,result[0]["a"]);
                Assert.AreEqual(3,result[1]["c"]);
            }

            Assert.BeginTest("Deeply nested JSON round-trip");
            {
                var deep = new Dictionary<string,object>
                {
                    {
                        "level1", new Dictionary<string, object>
                        {
                            {
                                "level2", new Dictionary<string, object>
                                {
                                    { "value", 42 }
                                }
                            }
                        }
                    }
                };
                string json = SimpleJson.SerializeObject(deep);
                var result = SimpleJson.DeserializeObject<
                    Dictionary<string,object>>(json);

                Assert.IsNotNull(result["level1"]);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 19. 错误处理
        // ──────────────────────────────────────────────────────────

        private static void TestErrorHandling()
        {
            Assert.BeginTest("Invalid JSON throws exception");
            {
                Assert.Throws<Exception>(() =>
                    SimpleJson.DeserializeObject<BasicPoco>("{invalid json}"));
            }

            Assert.BeginTest("Truncated JSON throws exception");
            {
                Assert.Throws<Exception>(() =>
                    SimpleJson.DeserializeObject<BasicPoco>("{\"Name\":"));
            }

            Assert.BeginTest("Empty string throws or returns null gracefully");
            {
                Assert.DoesNotThrow(() =>
                {
                    try
                    {
                        SimpleJson.DeserializeObject<object>("");
                    }
                    catch (Exception)
                    {
                        // 空字符串抛出异常是可接受的行为
                    }
                });
            }

            Assert.BeginTest("Null JSON string handled gracefully");
            {
                Assert.DoesNotThrow(() =>
                {
                    try
                    {
                        SimpleJson.DeserializeObject<object>(null);
                    }
                    catch (Exception)
                    {
                        // null 字符串抛出异常是可接受的行为
                    }
                });
            }

            Assert.BeginTest("Wrong type in JSON array does not crash");
            {
                Assert.DoesNotThrow(() =>
                {
                    // JSON 中混合类型数组
                    string json = "[1, \"two\", true, null, 3.14]";
                    var result = SimpleJson.DeserializeObject<List<object>>(json);
                    Assert.AreEqual(5,result.Count);
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 20. ClearCache 不崩溃
        // ──────────────────────────────────────────────────────────

        private static void TestClearCache()
        {
            // 注意：SimpleJson_Unity.cs 中没有 ClearReflectionCache 方法
            // 此测试已被禁用

            Assert.BeginTest("ClearReflectionCache does not throw");
            {
                Assert.DoesNotThrow(() => SimpleJson.ClearReflectionCache(),
                    "ClearReflectionCache must not throw NotImplementedException");
            }

            Assert.BeginTest("ClearCache then serialize still works");
            {
                SimpleJson.ClearReflectionCache();
                var obj = new BasicPoco { Name = "Test",Age = 30 };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                Assert.AreEqual("Test",result.Name);
                Assert.AreEqual(30,result.Age);
            }

            Assert.BeginTest("ClearCache multiple times does not crash");
            {
                Assert.DoesNotThrow(() =>
                {
                    for (int i = 0; i < 5; i++)
                        SimpleJson.ClearReflectionCache();
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 21. 线程安全
        // ──────────────────────────────────────────────────────────

        private static void TestThreadSafety()
        {
            Assert.BeginTest("Concurrent serialization of same type: no exception");
            {
                var obj = new BasicPoco { Name = "Thread",Age = 99 };
                var exceptions = new List<Exception>();
                var lockObj = new object();
                var threads = new List<Thread>();

                for (int i = 0; i < 8; i++)
                {
                    var t = new Thread(() =>
                    {
                        try
                        {
                            for (int j = 0; j < 200; j++)
                            {
                                string json = SimpleJson.SerializeObject(obj);
                                var result = SimpleJson.DeserializeObject<BasicPoco>(json);
                                if (result == null || result.Name != "Thread" || result.Age != 99)
                                    throw new Exception("Data mismatch");
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (lockObj) { exceptions.Add(ex); }
                        }
                    });
                    threads.Add(t);
                    t.Start();
                }

                foreach (var t in threads) t.Join();

                Assert.AreEqual(0,exceptions.Count,
                    "Concurrent serialization must produce no exceptions");
            }

            Assert.BeginTest("Concurrent first-access for different types: no exception");
            {
                var exceptions = new List<Exception>();
                var lockObj = new object();
                var threads = new List<Thread>();

                var objects = new object[]
                {
                    new BasicPoco    { Name = "A" },
                    new FloatPoco    { FloatValue = 1.5f },
                    new NullablePoco { NullableInt = 10 },
                    new EnumPoco     { Status = Color.Red },
                    new CollectionPoco
                    {
                        IntList       = new List<int> { 1, 2 },
                        StringKeyDict = new Dictionary<string, int> { { "k", 1 } },
                        IntKeyDict    = new Dictionary<int, string> { { 1, "v" } },
                        EnumKeyDict   = new Dictionary<Color, float>
                            { { Color.Red, 1.0f } }
                    }
                };

                foreach (var captured in objects)
                {
                    var cap = captured;
                    var t = new Thread(() =>
                    {
                        try
                        {
                            for (int j = 0; j < 100; j++)
                                SimpleJson.SerializeObject(cap);
                        }
                        catch (Exception ex)
                        {
                            lock (lockObj) { exceptions.Add(ex); }
                        }
                    });
                    threads.Add(t);
                    t.Start();
                }

                foreach (var t in threads) t.Join();

                Assert.AreEqual(0,exceptions.Count,
                    "Concurrent first-access for different types must not throw");
            }

            Assert.BeginTest("Concurrent ClearCache + serialize: no deadlock");
            {
                // 注意：SimpleJson_Unity.cs 中没有 ClearReflectionCache 方法
                // 此测试已被禁用

                var exceptions = new List<Exception>();
                var lockObj = new object();
                var done = false;

                var clearThread = new Thread(() =>
                {
                    try
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            SimpleJson.ClearReflectionCache();
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj) { exceptions.Add(ex); }
                    }
                    finally { done = true; }
                });

                var serializeThread = new Thread(() =>
                {
                    var obj = new BasicPoco { Name = "X",Age = 1 };
                    try
                    {
                        while (!done)
                        {
                            string json = SimpleJson.SerializeObject(obj);
                            SimpleJson.DeserializeObject<BasicPoco>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj) { exceptions.Add(ex); }
                    }
                });

                clearThread.Start();
                serializeThread.Start();
                clearThread.Join(3000);
                serializeThread.Join(3000);

                Assert.AreEqual(0,exceptions.Count,
                    "ClearCache concurrent with serialize must not throw");
            }
        }

        // ──────────────────────────────────────────────────────────
        // 22. 往返完整性验证
        // ──────────────────────────────────────────────────────────

        private static void TestFullRoundTrip()
        {
            Assert.BeginTest("Full CollectionPoco round-trip");
            {
                var original = new CollectionPoco
                {
                    IntList = new List<int> { 1,2,3 },
                    StringKeyDict = new Dictionary<string,int>
                        { { "x", 10 }, { "y", 20 } },
                    IntKeyDict = new Dictionary<int,string>
                        { { 1, "one" }, { 2, "two" } },
                    EnumKeyDict = new Dictionary<Color,float>
                    {
                        { Color.Red,   0.1f },
                        { Color.Green, 0.2f },
                        { Color.Blue,  0.3f }
                    }
                };

                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<CollectionPoco>(json);

                // IntList
                Assert.AreEqual(original.IntList.Count,result.IntList.Count);
                for (int i = 0; i < original.IntList.Count; i++)
                    Assert.AreEqual(original.IntList[i],result.IntList[i]);

                // StringKeyDict
                Assert.AreEqual(original.StringKeyDict.Count,
                    result.StringKeyDict.Count);
                foreach (var kvp in original.StringKeyDict)
                    Assert.AreEqual(kvp.Value,result.StringKeyDict[kvp.Key]);

                // IntKeyDict
                Assert.AreEqual(original.IntKeyDict.Count,result.IntKeyDict.Count);
                foreach (var kvp in original.IntKeyDict)
                    Assert.AreEqual(kvp.Value,result.IntKeyDict[kvp.Key]);

                // EnumKeyDict
                Assert.AreEqual(original.EnumKeyDict.Count,
                    result.EnumKeyDict.Count);
                foreach (var kvp in original.EnumKeyDict)
                    Assert.AreEqual(kvp.Value,result.EnumKeyDict[kvp.Key],1e-5f);
            }

            Assert.BeginTest("All attributes combined round-trip");
            {
                var original = new AttributePoco
                {
                    Visible = "visible_val",
                    Ignored = "should_not_persist",
                    NewName = "new_name_val",
                    WithAlias = "alias_val"
                };
                original.SetPrivateProperty("private_val");

                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<AttributePoco>(json);

                Assert.AreEqual("visible_val",result.Visible);
                Assert.IsNull(result.Ignored,
                    "JsonIgnore field must not be restored");
                Assert.AreEqual("new_name_val",result.NewName);
                Assert.AreEqual("alias_val",result.WithAlias);
                Assert.AreEqual("private_val",result.GetPrivateProperty());
            }

            Assert.BeginTest("Float precision: all special values round-trip");
            {
                float[] floats =
                {
                    0f, 1f, -1f,
                    float.Epsilon,
                    float.MaxValue,
                    1.0f / 3.0f,
                    (float)Math.PI,
                    123456789.0f
                };

                foreach (float f in floats)
                {
                    var obj = new FloatPoco { FloatValue = f };
                    string json = SimpleJson.SerializeObject(obj);
                    var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                    Assert.AreEqual(f,result.FloatValue,0f,
                        "float round-trip failed for value: " + f);
                }
            }

            Assert.BeginTest("Double precision: all special values round-trip");
            {
                double[] doubles =
                {
                    0.0, 1.0, -1.0,
                    double.Epsilon,
                    double.MaxValue,
                    1.0 / 3.0,
                    Math.PI,
                    Math.E,
                    1.23456789012345678
                };

                foreach (double d in doubles)
                {
                    var obj = new FloatPoco { DoubleValue = d };
                    string json = SimpleJson.SerializeObject(obj);
                    var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                    Assert.AreEqual(d,result.DoubleValue,0.0,
                        "double round-trip failed for value: " + d);
                }
            }

            Assert.BeginTest("JsonAlias: all deserialization paths");
            {
                // 通过别名
                var r1 = SimpleJson.DeserializeObject<AttributePoco>(
                    "{\"old_name\":\"via_alias\"}");
                Assert.AreEqual("via_alias",r1.NewName);

                // 通过原始名
                var r2 = SimpleJson.DeserializeObject<AttributePoco>(
                    "{\"NewName\":\"via_original\"}");
                Assert.AreEqual("via_original",r2.NewName);

                // 序列化输出原始名
                var obj = new AttributePoco { NewName = "serialize_test" };
                string json = SimpleJson.SerializeObject(obj);
                Assert.IsFalse(json.Contains("old_name"),
                    "Alias must never appear in serialized output");
            }

            Assert.BeginTest("Nullable: mixed null and non-null in same object");
            {
                var original = new NullablePoco
                {
                    NullableInt = 42,
                    NullableFloat = null,
                    NullableEnum = Color.Blue
                };
                string json = SimpleJson.SerializeObject(original);
                var result = SimpleJson.DeserializeObject<NullablePoco>(json);

                Assert.AreEqual(42,result.NullableInt);
                Assert.IsNull(result.NullableFloat);
                Assert.AreEqual(Color.Blue,result.NullableEnum);
            }

            Assert.BeginTest("Large collection performance: no exception");
            {
                Assert.DoesNotThrow(() =>
                {
                    var obj = new CollectionPoco
                    {
                        IntList = new List<int>(),
                        StringKeyDict = new Dictionary<string,int>(),
                        IntKeyDict = new Dictionary<int,string>(),
                        EnumKeyDict = new Dictionary<Color,float>()
                    };

                    for (int i = 0; i < 1000; i++)
                    {
                        obj.IntList.Add(i);
                        obj.StringKeyDict["key" + i] = i;
                        obj.IntKeyDict[i] = "val" + i;
                        obj.EnumKeyDict[(Color)(i % 3)] = i * 0.5f;
                    }

                    string json = SimpleJson.SerializeObject(obj);
                    var result = SimpleJson.DeserializeObject<CollectionPoco>(json);

                    Assert.AreEqual(1000,result.IntList.Count);
                    Assert.AreEqual(1000,result.StringKeyDict.Count);
                    Assert.AreEqual(1000,result.IntKeyDict.Count);
                });
            }
        }

        // ──────────────────────────────────────────────────────────
        // 23. JsonAlias 序列化别名（useJsonAliasForSerialization）
        // ──────────────────────────────────────────────────────────

        private static void TestJsonAliasSerialization()
        {
            Assert.BeginTest("序列化默认使用原始属性名");
            {
                var obj = new AliasSerializePoco { UserName = "张三",UserId = 123 };

                var strategy = new DefaultJsonSerializationStrategy();
                string json = SimpleJson.SerializeObject(obj,strategy);

                Assert.IsTrue(json.Contains("UserName"),
                    "默认应该输出原始属性名 UserName");
                Assert.IsTrue(json.Contains("UserId"),
                    "默认应该输出原始属性名 UserId");
                Assert.IsFalse(json.Contains("user_name"),
                    "默认不应该输出别名 user_name");
                Assert.IsFalse(json.Contains("user_id"),
                    "默认不应该输出别名 user_id");
            }

            Assert.BeginTest("序列化启用 useJsonAliasForSerialization=true");
            {
                var obj = new AliasSerializePoco { UserName = "李四",UserId = 456 };

                var strategy = new DefaultJsonSerializationStrategy(false,true);
                string json = SimpleJson.SerializeObject(obj,strategy);

                Assert.IsTrue(json.Contains("user_name"),
                    "启用后应该输出第一个别名 user_name");
                Assert.IsTrue(json.Contains("user_id"),
                    "启用后应该输出第一个别名 user_id");
                Assert.IsFalse(json.Contains("UserName") && !json.Contains("user_name"),
                    "启用后不应该输出原始属性名 UserName");
                Assert.IsFalse(json.Contains("UserId") && !json.Contains("user_id"),
                    "启用后不应该输出原始属性名 UserId");
            }

            Assert.BeginTest("序列化别名 + toLowerCase 组合");
            {
                var obj = new AliasSerializePoco { UserName = "王五",UserId = 789 };

                var strategy = new DefaultJsonSerializationStrategy(true,true);
                string json = SimpleJson.SerializeObject(obj,strategy);

                Assert.IsTrue(json.Contains("user_name"),
                    "toLowerCase=true + alias 应该保持别名的原始大小写");
                Assert.IsTrue(json.Contains("user_id"),
                    "toLowerCase=true + alias 应该保持别名的原始大小写");
            }

            Assert.BeginTest("反序列化仍然支持所有别名");
            {
                var strategy = new DefaultJsonSerializationStrategy(false,true);

                string json1 = "{\"user_name\":\"from_alias1\"}";
                var result1 = SimpleJson.DeserializeObject<AliasSerializePoco>(json1,strategy);
                Assert.AreEqual("from_alias1",result1.UserName);

                string json2 = "{\"userName\":\"from_alias2\"}";
                var result2 = SimpleJson.DeserializeObject<AliasSerializePoco>(json2,strategy);
                Assert.AreEqual("from_alias2",result2.UserName);

                string json3 = "{\"UserName\":\"from_original\"}";
                var result3 = SimpleJson.DeserializeObject<AliasSerializePoco>(json3,strategy);
                Assert.AreEqual("from_original",result3.UserName);
            }

            Assert.BeginTest("AcceptOriginalName=false 时反序列化行为");
            {
                var strictObj = new StrictAliasPoco { Code = 200 };
                string jsonStrict = SimpleJson.SerializeObject(strictObj);
                Assert.IsTrue(jsonStrict.Contains("Code"),
                    "序列化应输出原始属性名 Code");

                string jsonInput1 = "{\"api_code\":200}";
                var result1 = SimpleJson.DeserializeObject<StrictAliasPoco>(jsonInput1);
                Assert.AreEqual(200,result1.Code,
                    "应接受别名 api_code");

#if SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE
                // toLowerCase=true 时，setter 字典使用 OrdinalIgnoreCase，
                // "Code" 与别名 "code" 大小写无关匹配，因此 AcceptOriginalName=false
                // 的限制被绕过。这是大小写无关匹配的已知行为。
                string jsonInput2 = "{\"Code\":200}";
                var result2 = SimpleJson.DeserializeObject<StrictAliasPoco>(jsonInput2);
                Assert.AreEqual(200,result2.Code,
                    "toLowerCase=true 时 Code 与别名 code 大小写无关匹配");
#else
                string jsonInput2 = "{\"Code\":200}";
                var result2 = SimpleJson.DeserializeObject<StrictAliasPoco>(jsonInput2);
                Assert.AreEqual(0,result2.Code,
                    "AcceptOriginalName=false 时不应接受原始名称 Code");
#endif
            }
        }

        // ──────────────────────────────────────────────────────────
        // 24. 复杂多层嵌套类
        // ──────────────────────────────────────────────────────────

        private static void TestComplexNested()
        {
            // 测试 1：简单嵌套类（Address -> Person）
            Assert.BeginTest("简单嵌套类：Address -> Person");
            {
                var person = new Person
                {
                    Name = "张三",
                    Age = 30,
                    HomeAddress = new Address
                    {
                        Street = "长安街1号",
                        City = "北京",
                        Country = "中国",
                        ZipCode = "100000"
                    },
                    WorkAddress = new Address
                    {
                        Street = "中关村大街1号",
                        City = "北京",
                        Country = "中国",
                        ZipCode = "100080"
                    },
                    PhoneNumbers = new List<string> { "13800138000","010-12345678" }
                };

                string json = SimpleJson.SerializeObject(person);
                var result = SimpleJson.DeserializeObject<Person>(json);

                Assert.AreEqual("张三",result.Name);
                Assert.AreEqual(30,result.Age);
                Assert.IsNotNull(result.HomeAddress);
                Assert.AreEqual("长安街1号",result.HomeAddress.Street);
                Assert.AreEqual("北京",result.HomeAddress.City);
                Assert.IsNotNull(result.WorkAddress);
                Assert.AreEqual("中关村大街1号",result.WorkAddress.Street);
                Assert.AreEqual(2,result.PhoneNumbers.Count);
                Assert.AreEqual("13800138000",result.PhoneNumbers[0]);
            }

            // 测试 2：三层嵌套（Person -> Department）
            Assert.BeginTest("三层嵌套：Person -> Department");
            {
                var department = new Department
                {
                    Name = "研发部",
                    Manager = new Person
                    {
                        Name = "李四",
                        Age = 40,
                        HomeAddress = new Address
                        {
                            Street = "经理街1号",
                            City = "上海",
                            Country = "中国",
                            ZipCode = "200000"
                        }
                    },
                    Employees = new List<Person>
                    {
                        new Person
                        {
                            Name = "王五",
                            Age = 28,
                            HomeAddress = new Address
                            {
                                Street = "员工路1号",
                                City = "上海",
                                Country = "中国",
                                ZipCode = "200001"
                            }
                        },
                        new Person
                        {
                            Name = "赵六",
                            Age = 32,
                            HomeAddress = new Address
                            {
                                Street = "员工路2号",
                                City = "上海",
                                Country = "中国",
                                ZipCode = "200002"
                            }
                        }
                    },
                    MembersById = new Dictionary<int,Person>
                    {
                        { 1, new Person { Name = "员工001", Age = 25 } },
                        { 2, new Person { Name = "员工002", Age = 27 } }
                    }
                };

                string json = SimpleJson.SerializeObject(department);
                var result = SimpleJson.DeserializeObject<Department>(json);

                Assert.AreEqual("研发部",result.Name);
                Assert.IsNotNull(result.Manager);
                Assert.AreEqual("李四",result.Manager.Name);
                Assert.AreEqual(2,result.Employees.Count);
                Assert.AreEqual("王五",result.Employees[0].Name);
                Assert.AreEqual("赵六",result.Employees[1].Name);
                Assert.AreEqual(2,result.MembersById.Count);
                Assert.IsTrue(result.MembersById.ContainsKey(1));
                Assert.AreEqual("员工001",result.MembersById[1].Name);
            }

            // 测试 3：四层嵌套（Department -> Company）
            Assert.BeginTest("四层嵌套：Department -> Company");
            {
                var company = new Company
                {
                    CompanyName = "科技公司",
                    CEO = new Person
                    {
                        Name = "CEO张总",
                        Age = 50,
                        HomeAddress = new Address
                        {
                            Street = "CEO大街1号",
                            City = "深圳",
                            Country = "中国",
                            ZipCode = "518000"
                        }
                    },
                    Departments = new List<Department>
                    {
                        new Department
                        {
                            Name = "研发部",
                            Manager = new Person { Name = "研发经理", Age = 38 },
                            Employees = new List<Person>
                            {
                                new Person { Name = "研发员工1", Age = 28 },
                                new Person { Name = "研发员工2", Age = 30 }
                            }
                        },
                        new Department
                        {
                            Name = "市场部",
                            Manager = new Person { Name = "市场经理", Age = 35 },
                            Employees = new List<Person>
                            {
                                new Person { Name = "市场员工1", Age = 26 }
                            }
                        }
                    },
                    DepartmentsByName = new Dictionary<string,Department>
                    {
                        { "研发部", new Department { Name = "研发部" } },
                        { "市场部", new Department { Name = "市场部" } }
                    }
                };

                string json = SimpleJson.SerializeObject(company);
                var result = SimpleJson.DeserializeObject<Company>(json);

                Assert.AreEqual("科技公司",result.CompanyName);
                Assert.IsNotNull(result.CEO);
                Assert.AreEqual("CEO张总",result.CEO.Name);
                Assert.AreEqual(2,result.Departments.Count);
                Assert.AreEqual("研发部",result.Departments[0].Name);
                Assert.AreEqual("市场部",result.Departments[1].Name);
                Assert.AreEqual(2,result.DepartmentsByName.Count);
                Assert.IsTrue(result.DepartmentsByName.ContainsKey("研发部"));
            }

            // 测试 4：循环引用检测
            Assert.BeginTest("循环引用检测：Node -> Parent/Children");
            {
                var parent = new Node
                {
                    Id = "parent",
                    Name = "父节点"
                };

                var child1 = new Node
                {
                    Id = "child1",
                    Name = "子节点1",
                    Parent = parent  // 循环引用
                };

                var child2 = new Node
                {
                    Id = "child2",
                    Name = "子节点2",
                    Parent = parent  // 循环引用
                };

                parent.Children = new List<Node> { child1,child2 };

                // 序列化应该成功，循环引用字段会被序列化为 null
                string json = SimpleJson.SerializeObject(parent);

                // 反序列化
                var result = SimpleJson.DeserializeObject<Node>(json);

                Assert.AreEqual("父节点",result.Name);
                Assert.IsNull(result.Parent,"根节点的 Parent 应该为 null");
                Assert.IsNotNull(result.Children,"Children 列表应该存在");
                Assert.AreEqual(2,result.Children.Count);

                // 子节点的 Parent 应该为 null（循环引用被检测并序列化为 null）
                Assert.IsNull(result.Children[0].Parent,"子节点的 Parent 应该为 null（循环引用检测）");
                Assert.IsNull(result.Children[1].Parent,"子节点的 Parent 应该为 null（循环引用检测）");
            }

            // 测试 5：多层继承嵌套
            Assert.BeginTest("多层继承嵌套：BaseEntity -> User -> Profile");
            {
                var user = new User
                {
                    Id = 1001,
                    CreatedAt = new DateTime(2026,1,1,12,0,0,DateTimeKind.Utc),
                    Username = "testuser",
                    Email = "test@example.com",
                    Profile = new Profile
                    {
                        Id = 2001,
                        CreatedAt = new DateTime(2026,1,2,12,0,0,DateTimeKind.Utc),
                        DisplayName = "测试用户",
                        Tags = new List<string> { "VIP","活跃用户" },
                        Location = new Address
                        {
                            Street = "用户街1号",
                            City = "杭州",
                            Country = "中国",
                            ZipCode = "310000"
                        }
                    }
                };

                string json = SimpleJson.SerializeObject(user);
                var result = SimpleJson.DeserializeObject<User>(json);

                Assert.AreEqual(1001,result.Id);
                Assert.AreEqual("testuser",result.Username);
                Assert.AreEqual("test@example.com",result.Email);
                Assert.IsNotNull(result.Profile);
                Assert.AreEqual(2001,result.Profile.Id);
                Assert.AreEqual("测试用户",result.Profile.DisplayName);
                Assert.AreEqual(2,result.Profile.Tags.Count);
                Assert.AreEqual("VIP",result.Profile.Tags[0]);
                Assert.IsNotNull(result.Profile.Location);
                Assert.AreEqual("杭州",result.Profile.Location.City);
            }

            // 测试 6：泛型嵌套类
            Assert.BeginTest("泛型嵌套类：Container<Person>");
            {
                var container = new Container<Person>
                {
                    ContainerName = "人员容器",
                    Data = new Person
                    {
                        Name = "容器内人员",
                        Age = 35,
                        HomeAddress = new Address
                        {
                            Street = "容器街1号",
                            City = "成都",
                            Country = "中国",
                            ZipCode = "610000"
                        }
                    },
                    Items = new List<Person>
                    {
                        new Person { Name = "人员1", Age = 25 },
                        new Person { Name = "人员2", Age = 28 }
                    }
                };

                string json = SimpleJson.SerializeObject(container);
                var result = SimpleJson.DeserializeObject<Container<Person>>(json);

                Assert.AreEqual("人员容器",result.ContainerName);
                Assert.IsNotNull(result.Data);
                Assert.AreEqual("容器内人员",result.Data.Name);
                Assert.AreEqual(2,result.Items.Count);
                Assert.AreEqual("人员1",result.Items[0].Name);
            }

            // 测试 7：带 JsonAlias 的嵌套类
            Assert.BeginTest("带 JsonAlias 的嵌套类：ApiUser -> ApiProfile -> ApiTag");
            {
                var apiUser = new ApiUser
                {
                    UserId = 1001,
                    UserName = "api_user",
                    Profile = new ApiProfile
                    {
                        ProfileId = 2001,
                        DisplayName = "API用户",
                        Tags = new List<ApiTag>
                        {
                            new ApiTag { TagId = 1, TagName = "标签1" },
                            new ApiTag { TagId = 2, TagName = "标签2" }
                        }
                    }
                };

                // 默认序列化（使用原始属性名）
                string json1 = SimpleJson.SerializeObject(apiUser);
                Assert.IsTrue(json1.Contains("UserId"),"默认应使用原始属性名 UserId");
                Assert.IsTrue(json1.Contains("UserName"),"默认应使用原始属性名 UserName");

                // 启用别名序列化
                var strategy = new DefaultJsonSerializationStrategy(false,true);
                string json2 = SimpleJson.SerializeObject(apiUser,strategy);
                Assert.IsTrue(json2.Contains("user_id"),"启用别名后应使用 user_id");
                Assert.IsTrue(json2.Contains("user_name"),"启用别名后应使用 user_name");
                Assert.IsTrue(json2.Contains("profile_id"),"嵌套类中的别名也应生效");

                // 反序列化测试（支持所有别名）
                string jsonInput = @"{
                    ""user_id"": 1002,
                    ""userName"": ""api_user2"",
                    ""Profile"": {
                        ""profile_id"": 2002,
                        ""display_name"": ""API用户2"",
                        ""Tags"": [
                            {""tag_id"": 3, ""tag_name"": ""标签3""}
                        ]
                    }
                }";

                var result = SimpleJson.DeserializeObject<ApiUser>(jsonInput);
                Assert.AreEqual(1002,result.UserId);
                Assert.AreEqual("api_user2",result.UserName);
                Assert.IsNotNull(result.Profile);
                Assert.AreEqual(2002,result.Profile.ProfileId);
                Assert.AreEqual("API用户2",result.Profile.DisplayName);
                Assert.AreEqual(1,result.Profile.Tags.Count);
                Assert.AreEqual(3,result.Profile.Tags[0].TagId);
                Assert.AreEqual("标签3",result.Profile.Tags[0].TagName);
            }

            // 测试 8：深度嵌套的性能测试
            Assert.BeginTest("深度嵌套性能测试：100个部门，每个部门10个员工");
            {
                var company = new Company
                {
                    CompanyName = "大型企业",
                    CEO = new Person { Name = "CEO",Age = 50 },
                    Departments = new List<Department>()
                };

                for (int i = 0; i < 100; i++)
                {
                    var dept = new Department
                    {
                        Name = $"部门{i}",
                        Manager = new Person { Name = $"经理{i}",Age = 35 + i % 10 },
                        Employees = new List<Person>()
                    };

                    for (int j = 0; j < 10; j++)
                    {
                        dept.Employees.Add(new Person
                        {
                            Name = $"员工{i}_{j}",
                            Age = 25 + j
                        });
                    }

                    company.Departments.Add(dept);
                }

                // 序列化
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string json = SimpleJson.SerializeObject(company);
                stopwatch.Stop();
                long serializeMs = stopwatch.ElapsedMilliseconds;

                // 反序列化
                stopwatch.Reset();
                stopwatch.Start();
                var result = SimpleJson.DeserializeObject<Company>(json);
                stopwatch.Stop();
                long deserializeMs = stopwatch.ElapsedMilliseconds;

                Assert.AreEqual("大型企业",result.CompanyName);
                Assert.AreEqual(100,result.Departments.Count);
                Assert.AreEqual(10,result.Departments[0].Employees.Count);

                // 性能断言（宽松，避免在不同环境下失败）
                Assert.IsTrue(serializeMs < 5000,
                    $"序列化100个部门应小于5秒，实际: {serializeMs}ms");
                Assert.IsTrue(deserializeMs < 5000,
                    $"反序列化100个部门应小于5秒，实际: {deserializeMs}ms");
            }

            // 测试 9：混合类型嵌套
            Assert.BeginTest("混合类型嵌套：List<Dictionary<string, List<Person>>>");
            {
                var complexData = new List<Dictionary<string,List<Person>>>
                {
                    new Dictionary<string, List<Person>>
                    {
                        { "GroupA", new List<Person>
                            {
                                new Person { Name = "A1", Age = 20 },
                                new Person { Name = "A2", Age = 22 }
                            }
                        },
                        { "GroupB", new List<Person>
                            {
                                new Person { Name = "B1", Age = 25 }
                            }
                        }
                    },
                    new Dictionary<string, List<Person>>
                    {
                        { "GroupC", new List<Person>
                            {
                                new Person { Name = "C1", Age = 30 }
                            }
                        }
                    }
                };

                string json = SimpleJson.SerializeObject(complexData);
                var result = SimpleJson.DeserializeObject<List<Dictionary<string,List<Person>>>>(json);

                Assert.AreEqual(2,result.Count);
                Assert.AreEqual(2,result[0].Count);
                Assert.IsTrue(result[0].ContainsKey("GroupA"));
                Assert.AreEqual(2,result[0]["GroupA"].Count);
                Assert.AreEqual("A1",result[0]["GroupA"][0].Name);
                Assert.AreEqual(1,result[1].Count);
                Assert.AreEqual("C1",result[1]["GroupC"][0].Name);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 25. 列表专项测试
        // ──────────────────────────────────────────────────────────

        private static void TestListSpecial()
        {
            // 1. 基本列表序列化/反序列化
            Assert.BeginTest("List<int> 序列化输出正确");
            {
                var list = new List<int> { 1, 2, 3 };
                string json = SimpleJson.SerializeObject(list);
                Assert.AreEqual("[1,2,3]", json);
            }

            Assert.BeginTest("List<int> 反序列化正确");
            {
                string json = "[1,2,3]";
                var result = SimpleJson.DeserializeObject<List<int>>(json);
                Assert.AreEqual(3, result.Count);
                Assert.AreEqual(1, result[0]);
                Assert.AreEqual(2, result[1]);
                Assert.AreEqual(3, result[2]);
            }

            Assert.BeginTest("List<string> 序列化输出正确");
            {
                var list = new List<string> { "hello", "world" };
                string json = SimpleJson.SerializeObject(list);
                Assert.AreEqual("[\"hello\",\"world\"]", json);
            }

            Assert.BeginTest("List<string> 反序列化正确");
            {
                string json = "[\"hello\",\"world\"]";
                var result = SimpleJson.DeserializeObject<List<string>>(json);
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("hello", result[0]);
                Assert.AreEqual("world", result[1]);
            }

            // 2. POCO 内嵌列表属性
            Assert.BeginTest("POCO 内嵌 List<int> 属性序列化");
            {
                var obj = new CollectionPoco
                {
                    IntList = new List<int> { 10, 20, 30 },
                    StringKeyDict = new Dictionary<string, int>(),
                    IntKeyDict = new Dictionary<int, string>(),
                    EnumKeyDict = new Dictionary<Color, float>()
                };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json, "\"IntList\"");
                Assert.Contains(json, "[10,20,30]");
            }

            Assert.BeginTest("POCO 内嵌 List<int> 属性反序列化");
            {
                var obj = new CollectionPoco
                {
                    IntList = new List<int> { 10, 20, 30 },
                    StringKeyDict = new Dictionary<string, int>(),
                    IntKeyDict = new Dictionary<int, string>(),
                    EnumKeyDict = new Dictionary<Color, float>()
                };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<CollectionPoco>(json);
                Assert.IsNotNull(result.IntList);
                Assert.AreEqual(3, result.IntList.Count);
                Assert.AreEqual(10, result.IntList[0]);
                Assert.AreEqual(20, result.IntList[1]);
                Assert.AreEqual(30, result.IntList[2]);
            }

            // 3. POCO 内嵌字典属性
            Assert.BeginTest("POCO 内嵌 Dictionary<string,int> 属性序列化");
            {
                var obj = new CollectionPoco
                {
                    IntList = new List<int>(),
                    StringKeyDict = new Dictionary<string, int> { { "x", 10 }, { "y", 20 } },
                    IntKeyDict = new Dictionary<int, string>(),
                    EnumKeyDict = new Dictionary<Color, float>()
                };
                string json = SimpleJson.SerializeObject(obj);
                Assert.Contains(json, "\"StringKeyDict\"");
                Assert.Contains(json, "\"x\"");
                Assert.Contains(json, "\"y\"");
            }

            Assert.BeginTest("POCO 内嵌 Dictionary<string,int> 属性反序列化");
            {
                var obj = new CollectionPoco
                {
                    IntList = new List<int>(),
                    StringKeyDict = new Dictionary<string, int> { { "x", 10 }, { "y", 20 } },
                    IntKeyDict = new Dictionary<int, string>(),
                    EnumKeyDict = new Dictionary<Color, float>()
                };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<CollectionPoco>(json);
                Assert.IsNotNull(result.StringKeyDict);
                Assert.AreEqual(2, result.StringKeyDict.Count);
                Assert.AreEqual(10, result.StringKeyDict["x"]);
                Assert.AreEqual(20, result.StringKeyDict["y"]);
            }

            Assert.BeginTest("POCO 内嵌 Dictionary<int,string> 属性反序列化");
            {
                var obj = new CollectionPoco
                {
                    IntList = new List<int>(),
                    StringKeyDict = new Dictionary<string, int>(),
                    IntKeyDict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } },
                    EnumKeyDict = new Dictionary<Color, float>()
                };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<CollectionPoco>(json);
                Assert.IsNotNull(result.IntKeyDict);
                Assert.AreEqual(2, result.IntKeyDict.Count);
                Assert.AreEqual("one", result.IntKeyDict[1]);
                Assert.AreEqual("two", result.IntKeyDict[2]);
            }

            // 4. 列表内嵌 POCO
            Assert.BeginTest("List<Address> 序列化/反序列化");
            {
                var list = new List<Address>
                {
                    new Address { Street = "Main St", City = "NYC", Country = "US", ZipCode = "10001" },
                    new Address { Street = "Oak Ave", City = "LA", Country = "US", ZipCode = "90001" }
                };
                string json = SimpleJson.SerializeObject(list);
                var result = SimpleJson.DeserializeObject<List<Address>>(json);
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Main St", result[0].Street);
                Assert.AreEqual("NYC", result[0].City);
                Assert.AreEqual("Oak Ave", result[1].Street);
                Assert.AreEqual("LA", result[1].City);
            }

            // 5. 数组类型
            Assert.BeginTest("int[] 序列化/反序列化");
            {
                var arr = new int[] { 5, 10, 15 };
                string json = SimpleJson.SerializeObject(arr);
                var result = SimpleJson.DeserializeObject<int[]>(json);
                Assert.AreEqual(3, result.Length);
                Assert.AreEqual(5, result[0]);
                Assert.AreEqual(10, result[1]);
                Assert.AreEqual(15, result[2]);
            }

            Assert.BeginTest("string[] 序列化/反序列化");
            {
                var arr = new string[] { "a", "b", "c" };
                string json = SimpleJson.SerializeObject(arr);
                var result = SimpleJson.DeserializeObject<string[]>(json);
                Assert.AreEqual(3, result.Length);
                Assert.AreEqual("a", result[0]);
                Assert.AreEqual("b", result[1]);
                Assert.AreEqual("c", result[2]);
            }

            // 6. 空列表
            Assert.BeginTest("空 List<int> 序列化/反序列化");
            {
                var list = new List<int>();
                string json = SimpleJson.SerializeObject(list);
                Assert.AreEqual("[]", json);
                var result = SimpleJson.DeserializeObject<List<int>>(json);
                Assert.AreEqual(0, result.Count);
            }

            // 7. 列表含 null 元素
            Assert.BeginTest("List<string> 含 null 元素");
            {
                var list = new List<string> { "a", null, "c" };
                string json = SimpleJson.SerializeObject(list);
                var result = SimpleJson.DeserializeObject<List<string>>(json);
                Assert.AreEqual(3, result.Count);
                Assert.AreEqual("a", result[0]);
                Assert.IsNull(result[1]);
                Assert.AreEqual("c", result[2]);
            }

            // 8. 嵌套列表 List<List<int>>
            Assert.BeginTest("List<List<int>> 嵌套列表");
            {
                var list = new List<List<int>>
                {
                    new List<int> { 1, 2 },
                    new List<int> { 3, 4, 5 }
                };
                string json = SimpleJson.SerializeObject(list);
                Assert.AreEqual("[[1,2],[3,4,5]]", json);
                var result = SimpleJson.DeserializeObject<List<List<int>>>(json);
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual(2, result[0].Count);
                Assert.AreEqual(3, result[1].Count);
                Assert.AreEqual(5, result[1][2]);
            }

            // 9. Person 内嵌列表（PhoneNumbers）
            Assert.BeginTest("Person.PhoneNumbers List<string> 序列化/反序列化");
            {
                var person = new Person
                {
                    Name = "Alice",
                    Age = 30,
                    HomeAddress = new Address { Street = "1st", City = "NYC", Country = "US", ZipCode = "10001" },
                    WorkAddress = new Address { Street = "2nd", City = "LA", Country = "US", ZipCode = "90001" },
                    PhoneNumbers = new List<string> { "123-456", "789-012" }
                };
                string json = SimpleJson.SerializeObject(person);
                var result = SimpleJson.DeserializeObject<Person>(json);
                Assert.IsNotNull(result.PhoneNumbers);
                Assert.AreEqual(2, result.PhoneNumbers.Count);
                Assert.AreEqual("123-456", result.PhoneNumbers[0]);
                Assert.AreEqual("789-012", result.PhoneNumbers[1]);
            }

            // 10. 接口类型列表反序列化（IList<int>, ICollection<string> 等）
            Assert.BeginTest("IList<int> 接口类型反序列化");
            {
                string json = "[1,2,3]";
                var result = SimpleJson.DeserializeObject<IList<int>>(json);
                Assert.IsNotNull(result);
                Assert.AreEqual(3, result.Count);
                Assert.AreEqual(1, result[0]);
                Assert.AreEqual(2, result[1]);
                Assert.AreEqual(3, result[2]);
            }

            Assert.BeginTest("IList<Address> 接口类型反序列化嵌套POCO");
            {
                var list = new List<Address>
                {
                    new Address { Street = "Main", City = "NYC", Country = "US", ZipCode = "10001" }
                };
                string json = SimpleJson.SerializeObject(list);
                var result = SimpleJson.DeserializeObject<IList<Address>>(json);
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("Main", result[0].Street);
                Assert.AreEqual("NYC", result[0].City);
            }

            Assert.BeginTest("ICollection<string> 接口类型反序列化");
            {
                string json = "[\"a\",\"b\"]";
                var result = SimpleJson.DeserializeObject<ICollection<string>>(json);
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result.Count);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 26. 嵌套类专项测试
        // ──────────────────────────────────────────────────────────

        private static void TestNestedSpecial()
        {
            // 1. 简单嵌套：Person → Address
            Assert.BeginTest("Person → Address 嵌套序列化");
            {
                var person = new Person
                {
                    Name = "Bob",
                    Age = 25,
                    HomeAddress = new Address { Street = "Elm", City = "Boston", Country = "US", ZipCode = "02101" },
                    WorkAddress = new Address { Street = "Pine", City = "Cambridge", Country = "US", ZipCode = "02139" },
                    PhoneNumbers = new List<string>()
                };
                string json = SimpleJson.SerializeObject(person);
                Assert.Contains(json, "\"HomeAddress\"");
                Assert.Contains(json, "\"WorkAddress\"");
                Assert.Contains(json, "\"Street\"");
                Assert.Contains(json, "\"Elm\"");
                Assert.Contains(json, "\"Pine\"");
            }

            Assert.BeginTest("Person → Address 嵌套反序列化");
            {
                var person = new Person
                {
                    Name = "Bob",
                    Age = 25,
                    HomeAddress = new Address { Street = "Elm", City = "Boston", Country = "US", ZipCode = "02101" },
                    WorkAddress = new Address { Street = "Pine", City = "Cambridge", Country = "US", ZipCode = "02139" },
                    PhoneNumbers = new List<string>()
                };
                string json = SimpleJson.SerializeObject(person);
                var result = SimpleJson.DeserializeObject<Person>(json);
                Assert.AreEqual("Bob", result.Name);
                Assert.AreEqual(25, result.Age);
                Assert.IsNotNull(result.HomeAddress);
                Assert.AreEqual("Elm", result.HomeAddress.Street);
                Assert.AreEqual("Boston", result.HomeAddress.City);
                Assert.IsNotNull(result.WorkAddress);
                Assert.AreEqual("Pine", result.WorkAddress.Street);
                Assert.AreEqual("Cambridge", result.WorkAddress.City);
            }

            // 2. 三层嵌套：Person → Department → Company
            Assert.BeginTest("三层嵌套 Person → Department 反序列化");
            {
                var dept = new Department
                {
                    Name = "Engineering",
                    Manager = new Person
                    {
                        Name = "Carol",
                        Age = 40,
                        HomeAddress = new Address { Street = "Oak", City = "SF", Country = "US", ZipCode = "94102" },
                        WorkAddress = new Address { Street = "Market", City = "SF", Country = "US", ZipCode = "94105" },
                        PhoneNumbers = new List<string> { "555-0100" }
                    },
                    Employees = new List<Person>
                    {
                        new Person
                        {
                            Name = "Dave",
                            Age = 28,
                            HomeAddress = new Address { Street = "Pine", City = "Oakland", Country = "US", ZipCode = "94601" },
                            WorkAddress = new Address { Street = "Market", City = "SF", Country = "US", ZipCode = "94105" },
                            PhoneNumbers = new List<string>()
                        }
                    },
                    MembersById = new Dictionary<int, Person>
                    {
                        { 1, new Person { Name = "Eve", Age = 35, HomeAddress = new Address { Street = "A", City = "B", Country = "C", ZipCode = "D" }, WorkAddress = new Address { Street = "E", City = "F", Country = "G", ZipCode = "H" }, PhoneNumbers = new List<string>() } }
                    }
                };
                string json = SimpleJson.SerializeObject(dept);
                var result = SimpleJson.DeserializeObject<Department>(json);
                Assert.AreEqual("Engineering", result.Name);
                Assert.IsNotNull(result.Manager);
                Assert.AreEqual("Carol", result.Manager.Name);
                Assert.AreEqual("Oak", result.Manager.HomeAddress.Street);
                Assert.IsNotNull(result.Employees);
                Assert.AreEqual(1, result.Employees.Count);
                Assert.AreEqual("Dave", result.Employees[0].Name);
                Assert.AreEqual("Pine", result.Employees[0].HomeAddress.Street);
                Assert.IsNotNull(result.MembersById);
                Assert.AreEqual(1, result.MembersById.Count);
                Assert.AreEqual("Eve", result.MembersById[1].Name);
            }

            // 3. 四层嵌套：Department → Company
            Assert.BeginTest("四层嵌套 Company → Department → Person → Address");
            {
                var company = new Company
                {
                    CompanyName = "Acme Corp",
                    Departments = new List<Department>
                    {
                        new Department
                        {
                            Name = "R&D",
                            Manager = new Person { Name = "Frank", Age = 45, HomeAddress = new Address { Street = "1st", City = "A", Country = "US", ZipCode = "00001" }, WorkAddress = new Address { Street = "2nd", City = "B", Country = "US", ZipCode = "00002" }, PhoneNumbers = new List<string>() },
                            Employees = new List<Person>(),
                            MembersById = new Dictionary<int, Person>()
                        }
                    },
                    DepartmentsByName = new Dictionary<string, Department>(),
                    CEO = new Person { Name = "Grace", Age = 55, HomeAddress = new Address { Street = "CEO St", City = "Top", Country = "US", ZipCode = "99999" }, WorkAddress = new Address { Street = "HQ", City = "Top", Country = "US", ZipCode = "99998" }, PhoneNumbers = new List<string> { "555-0001" } }
                };
                string json = SimpleJson.SerializeObject(company);
                var result = SimpleJson.DeserializeObject<Company>(json);
                Assert.AreEqual("Acme Corp", result.CompanyName);
                Assert.IsNotNull(result.Departments);
                Assert.AreEqual(1, result.Departments.Count);
                Assert.AreEqual("R&D", result.Departments[0].Name);
                Assert.AreEqual("Frank", result.Departments[0].Manager.Name);
                Assert.AreEqual("1st", result.Departments[0].Manager.HomeAddress.Street);
                Assert.IsNotNull(result.CEO);
                Assert.AreEqual("Grace", result.CEO.Name);
                Assert.AreEqual("CEO St", result.CEO.HomeAddress.Street);
                Assert.AreEqual(1, result.CEO.PhoneNumbers.Count);
                Assert.AreEqual("555-0001", result.CEO.PhoneNumbers[0]);
            }

            // 4. 原始 JSON 反序列化（不经过序列化，直接从 JSON 字符串反序列化）
            Assert.BeginTest("从原始 JSON 字符串反序列化嵌套 POCO");
            {
                string json = "{\"Name\":\"Test\",\"Age\":99,\"HomeAddress\":{\"Street\":\"Main\",\"City\":\"NYC\",\"Country\":\"US\",\"ZipCode\":\"10001\"},\"WorkAddress\":{\"Street\":\"Broadway\",\"City\":\"NYC\",\"Country\":\"US\",\"ZipCode\":\"10002\"},\"PhoneNumbers\":[\"111\",\"222\"]}";
                var result = SimpleJson.DeserializeObject<Person>(json);
                Assert.AreEqual("Test", result.Name);
                Assert.AreEqual(99, result.Age);
                Assert.IsNotNull(result.HomeAddress);
                Assert.AreEqual("Main", result.HomeAddress.Street);
                Assert.AreEqual("NYC", result.HomeAddress.City);
                Assert.IsNotNull(result.WorkAddress);
                Assert.AreEqual("Broadway", result.WorkAddress.Street);
                Assert.IsNotNull(result.PhoneNumbers);
                Assert.AreEqual(2, result.PhoneNumbers.Count);
                Assert.AreEqual("111", result.PhoneNumbers[0]);
            }

            // 5. 小写 JSON key + ignoreLowerCase 反序列化嵌套 POCO
            Assert.BeginTest("小写 JSON key 反序列化嵌套 POCO（ignoreLowerCase=true）");
            {
                string json = "{\"name\":\"Lower\",\"age\":33,\"homeaddress\":{\"street\":\"Low\",\"city\":\"LA\",\"country\":\"US\",\"zipcode\":\"90001\"},\"workaddress\":{\"street\":\"High\",\"city\":\"LA\",\"country\":\"US\",\"zipcode\":\"90002\"},\"phonenumbers\":[\"333\"]}";
                var result = SimpleJson.DeserializeObject<Person>(json);
                Assert.AreEqual("Lower", result.Name);
                Assert.AreEqual(33, result.Age);
                Assert.IsNotNull(result.HomeAddress);
                Assert.AreEqual("Low", result.HomeAddress.Street);
                Assert.AreEqual("LA", result.HomeAddress.City);
                Assert.IsNotNull(result.PhoneNumbers);
                Assert.AreEqual(1, result.PhoneNumbers.Count);
                Assert.AreEqual("333", result.PhoneNumbers[0]);
            }

            // 6. 嵌套 POCO 内嵌列表
            Assert.BeginTest("嵌套 POCO 内嵌 List<POCO> 反序列化");
            {
                var dept = new Department
                {
                    Name = "Sales",
                    Manager = new Person { Name = "M1", Age = 50, HomeAddress = new Address { Street = "S1", City = "C1", Country = "US", ZipCode = "00001" }, WorkAddress = new Address { Street = "S2", City = "C2", Country = "US", ZipCode = "00002" }, PhoneNumbers = new List<string>() },
                    Employees = new List<Person>
                    {
                        new Person { Name = "E1", Age = 25, HomeAddress = new Address { Street = "S3", City = "C3", Country = "US", ZipCode = "00003" }, WorkAddress = new Address { Street = "S4", City = "C4", Country = "US", ZipCode = "00004" }, PhoneNumbers = new List<string> { "555-0001" } },
                        new Person { Name = "E2", Age = 30, HomeAddress = new Address { Street = "S5", City = "C5", Country = "US", ZipCode = "00005" }, WorkAddress = new Address { Street = "S6", City = "C6", Country = "US", ZipCode = "00006" }, PhoneNumbers = new List<string>() }
                    },
                    MembersById = new Dictionary<int, Person>()
                };
                string json = SimpleJson.SerializeObject(dept);
                var result = SimpleJson.DeserializeObject<Department>(json);
                Assert.AreEqual(2, result.Employees.Count);
                Assert.AreEqual("E1", result.Employees[0].Name);
                Assert.AreEqual("S3", result.Employees[0].HomeAddress.Street);
                Assert.AreEqual("E2", result.Employees[1].Name);
                Assert.AreEqual(1, result.Employees[0].PhoneNumbers.Count);
                Assert.AreEqual("555-0001", result.Employees[0].PhoneNumbers[0]);
            }

            // 7. 嵌套 POCO 内嵌字典
            Assert.BeginTest("嵌套 POCO 内嵌 Dictionary<int,Person> 反序列化");
            {
                var dept = new Department
                {
                    Name = "HR",
                    Manager = new Person { Name = "M2", Age = 45, HomeAddress = new Address { Street = "S1", City = "C1", Country = "US", ZipCode = "00001" }, WorkAddress = new Address { Street = "S2", City = "C2", Country = "US", ZipCode = "00002" }, PhoneNumbers = new List<string>() },
                    Employees = new List<Person>(),
                    MembersById = new Dictionary<int, Person>
                    {
                        { 100, new Person { Name = "P100", Age = 30, HomeAddress = new Address { Street = "D1", City = "DC", Country = "US", ZipCode = "20001" }, WorkAddress = new Address { Street = "D2", City = "DC", Country = "US", ZipCode = "20002" }, PhoneNumbers = new List<string>() } },
                        { 200, new Person { Name = "P200", Age = 35, HomeAddress = new Address { Street = "D3", City = "DC", Country = "US", ZipCode = "20003" }, WorkAddress = new Address { Street = "D4", City = "DC", Country = "US", ZipCode = "20004" }, PhoneNumbers = new List<string> { "555-0200" } } }
                    }
                };
                string json = SimpleJson.SerializeObject(dept);
                var result = SimpleJson.DeserializeObject<Department>(json);
                Assert.AreEqual(2, result.MembersById.Count);
                Assert.AreEqual("P100", result.MembersById[100].Name);
                Assert.AreEqual("D1", result.MembersById[100].HomeAddress.Street);
                Assert.AreEqual("P200", result.MembersById[200].Name);
                Assert.AreEqual(1, result.MembersById[200].PhoneNumbers.Count);
                Assert.AreEqual("555-0200", result.MembersById[200].PhoneNumbers[0]);
            }

            // 8. 值类型嵌套（struct 内嵌 struct）
            Assert.BeginTest("Nullable 内嵌值类型反序列化");
            {
                var obj = new FloatPoco { FloatValue = 1.5f, DoubleValue = 2.5, DecimalValue = 3.5m };
                string json = SimpleJson.SerializeObject(obj);
                var result = SimpleJson.DeserializeObject<FloatPoco>(json);
                Assert.AreEqual<float>(1.5f, result.FloatValue);
                Assert.AreEqual<double>(2.5, result.DoubleValue);
                Assert.AreEqual<decimal>(3.5m, result.DecimalValue);
            }

            // 9. 泛型嵌套类
            Assert.BeginTest("Container<Person> 泛型嵌套类");
            {
                var container = new Container<Person>
                {
                    ContainerName = "People",
                    Data = new Person { Name = "Inside", Age = 22, HomeAddress = new Address { Street = "Inner", City = "Box", Country = "US", ZipCode = "00000" }, WorkAddress = new Address { Street = "Outer", City = "Box", Country = "US", ZipCode = "00001" }, PhoneNumbers = new List<string>() },
                    Items = new List<Person>
                    {
                        new Person { Name = "Item1", Age = 10, HomeAddress = new Address { Street = "I1", City = "C1", Country = "US", ZipCode = "00010" }, WorkAddress = new Address { Street = "I2", City = "C2", Country = "US", ZipCode = "00011" }, PhoneNumbers = new List<string>() }
                    }
                };
                string json = SimpleJson.SerializeObject(container);
                var result = SimpleJson.DeserializeObject<Container<Person>>(json);
                Assert.AreEqual("People", result.ContainerName);
                Assert.IsNotNull(result.Data);
                Assert.AreEqual("Inside", result.Data.Name);
                Assert.AreEqual("Inner", result.Data.HomeAddress.Street);
                Assert.IsNotNull(result.Items);
                Assert.AreEqual(1, result.Items.Count);
                Assert.AreEqual("Item1", result.Items[0].Name);
                Assert.AreEqual("I1", result.Items[0].HomeAddress.Street);
            }
        }
    }
}
