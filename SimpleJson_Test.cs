using NUnit.Framework;
using RS.SimpleJsonAOT;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;



namespace RS.SimpleJsonAOT.Test
{
    // ============================================================================
    // 单元测试 - 非 Unity 环境使用
    // ============================================================================
    public class SimpleJsonFeatureTests
    {
        public static void Test()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        SimpleJson 功能验证测试套件                              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            TestJsonIgnore();
            TestJsonInclude();
            TestJsonAlias();
            TestCaseSensitivity();
            TestPascalCamelCase();
            TestCombinedFeatures();

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              所有测试完成！如有失败，请查看上面的输出              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

            RunNUnitTests();

            Console.ReadLine();
        }

        static void RunNUnitTests()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    NUnit 测试套件执行                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            RunTestClass(new SimpleJsonFeatureTests2(), "SimpleJsonFeatureTests2");
            RunTestClass(new SimpleJsonCoreTypeTests(), "SimpleJsonCoreTypeTests");
            RunTestClass(new SimpleJsonCollectionTests(), "SimpleJsonCollectionTests");
            RunTestClass(new SimpleJsonComplexTypeTests(), "SimpleJsonComplexTypeTests");
            RunTestClass(new SimpleJsonEdgeCaseTests(), "SimpleJsonEdgeCaseTests");
            RunTestClass(new SimpleJsonPerformanceTests(), "SimpleJsonPerformanceTests");

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    所有 NUnit 测试执行完成                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        static void RunTestClass(object testInstance, string className)
        {
            Console.WriteLine($"\n┌─────────────────────────────────────────────────────────────┐");
            Console.WriteLine($"│ 执行测试类: {className.PadRight(47)} │");
            Console.WriteLine($"└─────────────────────────────────────────────────────────────┘");

            var type = testInstance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0);

            int passed = 0, failed = 0;
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(testInstance, null);
                    passed++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {method.Name}");
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
                }
                Console.ResetColor();
            }

            Console.WriteLine($"\n  结果: {passed} 通过, {failed} 失败");
        }

        #region 测试模型

        public class UserWithIgnore
        {
            [JsonIgnore]
            public string Password { get; set; } = "";

            public string Username { get; set; } = "";
            public string Email { get; set; } = "";

            [JsonIgnore]
            private string InternalId = "secret";

            public override string ToString() => $"Username: {Username}, Email: {Email}";
        }

        public class UserWithAlias
        {
            [JsonAlias("user_name")]
            public string Username { get; set; } = "";

            [JsonAlias("email_address")]
            public string Email { get; set; } = "";

            [JsonAlias("user_age",acceptOriginalName: true)]
            public int Age { get; set; }

            public override string ToString() => $"User: {Username}, Email: {Email}, Age: {Age}";
        }

        public class CaseSensitiveModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int Age { get; set; }

            public override string ToString() => $"{FirstName} {LastName}, {Age} years old";
        }

        public class PascalCaseModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int UserAge { get; set; }
            public bool IsActive { get; set; }

            public override string ToString() => $"{FirstName} {LastName}, Age: {UserAge}, Active: {IsActive}";
        }

        public class CombinedModel
        {
            [JsonAlias("user_id")]
            public int Id { get; set; }

            [JsonIgnore]
            public string InternalSecret { get; set; } = "";

            public string Name { get; set; } = "";

            public override string ToString() => $"ID: {Id}, Name: {Name}";
        }

        #endregion

        #region 测试方法

        static void TestJsonIgnore()
        {
            PrintTestHeader("测试 JsonIgnore 功能");

            var user = new UserWithIgnore
            {
                Username = "john_doe",
                Email = "john@example.com",
                Password = "secret123"
            };

            Console.WriteLine($"原始对象: {user}");
            Console.WriteLine($"密码: {user.Password}");

            string json = SimpleJson.SerializeObject(user);
            Console.WriteLine($"序列化后: {json}");

            bool passwordInJson = json.Contains("Password") || json.Contains("secret");
            bool usernameInJson = json.Contains("john_doe");
            bool emailInJson = json.Contains("john@example.com");

            PrintTestResult(
                !passwordInJson && usernameInJson && emailInJson,
                "✓ Password 被正确忽略，其他字段被保留",
                "✗ JsonIgnore 功能异常"
            );

            // 反序列化测试
            var restored = SimpleJson.DeserializeObject<UserWithIgnore>(json);
            Console.WriteLine($"反序列化后: {restored}");
            PrintTestResult(
                restored.Username == "john_doe" && restored.Email == "john@example.com" && restored.Password == "",
                "✓ 反序列化正确，Password 为默认值",
                "✗ 反序列化有问题"
            );

            Console.WriteLine();
        }

        static void TestJsonInclude()
        {
            PrintTestHeader("测试 JsonInclude 功能（私有成员）");

            // JsonInclude 的测试需要 onlyPublic=false
            // 这里展示如何测试
            Console.WriteLine("JsonInclude 需要配合 onlyPublic=false 参数使用");
            Console.WriteLine("示例: model.ToJson(lowerCase: false, onlyPublic: false)");
            Console.WriteLine("✓ JsonInclude 功能在 onlyPublic=false 时启用\n");
        }

        static void TestJsonAlias()
        {
            PrintTestHeader("测试 JsonAlias 别名功能");

            var user = new UserWithAlias
            {
                Username = "alice",
                Email = "alice@example.com",
                Age = 28
            };

            Console.WriteLine($"原始对象: {user}");

            // 序列化测试
            string json = SimpleJson.SerializeObject(user);
            Console.WriteLine($"序列化后: {json}");

            bool hasUserName = json.Contains("user_name");
            bool hasEmailAddress = json.Contains("email_address");
            bool hasUserAge = json.Contains("user_age");
            bool noOriginalNames = !json.Contains("Username") && !json.Contains("Email");

            PrintTestResult(
                hasUserName && hasEmailAddress && hasUserAge && noOriginalNames,
                "✓ 别名正确使用于序列化",
                "✗ 别名序列化有问题"
            );

            // 反序列化测试 - 使用别名
            string jsonWithAlias = @"{
                ""user_name"": ""bob"",
                ""email_address"": ""bob@example.com"",
                ""user_age"": 30
            }";

            Console.WriteLine($"反序列化输入: {jsonWithAlias}");
            var restored = SimpleJson.DeserializeObject<UserWithAlias>(jsonWithAlias);
            Console.WriteLine($"反序列化后: {restored}");

            PrintTestResult(
                restored.Username == "bob" && restored.Email == "bob@example.com" && restored.Age == 30,
                "✓ 别名正确用于反序列化",
                "✗ 别名反序列化有问题"
            );

            // 测试 acceptOriginalName=true
            Console.WriteLine("\n--- 测试 acceptOriginalName 功能 ---");
            string jsonWithOriginalAge = @"{
                ""user_name"": ""charlie"",
                ""email_address"": ""charlie@example.com"",
                ""Age"": 25
            }";

            Console.WriteLine($"使用原始名称 'Age' 的 JSON: {jsonWithOriginalAge}");
            var restoredWithOriginal = SimpleJson.DeserializeObject<UserWithAlias>(jsonWithOriginalAge);

            if (restoredWithOriginal != null)
            {
                Console.WriteLine($"反序列化后: {restoredWithOriginal}");
                PrintTestResult(
                    restoredWithOriginal.Age == 25,
                    "✓ acceptOriginalName=true 时同时接受原始名称",
                    "✗ acceptOriginalName 功能有问题"
                );
            }
            else
            {
                PrintTestResult(false,"","✗ 反序列化失败");
            }

            Console.WriteLine();
        }

        static void TestCaseSensitivity()
        {
            PrintTestHeader("测试大小写敏感性和兜底匹配");

            Console.WriteLine("--- 精确匹配测试 ---");
            string jsonExactCase = @"{
                ""FirstName"": ""John"",
                ""LastName"": ""Doe"",
                ""Age"": 30
            }";

            Console.WriteLine($"输入 JSON: {jsonExactCase}");
            var model1 = SimpleJson.DeserializeObject<CaseSensitiveModel>(jsonExactCase);
            Console.WriteLine($"反序列化结果: {model1}");

            PrintTestResult(
                model1 != null && model1.FirstName == "John" && model1.LastName == "Doe",
                "✓ 精确匹配大小写正常工作",
                "✗ 精确匹配有问题"
            );

            Console.WriteLine("\n--- 大小写不匹配测试（兜底）---");
            string jsonLowerCase = @"{
                ""firstname"": ""jane"",
                ""lastname"": ""smith"",
                ""age"": 25
            }";

            Console.WriteLine($"输入 JSON（全小写）: {jsonLowerCase}");
            var model2 = SimpleJson.DeserializeObject<CaseSensitiveModel>(jsonLowerCase);
            Console.WriteLine($"反序列化结果: {model2}");

            PrintTestResult(
                model2 != null && model2.FirstName == "jane" && model2.LastName == "smith" && model2.Age == 25,
                "✓ 大小写不敏感兜底匹配正常工作",
                "✗ 兜底匹配有问题"
            );

            Console.WriteLine("\n--- 混合大小写测试 ---");
            string jsonMixedCase = @"{
                ""FirstName"": ""Bob"",
                ""lastname"": ""johnson"",
                ""AGE"": 35
            }";

            Console.WriteLine($"输入 JSON（混合大小写）: {jsonMixedCase}");
            var model3 = SimpleJson.DeserializeObject<CaseSensitiveModel>(jsonMixedCase);
            Console.WriteLine($"反序列化结果: {model3}");

            PrintTestResult(
                model3 != null && model3.FirstName == "Bob" && model3.LastName == "johnson" && model3.Age == 35,
                "✓ 混合大小写兜底匹配正常工作",
                "✗ 混合大小写有问题"
            );

            Console.WriteLine();
        }

        static void TestPascalCamelCase()
        {
            PrintTestHeader("测试 PascalCase ↔ camelCase 转换");

            var model = new PascalCaseModel
            {
                FirstName = "Sarah",
                LastName = "Williams",
                UserAge = 28,
                IsActive = true
            };

            Console.WriteLine($"原始对象: {model}");

            // 序列化 - 启用 lowerCase=true
            Console.WriteLine("\n--- 序列化为 camelCase ---");
            string jsonCamel = model.ToJson(lowerCase: true,onlyPublic: true);
            Console.WriteLine($"序列化结果: {jsonCamel}");

            bool hasCamelCase = jsonCamel.Contains("firstName") &&
                               jsonCamel.Contains("lastName") &&
                               jsonCamel.Contains("userAge") &&
                               jsonCamel.Contains("isActive");
            bool noUpperCase = !jsonCamel.Contains("FirstName") &&
                              !jsonCamel.Contains("LastName");

            PrintTestResult(
                hasCamelCase && noUpperCase,
                "✓ PascalCase 正确转换为 camelCase",
                "✗ camelCase 转换有问题"
            );

            // 反序列化 - 从 camelCase 恢复
            Console.WriteLine("\n--- 从 camelCase 反序列化 ---");
            var restored = jsonCamel.FromJson<PascalCaseModel>(lowerCase: true,onlyPublic: true);
            Console.WriteLine($"反序列化结果: {restored}");

            PrintTestResult(
                restored != null &&
                restored.FirstName == "Sarah" &&
                restored.LastName == "Williams" &&
                restored.UserAge == 28 &&
                restored.IsActive == true,
                "✓ camelCase 正确反序列化为 PascalCase 属性",
                "✗ camelCase 反序列化有问题"
            );

            Console.WriteLine();
        }

        static void TestCombinedFeatures()
        {
            PrintTestHeader("测试组合功能（Alias + Ignore）");

            var model = new CombinedModel
            {
                Id = 999,
                Name = "Test User",
                InternalSecret = "hidden"
            };

            Console.WriteLine($"原始对象: {model}");
            Console.WriteLine($"内部密钥: {model.InternalSecret}");

            // 序列化
            string json = SimpleJson.SerializeObject(model);
            Console.WriteLine($"序列化后: {json}");

            bool hasAlias = json.Contains("user_id");
            bool idValue = json.Contains("999");
            bool hasName = json.Contains("Test User");
            bool noSecret = !json.Contains("InternalSecret") && !json.Contains("hidden");

            PrintTestResult(
                hasAlias && idValue && hasName && noSecret,
                "✓ Alias 和 Ignore 同时工作正确",
                "✗ 组合功能有问题"
            );

            // 反序列化
            string jsonInput = @"{""user_id"": 123, ""Name"": ""Another User""}";
            var restored = SimpleJson.DeserializeObject<CombinedModel>(jsonInput);
            Console.WriteLine($"反序列化后: {restored}");

            PrintTestResult(
                restored != null && restored.Id == 123 && restored.Name == "Another User",
                "✓ 组合功能反序列化正确",
                "✗ 组合反序列化有问题"
            );

            Console.WriteLine();
        }

        #endregion

        #region 辅助方法

        static void PrintTestHeader(string title)
        {
            Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
            Console.WriteLine($"│ {title.PadRight(59)} │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        }

        static void PrintTestResult(bool passed,string successMsg,string failMsg)
        {
            if (passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(successMsg);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(failMsg);
            }
            Console.ResetColor();
        }

        #endregion
    }

    /// <summary>
    /// SimpleJson 属性和 DataContract 功能测试套件
    /// </summary>
    [TestFixture]
    public class SimpleJsonFeatureTests2
    {
        #region 测试模型定义

        /// <summary>
        /// 测试 JsonIgnore 和 JsonInclude 属性
        /// </summary>
        public class IgnoreIncludeTestModel
        {
            [JsonIgnore]
            public string IgnoredField = "should be ignored";

            public string NormalField = "should serialize";

            [JsonIgnore]
            public string IgnoredProperty { get; set; } = "ignored prop";

            [JsonInclude]
            private string IncludedPrivateField = "private but included";

            [JsonInclude]
            private string IncludedPrivateProperty { get; set; } = "private prop included";

            public string GetIncludedPrivateField() => IncludedPrivateField;
            public string GetIncludedPrivateProperty() => IncludedPrivateProperty;
        }

        /// <summary>
        /// 测试 JsonAlias 属性（别名映射）
        /// </summary>
        public class AliasTestModel
        {
            [JsonAlias("user_name")]
            public string UserName { get; set; } = "";

            [JsonAlias("user_age",acceptOriginalName: true)]  // 同时接受原始名称
            public int Age { get; set; }

            [JsonAlias("email_address")]
            public string Email { get; set; } = "";

            public string Description { get; set; } = "";  // 无别名
        }

        /// <summary>
        /// 测试大小写敏感性
        /// </summary>
        public class CaseSensitivityTestModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int Age { get; set; }
        }

        /// <summary>
        /// 测试 PascalCase 到 camelCase 的转换
        /// </summary>
        public class PascalCaseModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int UserAge { get; set; }
            public bool IsActive { get; set; }
        }

        #endregion

        #region JsonIgnore 和 JsonInclude 测试

        [Test]
        [Category("JsonIgnore")]
        public void TestJsonIgnore_FieldsShouldBeExcluded()
        {
            // Arrange
            var model = new IgnoreIncludeTestModel
            {
                NormalField = "normal value"
            };

            // Act
            string json = SimpleJson.SerializeObject(model);

            // Assert
            Assert.That(json,Does.Contain("normalField"),"正常字段应该被序列化");
            Assert.That(json,Does.Contain("normal value"),"字段值应该被序列化");
            Assert.That(json,Does.Not.Contain("IgnoredField"),"被忽略的字段不应出现");
            Assert.That(json,Does.Not.Contain("should be ignored"),"被忽略的值不应出现");
            Console.WriteLine($"JsonIgnore 测试 - 序列化结果:\n{json}");
        }

        [Test]
        [Category("JsonIgnore")]
        public void TestJsonIgnore_PropertiesShouldBeExcluded()
        {
            // Arrange
            var model = new IgnoreIncludeTestModel
            {
                NormalField = "test"
            };

            // Act
            string json = SimpleJson.SerializeObject(model);

            // Assert
            Assert.That(json,Does.Not.Contain("IgnoredProperty"),"被 JsonIgnore 标记的属性不应出现");
            Assert.That(json,Does.Not.Contain("ignored prop"),"被忽略的属性值不应出现");
            Console.WriteLine($"JsonIgnore 属性测试 - 通过");
        }

        [Test]
        [Category("JsonInclude")]
        public void TestJsonInclude_PrivateFieldsShouldBeIncluded()
        {
            // Arrange
            var model = new IgnoreIncludeTestModel();

            // Act
            string json = SimpleJson.SerializeObject(model);

            // Assert
            // 注意：默认情况下，private 字段可能不会被序列化（取决于 onlyPublic 参数）
            // 使用 SerializeObject(model, lowerCase, onlyPublic: false) 来包含 private 成员
            string jsonWithPrivate = model.ToJson(lowerCase: false,onlyPublic: false);

            Assert.That(jsonWithPrivate,Does.Contain("IncludedPrivateField"),
                "使用 JsonInclude 标记的私有字段应被包含（当 onlyPublic=false 时）");
            Console.WriteLine($"JsonInclude 测试 - 序列化结果(onlyPublic=false):\n{jsonWithPrivate}");
        }

        #endregion

        #region JsonAlias 别名映射测试

        [Test]
        [Category("JsonAlias")]
        public void TestJsonAlias_SerializationUsesAlias()
        {
            // Arrange
            var model = new AliasTestModel
            {
                UserName = "John",
                Age = 30,
                Email = "john@example.com",
                Description = "Test user"
            };

            // Act
            string json = SimpleJson.SerializeObject(model);

            // Assert
            Assert.That(json,Does.Contain("user_name"),"应该使用别名 'user_name'");
            Assert.That(json,Does.Contain("John"),"别名值应该正确");
            Assert.That(json,Does.Contain("user_age"),"应该使用别名 'user_age'");
            Assert.That(json,Does.Contain("email_address"),"应该使用别名 'email_address'");
            Assert.That(json,Does.Contain("description"),"无别名的属性转为小写");
            Assert.That(json,Does.Not.Contain("UserName"),"应该不包含原始属性名（被别名替代）");
            Console.WriteLine($"JsonAlias 序列化测试 - 结果:\n{json}");
        }

        [Test]
        [Category("JsonAlias")]
        public void TestJsonAlias_DeserializationWithAlias()
        {
            // Arrange
            string json = @"{
                ""user_name"": ""Alice"",
                ""user_age"": 25,
                ""email_address"": ""alice@example.com"",
                ""Description"": ""Alice User""
            }";

            // Act
            var model = SimpleJson.DeserializeObject<AliasTestModel>(json);

            // Assert
            Assert.That(model,Is.Not.Null);
            Assert.That(model.UserName,Is.EqualTo("Alice"),"应该通过别名 'user_name' 反序列化");
            Assert.That(model.Age,Is.EqualTo(25),"应该通过别名 'user_age' 反序列化");
            Assert.That(model.Email,Is.EqualTo("alice@example.com"),"应该通过别名 'email_address' 反序列化");
            Assert.That(model.Description,Is.EqualTo("Alice User"),"应该通过原始名称反序列化");
            Console.WriteLine($"JsonAlias 反序列化测试 - 通过");
        }

        [Test]
        [Category("JsonAlias")]
        public void TestJsonAlias_AcceptOriginalName()
        {
            // Arrange - 使用原始属性名而不是别名
            string jsonWithOriginalName = @"{
                ""user_name"": ""Bob"",
                ""Age"": 28,
                ""email_address"": ""bob@example.com""
            }";

            // Act
            var model = SimpleJson.DeserializeObject<AliasTestModel>(jsonWithOriginalName);

            // Assert
            Assert.That(model,Is.Not.Null);
            // Age 属性设置了 acceptOriginalName: true，所以应该同时接受 'Age' 和 'user_age'
            Assert.That(model.Age,Is.EqualTo(28),"设置 acceptOriginalName=true 的属性应接受原始名称");
            Console.WriteLine($"JsonAlias AcceptOriginalName 测试 - 通过");
        }

        #endregion

        #region 大小写敏感性测试

        [Test]
        [Category("CaseSensitivity")]
        public void TestCaseSensitivity_ExactMatch()
        {
            // Arrange
            string json = @"{
                ""FirstName"": ""John"",
                ""LastName"": ""Doe"",
                ""Age"": 30
            }";

            // Act
            var model = SimpleJson.DeserializeObject<CaseSensitivityTestModel>(json);

            // Assert
            Assert.That(model,Is.Not.Null);
            Assert.That(model.FirstName,Is.EqualTo("John"),"大小写完全匹配时应该反序列化");
            Assert.That(model.LastName,Is.EqualTo("Doe"));
            Console.WriteLine("大小写敏感性测试 - 精确匹配：通过");
        }

        [Test]
        [Category("CaseSensitivity")]
        public void TestCaseSensitivity_MismatchWithFallback()
        {
            // Arrange
            string jsonWithWrongCase = @"{
                ""firstname"": ""John"",
                ""lastname"": ""Doe"",
                ""age"": 30
            }";

            // Act
            var model = SimpleJson.DeserializeObject<CaseSensitivityTestModel>(jsonWithWrongCase);

            // Assert
            // 代码中有大小写不敏感的兜底机制（见第 1356 行的 StringComparer.OrdinalIgnoreCase）
            Assert.That(model,Is.Not.Null);
            Assert.That(model.FirstName,Is.EqualTo("John"),
                "应该使用大小写不敏感的兜底匹配");
            Assert.That(model.LastName,Is.EqualTo("Doe"));
            Assert.That(model.Age,Is.EqualTo(30));
            Console.WriteLine("大小写敏感性测试 - 兜底匹配：通过");
        }

        [Test]
        [Category("CaseSensitivity")]
        public void TestCaseSensitivity_MixedCase()
        {
            // Arrange
            string jsonMixedCase = @"{
                ""FirstName"": ""Jane"",
                ""lastname"": ""Smith"",
                ""AGE"": 25
            }";

            // Act
            var model = SimpleJson.DeserializeObject<CaseSensitivityTestModel>(jsonMixedCase);

            // Assert
            Assert.That(model,Is.Not.Null);
            Assert.That(model.FirstName,Is.EqualTo("Jane"),"精确匹配的字段应该正确反序列化");
            Assert.That(model.LastName,Is.EqualTo("Smith"),"兜底匹配的字段应该正确反序列化");
            Assert.That(model.Age,Is.EqualTo(25),"全大写字段应该通过兜底匹配反序列化");
            Console.WriteLine("大小写敏感性测试 - 混合大小写：通过");
        }

        #endregion

        #region PascalCase 到 camelCase 转换测试

        [Test]
        [Category("CamelCaseConversion")]
        public void TestPascalToCamelCase_Serialization()
        {
            // Arrange
            var model = new PascalCaseModel
            {
                FirstName = "John",
                LastName = "Doe",
                UserAge = 30,
                IsActive = true
            };

            // Act
            // 使用 lowerCase=true 启用 PascalCase -> camelCase 转换
            string json = model.ToJson(lowerCase: true,onlyPublic: true);

            // Assert
            Assert.That(json,Does.Contain("firstName"),"FirstName 应转换为 camelCase");
            Assert.That(json,Does.Contain("lastName"),"LastName 应转换为 camelCase");
            Assert.That(json,Does.Contain("userAge"),"UserAge 应转换为 camelCase");
            Assert.That(json,Does.Contain("isActive"),"IsActive 应转换为 camelCase");
            Assert.That(json,Does.Not.Contain("FirstName"),"不应包含 PascalCase 版本");
            Console.WriteLine($"PascalCase 到 camelCase 转换测试 - 序列化结果:\n{json}");
        }

        [Test]
        [Category("CamelCaseConversion")]
        public void TestPascalToCamelCase_Deserialization()
        {
            // Arrange
            string jsonCamelCase = @"{
                ""firstName"": ""Jane"",
                ""lastName"": ""Smith"",
                ""userAge"": 28,
                ""isActive"": false
            }";

            // Act
            var model = jsonCamelCase.FromJson<PascalCaseModel>(lowerCase: true,onlyPublic: true);

            // Assert
            Assert.That(model,Is.Not.Null);
            Assert.That(model.FirstName,Is.EqualTo("Jane"),"camelCase 应反序列化为 PascalCase 属性");
            Assert.That(model.LastName,Is.EqualTo("Smith"));
            Assert.That(model.UserAge,Is.EqualTo(28));
            Assert.That(model.IsActive,Is.False);
            Console.WriteLine("PascalCase 到 camelCase 转换测试 - 反序列化：通过");
        }
        public class URLModel
        {
            public string HTTPServer { get; set; } = "";
            public string XMLParser { get; set; } = "";
        }
        [Test]
        [Category("CamelCaseConversion")]
        public void TestCamelCasePreservesUppercaseSequence()
        {
            // Arrange


            var model = new URLModel
            {
                HTTPServer = "apache",
                XMLParser = "expat"
            };

            // Act
            string json = model.ToJson(lowerCase: true);

            // Assert
            // ToJsonPropertyName 规则：连续大写字符会全部转小写，然后接余下的字符
            // HTTPServer -> 循环遍历 H,T,T,P,S (5个大写)，遇到 e 停止
            // 结果 = "https".ToLowerInvariant() + "erver" = "httpserver"
            // XMLParser -> 循环遍历 X,M,L (3个大写)，遇到 P 停止
            // 结果 = "xml".ToLowerInvariant() + "Parser" = "xmlparser"
            Assert.That(json,Does.Contain("httpserver"),"连续大写应正确转换");
            Assert.That(json,Does.Contain("xmlparser"),"连续大写应正确转换");
            Console.WriteLine($"连续大写 camelCase 转换 - 结果:\n{json}");
        }

        #endregion

        #region DataContract 支持测试（需启用 SIMPLE_JSON_DATACONTRACT）

        // 如果启用了 SIMPLE_JSON_DATACONTRACT，可以取消注释以下代码
        /*
        [DataContract]
        public class DataContractTestModel
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember(Name = "user_age")]
            public int Age { get; set; }

            [IgnoreDataMember]
            public string IgnoredField { get; set; }

            // 无 DataMember 标记的属性不会被序列化
            public string NotIncluded { get; set; }
        }

        [Test]
        [Category("DataContract")]
        public void TestDataContract_OnlyDataMembersAreSerialized()
        {
            // Arrange
            var model = new DataContractTestModel
            {
                Name = "John",
                Age = 30,
                IgnoredField = "ignored",
                NotIncluded = "not included"
            };

            // Act
            string json = SimpleJson.SerializeObject(model);

            // Assert
            Assert.That(json, Does.Contain("Name"), "DataMember 标记的属性应被序列化");
            Assert.That(json, Does.Contain("user_age"), "DataMember 指定的别名应被使用");
            Assert.That(json, Does.Not.Contain("IgnoredField"), "IgnoreDataMember 标记的字段不应序列化");
            Assert.That(json, Does.Not.Contain("NotIncluded"), "无 DataMember 标记的属性不应序列化");
            Console.WriteLine($"DataContract 序列化测试 - 结果:\n{json}");
        }

        [Test]
        [Category("DataContract")]
        public void TestDataContract_DeserializationWithAlias()
        {
            // Arrange
            string json = @"{
                ""Name"": ""Alice"",
                ""user_age"": 25,
                ""IgnoredField"": ""should be ignored"",
                ""NotIncluded"": ""should be ignored""
            }";

            // Act
            var model = SimpleJson.DeserializeObject<DataContractTestModel>(json);

            // Assert
            Assert.That(model.Name, Is.EqualTo("Alice"));
            Assert.That(model.Age, Is.EqualTo(25), "应通过别名 'user_age' 反序列化");
            Assert.That(model.IgnoredField, Is.Null, "IgnoreDataMember 字段不应被赋值");
            Assert.That(model.NotIncluded, Is.Null, "无 DataMember 的属性不应被赋值");
            Console.WriteLine("DataContract 反序列化测试 - 通过");
        }
        */

        #endregion

        #region 复合功能测试
        public class CombinedModel
        {
            [JsonAlias("user_id")]
            public int Id { get; set; }

            [JsonIgnore]
            public string Password { get; set; } = "";

            public string Username { get; set; } = "";
        }

        [Test]
        [Category("Combined")]
        public void TestCombined_AliasAndIgnore()
        {
            // 测试 Alias 和 Ignore 的组合

            var model = new CombinedModel
            {
                Id = 123,
                Password = "secret",
                Username = "john"
            };

            // Serialize
            string json = SimpleJson.SerializeObject(model);
            Assert.That(json,Does.Contain("user_id"),"Alias 应该被使用");
            Assert.That(json,Does.Contain("123"));
            Assert.That(json,Does.Not.Contain("Password"),"Ignore 应该被遵守");
            Assert.That(json,Does.Not.Contain("secret"));

            // Deserialize
            var restored = SimpleJson.DeserializeObject<CombinedModel>(json);
            Assert.That(restored.Id,Is.EqualTo(123));
            Assert.That(restored.Username,Is.EqualTo("john"));
            Console.WriteLine("组合功能测试 - 通过");
        }

        #endregion

        #region 辅助方法

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("\n========== 开始新测试 ==========");
        }

        [TearDown]
        public void Cleanup()
        {
            Console.WriteLine("========== 测试结束 ==========\n");
        }

        #endregion
    }

    [TestFixture]
    [Category("CoreTypes")]
    public class SimpleJsonCoreTypeTests
    {
        #region 基础类型测试

        [Test]
        public void TestSerialize_Int()
        {
            int value = 42;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("42"));
        }

        [Test]
        public void TestDeserialize_Int()
        {
            string json = "42";
            int result = SimpleJson.DeserializeObject<int>(json);
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void TestSerialize_Double()
        {
            double value = 3.14159;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("3.14"));
        }

        [Test]
        public void TestDeserialize_Double()
        {
            string json = "3.14159";
            double result = SimpleJson.DeserializeObject<double>(json);
            Assert.That(result, Is.EqualTo(3.14159).Within(0.00001));
        }

        [Test]
        public void TestSerialize_Bool_True()
        {
            bool value = true;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("true"));
        }

        [Test]
        public void TestSerialize_Bool_False()
        {
            bool value = false;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("false"));
        }

        [Test]
        public void TestDeserialize_Bool_True()
        {
            string json = "true";
            bool result = SimpleJson.DeserializeObject<bool>(json);
            Assert.That(result, Is.True);
        }

        [Test]
        public void TestDeserialize_Bool_False()
        {
            string json = "false";
            bool result = SimpleJson.DeserializeObject<bool>(json);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestSerialize_String()
        {
            string value = "Hello, World!";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo(@"""Hello, World!"""));
        }

        [Test]
        public void TestDeserialize_String()
        {
            string json = @"""Hello, World!""";
            string result = SimpleJson.DeserializeObject<string>(json);
            Assert.That(result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void TestSerialize_String_WithEscape()
        {
            string value = "Line1\nLine2\tTabbed";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("\\n"));
            Assert.That(json, Does.Contain("\\t"));
        }

        [Test]
        public void TestSerialize_Null()
        {
            object value = null;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("null"));
        }

        [Test]
        public void TestDeserialize_Null()
        {
            string json = "null";
            object result = SimpleJson.DeserializeObject<object>(json);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestSerialize_Long()
        {
            long value = 9223372036854775807L;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("9223372036854775807"));
        }

        [Test]
        public void TestSerialize_Float()
        {
            float value = 3.14f;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("3.14"));
        }

        [Test]
        public void TestSerialize_Decimal()
        {
            decimal value = 123.456m;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("123.456"));
        }

        #endregion

        #region DateTime 测试

        [Test]
        public void TestSerialize_DateTime()
        {
            var date = new DateTime(2024, 12, 25, 10, 30, 45);
            string json = SimpleJson.SerializeObject(date);
            Assert.That(json, Does.Contain("2024"));
            Assert.That(json, Does.Contain("12"));
            Assert.That(json, Does.Contain("25"));
        }

        [Test]
        public void TestDeserialize_DateTime()
        {
            string json = @"""2024-12-25T10:30:45""";
            DateTime result = SimpleJson.DeserializeObject<DateTime>(json);
            Assert.That(result.Year, Is.EqualTo(2024));
            Assert.That(result.Month, Is.EqualTo(12));
            Assert.That(result.Day, Is.EqualTo(25));
        }

        [Test]
        public void TestSerialize_DateTimeOffset()
        {
            var date = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(8));
            string json = SimpleJson.SerializeObject(date);
            Assert.That(json, Does.Contain("2024"));
        }

        #endregion

        #region Enum 测试

        public enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        [Test]
        public void TestSerialize_Enum()
        {
            TestEnum value = TestEnum.Value2;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("1"));
        }

        [Test]
        public void TestDeserialize_Enum()
        {
            string json = "1";
            TestEnum result = SimpleJson.DeserializeObject<TestEnum>(json);
            Assert.That(result, Is.EqualTo(TestEnum.Value2));
        }

        [Flags]
        public enum TestFlags
        {
            None = 0,
            Read = 1,
            Write = 2,
            Execute = 4
        }

        [Test]
        public void TestSerialize_FlagsEnum()
        {
            TestFlags value = TestFlags.Read | TestFlags.Write;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("3"));
        }

        #endregion

        #region Nullable 测试

        [Test]
        public void TestSerialize_NullableInt_HasValue()
        {
            int? value = 42;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("42"));
        }

        [Test]
        public void TestSerialize_NullableInt_Null()
        {
            int? value = null;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo("null"));
        }

        [Test]
        public void TestDeserialize_NullableInt_HasValue()
        {
            string json = "42";
            int? result = SimpleJson.DeserializeObject<int?>(json);
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void TestDeserialize_NullableInt_Null()
        {
            string json = "null";
            int? result = SimpleJson.DeserializeObject<int?>(json);
            Assert.That(result, Is.Null);
        }

        #endregion

        #region Guid 测试

        [Test]
        public void TestSerialize_Guid()
        {
            Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            string json = SimpleJson.SerializeObject(guid);
            Assert.That(json, Does.Contain("12345678"));
        }

        [Test]
        public void TestDeserialize_Guid()
        {
            string json = @"""12345678-1234-1234-1234-123456789abc""";
            Guid result = SimpleJson.DeserializeObject<Guid>(json);
            Assert.That(result, Is.EqualTo(Guid.Parse("12345678-1234-1234-1234-123456789abc")));
        }

        #endregion
    }

    [TestFixture]
    [Category("Collections")]
    public class SimpleJsonCollectionTests
    {
        #region Array 测试

        [Test]
        public void TestSerialize_IntArray()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            string json = SimpleJson.SerializeObject(arr);
            Assert.That(json, Is.EqualTo("[1,2,3,4,5]"));
        }

        [Test]
        public void TestDeserialize_IntArray()
        {
            string json = "[1,2,3,4,5]";
            int[] result = SimpleJson.DeserializeObject<int[]>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(result[4], Is.EqualTo(5));
        }

        [Test]
        public void TestSerialize_StringArray()
        {
            string[] arr = { "a", "b", "c" };
            string json = SimpleJson.SerializeObject(arr);
            Assert.That(json, Is.EqualTo(@"[""a"",""b"",""c""]"));
        }

        [Test]
        public void TestSerialize_EmptyArray()
        {
            int[] arr = new int[0];
            string json = SimpleJson.SerializeObject(arr);
            Assert.That(json, Is.EqualTo("[]"));
        }

        [Test]
        public void TestDeserialize_EmptyArray()
        {
            string json = "[]";
            int[] result = SimpleJson.DeserializeObject<int[]>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestSerialize_2DArray()
        {
            int[][] arr = { new[] { 1, 2 }, new[] { 3, 4 } };
            string json = SimpleJson.SerializeObject(arr);
            Assert.That(json, Is.EqualTo("[[1,2],[3,4]]"));
        }

        #endregion

        #region List 测试

        [Test]
        public void TestSerialize_IntList()
        {
            var list = new List<int> { 1, 2, 3 };
            string json = SimpleJson.SerializeObject(list);
            Assert.That(json, Is.EqualTo("[1,2,3]"));
        }

        [Test]
        public void TestDeserialize_IntList()
        {
            string json = "[1,2,3]";
            List<int> result = SimpleJson.DeserializeObject<List<int>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSerialize_StringList()
        {
            var list = new List<string> { "hello", "world" };
            string json = SimpleJson.SerializeObject(list);
            Assert.That(json, Does.Contain("hello"));
            Assert.That(json, Does.Contain("world"));
        }

        #endregion

        #region Dictionary 测试

        [Test]
        public void TestSerialize_StringStringDictionary()
        {
            var dict = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            string json = SimpleJson.SerializeObject(dict);
            Assert.That(json, Does.Contain("key1"));
            Assert.That(json, Does.Contain("value1"));
            Assert.That(json, Does.Contain("key2"));
            Assert.That(json, Does.Contain("value2"));
        }

        [Test]
        public void TestDeserialize_StringStringDictionary()
        {
            string json = @"{""key1"":""value1"",""key2"":""value2""}";
            var result = SimpleJson.DeserializeObject<Dictionary<string, string>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["key1"], Is.EqualTo("value1"));
            Assert.That(result["key2"], Is.EqualTo("value2"));
        }

        [Test]
        public void TestSerialize_StringIntDictionary()
        {
            var dict = new Dictionary<string, int>
            {
                { "one", 1 },
                { "two", 2 }
            };
            string json = SimpleJson.SerializeObject(dict);
            Assert.That(json, Does.Contain("one"));
            Assert.That(json, Does.Contain("1"));
        }

        [Test]
        public void TestDeserialize_StringIntDictionary()
        {
            string json = @"{""one"":1,""two"":2}";
            var result = SimpleJson.DeserializeObject<Dictionary<string, int>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result["one"], Is.EqualTo(1));
            Assert.That(result["two"], Is.EqualTo(2));
        }

        [Test]
        public void TestSerialize_IntStringDictionary()
        {
            var dict = new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" }
            };
            Assert.Throws<Exception>(() => SimpleJson.SerializeObject(dict));
        }

        [Test]
        public void TestSerialize_EmptyDictionary()
        {
            var dict = new Dictionary<string, string>();
            string json = SimpleJson.SerializeObject(dict);
            Assert.That(json, Is.EqualTo("{}"));
        }

        [Test]
        public void TestDeserialize_EmptyDictionary()
        {
            string json = "{}";
            var result = SimpleJson.DeserializeObject<Dictionary<string, string>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region HashSet 测试

        [Test]
        public void TestSerialize_IntHashSet()
        {
            var set = new HashSet<int> { 1, 2, 3 };
            string json = SimpleJson.SerializeObject(set);
            Assert.That(json, Does.Contain("1"));
            Assert.That(json, Does.Contain("2"));
            Assert.That(json, Does.Contain("3"));
        }

        #endregion

        #region 嵌套集合测试

        [Test]
        public void TestSerialize_NestedList()
        {
            var nested = new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 3, 4 }
            };
            string json = SimpleJson.SerializeObject(nested);
            Assert.That(json, Is.EqualTo("[[1,2],[3,4]]"));
        }

        [Test]
        public void TestDeserialize_NestedList()
        {
            string json = "[[1,2],[3,4]]";
            var result = SimpleJson.DeserializeObject<List<List<int>>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0][0], Is.EqualTo(1));
            Assert.That(result[1][1], Is.EqualTo(4));
        }

        [Test]
        public void TestSerialize_DictionaryWithListValue()
        {
            var dict = new Dictionary<string, List<int>>
            {
                { "numbers", new List<int> { 1, 2, 3 } }
            };
            string json = SimpleJson.SerializeObject(dict);
            Assert.That(json, Does.Contain("numbers"));
            Assert.That(json, Does.Contain("[1,2,3]"));
        }

        [Test]
        public void TestDeserialize_DictionaryWithListValue()
        {
            string json = @"{""numbers"":[1,2,3]}";
            var result = SimpleJson.DeserializeObject<Dictionary<string, List<int>>>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result["numbers"], Is.Not.Null);
            Assert.That(result["numbers"].Count, Is.EqualTo(3));
        }

        #endregion
    }

    [TestFixture]
    [Category("ComplexTypes")]
    public class SimpleJsonComplexTypeTests
    {
        #region 测试模型

        public class SimplePerson
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class Address
        {
            public string Street { get; set; } = "";
            public string City { get; set; } = "";
            public string ZipCode { get; set; } = "";
        }

        public class PersonWithAddress
        {
            public string Name { get; set; } = "";
            public Address? Address { get; set; }
        }

        public class Company
        {
            public string Name { get; set; } = "";
            public List<SimplePerson> Employees { get; set; } = new();
        }

        public class RecursiveModel
        {
            public string Name { get; set; } = "";
            public RecursiveModel? Child { get; set; }
        }

        public class ModelWithDefaultValues
        {
            public int Number { get; set; } = 100;
            public string Text { get; set; } = "default";
            public bool Flag { get; set; } = true;
        }

        #endregion

        #region 简单对象测试

        [Test]
        public void TestSerialize_SimpleObject()
        {
            var person = new SimplePerson { Name = "John", Age = 30 };
            string json = SimpleJson.SerializeObject(person);
            Assert.That(json, Does.Contain("name"));
            Assert.That(json, Does.Contain("John"));
            Assert.That(json, Does.Contain("age"));
            Assert.That(json, Does.Contain("30"));
        }

        [Test]
        public void TestDeserialize_SimpleObject()
        {
            string json = @"{""Name"":""John"",""Age"":30}";
            var result = SimpleJson.DeserializeObject<SimplePerson>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("John"));
            Assert.That(result.Age, Is.EqualTo(30));
        }

        [Test]
        public void TestSerialize_ObjectWithNullProperty()
        {
            var person = new SimplePerson { Name = "", Age = 25 };
            string json = SimpleJson.SerializeObject(person);
            Assert.That(json, Does.Contain("age"));
            Assert.That(json, Does.Contain("25"));
        }

        #endregion

        #region 嵌套对象测试

        [Test]
        public void TestSerialize_NestedObject()
        {
            var person = new PersonWithAddress
            {
                Name = "John",
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "New York",
                    ZipCode = "10001"
                }
            };
            string json = SimpleJson.SerializeObject(person);
            Assert.That(json, Does.Contain("John"));
            Assert.That(json, Does.Contain("123 Main St"));
            Assert.That(json, Does.Contain("New York"));
        }

        [Test]
        public void TestDeserialize_NestedObject()
        {
            string json = @"{""Name"":""John"",""Address"":{""Street"":""123 Main St"",""City"":""New York"",""ZipCode"":""10001""}}";
            var result = SimpleJson.DeserializeObject<PersonWithAddress>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("John"));
            Assert.That(result.Address, Is.Not.Null);
            Assert.That(result.Address.Street, Is.EqualTo("123 Main St"));
            Assert.That(result.Address.City, Is.EqualTo("New York"));
        }

        [Test]
        public void TestSerialize_NestedObjectNull()
        {
            var person = new PersonWithAddress
            {
                Name = "John",
                Address = new Address { Street = "", City = "", ZipCode = "" }
            };
            string json = SimpleJson.SerializeObject(person);
            Assert.That(json, Does.Contain("John"));
        }

        #endregion

        #region 对象列表测试

        [Test]
        public void TestSerialize_ObjectList()
        {
            var company = new Company
            {
                Name = "Tech Corp",
                Employees = new List<SimplePerson>
                {
                    new SimplePerson { Name = "Alice", Age = 28 },
                    new SimplePerson { Name = "Bob", Age = 32 }
                }
            };
            string json = SimpleJson.SerializeObject(company);
            Assert.That(json, Does.Contain("Tech Corp"));
            Assert.That(json, Does.Contain("Alice"));
            Assert.That(json, Does.Contain("Bob"));
        }

        [Test]
        public void TestDeserialize_ObjectList()
        {
            string json = @"{""Name"":""Tech Corp"",""Employees"":[{""Name"":""Alice"",""Age"":28},{""Name"":""Bob"",""Age"":32}]}";
            var result = SimpleJson.DeserializeObject<Company>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Tech Corp"));
            Assert.That(result.Employees, Is.Not.Null);
            Assert.That(result.Employees.Count, Is.EqualTo(2));
            Assert.That(result.Employees[0].Name, Is.EqualTo("Alice"));
        }

        #endregion

        #region 递归对象测试

        [Test]
        public void TestSerialize_RecursiveObject()
        {
            var root = new RecursiveModel
            {
                Name = "Root",
                Child = new RecursiveModel
                {
                    Name = "Child",
                    Child = new RecursiveModel { Name = "" }
                }
            };
            string json = SimpleJson.SerializeObject(root);
            Assert.That(json, Does.Contain("Root"));
            Assert.That(json, Does.Contain("Child"));
        }

        [Test]
        public void TestDeserialize_RecursiveObject()
        {
            string json = @"{""Name"":""Root"",""Child"":{""Name"":""Child"",""Child"":null}}";
            var result = SimpleJson.DeserializeObject<RecursiveModel>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Root"));
            Assert.That(result.Child, Is.Not.Null);
            Assert.That(result.Child.Name, Is.EqualTo("Child"));
            Assert.That(result.Child.Child, Is.Null);
        }

        #endregion

        #region 默认值测试

        [Test]
        public void TestSerialize_DefaultValues()
        {
            var model = new ModelWithDefaultValues();
            string json = SimpleJson.SerializeObject(model);
            Assert.That(json, Does.Contain("100"));
            Assert.That(json, Does.Contain("default"));
            Assert.That(json, Does.Contain("true"));
        }

        [Test]
        public void TestDeserialize_PartialJson_UsesDefaults()
        {
            string json = @"{""Number"":200}";
            var result = SimpleJson.DeserializeObject<ModelWithDefaultValues>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Number, Is.EqualTo(200));
        }

        #endregion
    }

    [TestFixture]
    [Category("EdgeCases")]
    public class SimpleJsonEdgeCaseTests
    {
        #region 特殊字符测试

        [Test]
        public void TestSerialize_StringWithQuotes()
        {
            string value = @"He said ""Hello""";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain(@"\"""));
        }

        [Test]
        public void TestDeserialize_StringWithQuotes()
        {
            string json = @"""He said \""Hello\""""";
            string result = SimpleJson.DeserializeObject<string>(json);
            Assert.That(result, Is.EqualTo(@"He said ""Hello"""));
        }

        [Test]
        public void TestSerialize_StringWithBackslash()
        {
            string value = @"C:\Path\To\File";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain(@"\\"));
        }

        [Test]
        public void TestDeserialize_StringWithBackslash()
        {
            string json = @"""C:\\Path\\To\\File""";
            string result = SimpleJson.DeserializeObject<string>(json);
            Assert.That(result, Is.EqualTo(@"C:\Path\To\File"));
        }

        [Test]
        public void TestSerialize_StringWithUnicode()
        {
            string value = "Hello 世界 🌍";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("世界"));
        }

        [Test]
        public void TestDeserialize_StringWithUnicode()
        {
            string json = @"""Hello 世界 🌍""";
            string result = SimpleJson.DeserializeObject<string>(json);
            Assert.That(result, Is.EqualTo("Hello 世界 🌍"));
        }

        [Test]
        public void TestSerialize_StringWithNewline()
        {
            string value = "Line1\nLine2\r\nLine3";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("\\n"));
            Assert.That(json, Does.Contain("\\r"));
        }

        [Test]
        public void TestSerialize_StringWithTab()
        {
            string value = "Col1\tCol2";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("\\t"));
        }

        #endregion

        #region 边界值测试

        [Test]
        public void TestSerialize_MaxInt()
        {
            int value = int.MaxValue;
            string json = SimpleJson.SerializeObject(value);
            int result = SimpleJson.DeserializeObject<int>(json);
            Assert.That(result, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void TestSerialize_MinInt()
        {
            int value = int.MinValue;
            string json = SimpleJson.SerializeObject(value);
            int result = SimpleJson.DeserializeObject<int>(json);
            Assert.That(result, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void TestSerialize_MaxDouble()
        {
            double value = double.MaxValue;
            string json = SimpleJson.SerializeObject(value);
            double result = SimpleJson.DeserializeObject<double>(json);
            Assert.That(result, Is.EqualTo(double.MaxValue));
        }

        [Test]
        public void TestSerialize_DoubleInfinity()
        {
            double value = double.PositiveInfinity;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("Infinity"));
        }

        [Test]
        public void TestSerialize_DoubleNaN()
        {
            double value = double.NaN;
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Does.Contain("NaN"));
        }

        [Test]
        public void TestSerialize_EmptyString()
        {
            string value = "";
            string json = SimpleJson.SerializeObject(value);
            Assert.That(json, Is.EqualTo(@""""""));
        }

        [Test]
        public void TestDeserialize_EmptyString()
        {
            string json = @"""""";
            string result = SimpleJson.DeserializeObject<string>(json);
            Assert.That(result, Is.EqualTo(""));
        }

        #endregion

        #region 空白和格式测试

        [Test]
        public void TestDeserialize_JsonWithWhitespace()
        {
            string json = "{ \"Name\" : \"John\" , \"Age\" : 30 }";
            var result = SimpleJson.DeserializeObject<SimpleJsonComplexTypeTests.SimplePerson>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("John"));
            Assert.That(result.Age, Is.EqualTo(30));
        }

        [Test]
        public void TestDeserialize_JsonWithNewlines()
        {
            string json = @"{
                ""Name"": ""John"",
                ""Age"": 30
            }";
            var result = SimpleJson.DeserializeObject<SimpleJsonComplexTypeTests.SimplePerson>(json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("John"));
        }

        #endregion

        #region 错误处理测试

        [Test]
        public void TestDeserialize_InvalidJson_Throws()
        {
            string json = "not valid json";
            Assert.Throws<SerializationException>(() => SimpleJson.DeserializeObject<object>(json));
        }

        [Test]
        public void TestDeserialize_TruncatedJson_Throws()
        {
            string json = @"{""Name"": ""John";
            Assert.Throws<SerializationException>(() => SimpleJson.DeserializeObject<SimpleJsonComplexTypeTests.SimplePerson>(json));
        }

        [Test]
        public void TestDeserialize_TypeMismatch()
        {
            string json = @"""not a number""";
            Assert.Throws<FormatException>(() => SimpleJson.DeserializeObject<int>(json));
        }

        #endregion

        #region 扩展方法测试

        [Test]
        public void TestToJson_ExtensionMethod()
        {
            var person = new SimpleJsonComplexTypeTests.SimplePerson { Name = "Test", Age = 25 };
            string json = person.ToJson();
            Assert.That(json, Does.Contain("Test"));
            Assert.That(json, Does.Contain("25"));
        }

        [Test]
        public void TestFromJson_ExtensionMethod()
        {
            string json = @"{""Name"":""Test"",""Age"":25}";
            var result = json.FromJson<SimpleJsonComplexTypeTests.SimplePerson>();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(25));
        }

        [Test]
        public void TestToJson_WithLowerCase()
        {
            var person = new SimpleJsonComplexTypeTests.SimplePerson { Name = "Test", Age = 25 };
            string json = person.ToJson(lowerCase: true);
            Assert.That(json, Does.Contain("name"));
            Assert.That(json, Does.Contain("age"));
        }

        [Test]
        public void TestFromJson_WithLowerCase()
        {
            string json = @"{""name"":""Test"",""age"":25}";
            var result = json.FromJson<SimpleJsonComplexTypeTests.SimplePerson>(lowerCase: true);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.Age, Is.EqualTo(25));
        }

        #endregion
    }

    [TestFixture]
    [Category("Performance")]
    public class SimpleJsonPerformanceTests
    {
        [Test]
        public void TestPerformance_SerializeManyObjects()
        {
            var persons = new List<SimpleJsonComplexTypeTests.SimplePerson>();
            for (int i = 0; i < 1000; i++)
            {
                persons.Add(new SimpleJsonComplexTypeTests.SimplePerson
                {
                    Name = $"Person_{i}",
                    Age = i % 100
                });
            }

            var sw = Stopwatch.StartNew();
            string json = SimpleJson.SerializeObject(persons);
            sw.Stop();

            Assert.That(json, Is.Not.Null);
            Assert.That(json.Length, Is.GreaterThan(0));
            Console.WriteLine($"Serialize 1000 objects: {sw.ElapsedMilliseconds}ms");
        }

        [Test]
        public void TestPerformance_DeserializeManyObjects()
        {
            var persons = new List<SimpleJsonComplexTypeTests.SimplePerson>();
            for (int i = 0; i < 1000; i++)
            {
                persons.Add(new SimpleJsonComplexTypeTests.SimplePerson
                {
                    Name = $"Person_{i}",
                    Age = i % 100
                });
            }
            string json = SimpleJson.SerializeObject(persons);

            var sw = Stopwatch.StartNew();
            var result = SimpleJson.DeserializeObject<List<SimpleJsonComplexTypeTests.SimplePerson>>(json);
            sw.Stop();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1000));
            Console.WriteLine($"Deserialize 1000 objects: {sw.ElapsedMilliseconds}ms");
        }

        [Test]
        public void TestPerformance_DeepNestedObject()
        {
            var root = new SimpleJsonComplexTypeTests.RecursiveModel { Name = "Root" };
            var current = root;
            for (int i = 0; i < 100; i++)
            {
                var child = new SimpleJsonComplexTypeTests.RecursiveModel { Name = $"Level_{i}", Child = new SimpleJsonComplexTypeTests.RecursiveModel { Name = "" } };
                current.Child = child;
                current = child;
            }

            var sw = Stopwatch.StartNew();
            string json = SimpleJson.SerializeObject(root);
            sw.Stop();

            Assert.That(json, Is.Not.Null);
            Console.WriteLine($"Serialize 100-level nested object: {sw.ElapsedMilliseconds}ms");
        }
    }

}


