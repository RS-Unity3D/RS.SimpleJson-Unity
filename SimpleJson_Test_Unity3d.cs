using RS.SimpleJsonAOT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using UnityEngine;


namespace RS.SimpleJsonAOT.Test
{
    public class UnitySerializationTests : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("========== Unity Serialization Tests ==========");

            TestVector3Serialization();
            TestVector3Deserialization();
            TestQuaternionSerialization();
            TestCustomClassWithUnityObjects();
            TestTransformData();
            TestNestedUnityObjects();
            TestUnityObjectArray();

            // P0 BUG 修复测试
            TestP0BugFix_NonStringKeyDictionary();

            Debug.Log("========== All Tests Completed ==========");
        }

        // ✅ 测试 1：Vector3 序列化
        void TestVector3Serialization()
        {
            Debug.Log("\n--- Test 1: Vector3 Serialization ---");

            Vector3 vec = new Vector3(1.5f,2.5f,3.5f);
            string json = SimpleJson.SerializeObject(vec);

            Debug.Log($"Original: {vec}");
            Debug.Log($"JSON: {json}");

            bool isCorrect = json.Contains("1.5") && json.Contains("2.5") && json.Contains("3.5");
            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 2：Vector3 反序列化
        void TestVector3Deserialization()
        {
            Debug.Log("\n--- Test 2: Vector3 Deserialization ---");

            string json = "{\"x\":1.5,\"y\":2.5,\"z\":3.5}";
            Vector3 vec = SimpleJson.DeserializeObject<Vector3>(json);

            Debug.Log($"JSON: {json}");
            Debug.Log($"Restored: {vec}");

            bool isCorrect = Mathf.Abs(vec.x - 1.5f) < 0.001f &&
                            Mathf.Abs(vec.y - 2.5f) < 0.001f &&
                            Mathf.Abs(vec.z - 3.5f) < 0.001f;
            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 3：Quaternion 序列化
        void TestQuaternionSerialization()
        {
            Debug.Log("\n--- Test 3: Quaternion Serialization ---");

            Quaternion quat = Quaternion.Euler(45,90,0);
            string json = SimpleJson.SerializeObject(quat);

            Debug.Log($"Original: {quat}");
            Debug.Log($"JSON: {json}");

            bool hasX = json.Contains("\"x\"");
            bool hasY = json.Contains("\"y\"");
            bool hasZ = json.Contains("\"z\"");
            bool hasW = json.Contains("\"w\"");

            Debug.Log($"Result: {(hasX && hasY && hasZ && hasW ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 4：自定义类包含 Unity 对象
        void TestCustomClassWithUnityObjects()
        {
            Debug.Log("\n--- Test 4: Custom Class with Unity Objects ---");

            var data = new GameData
            {
                name = "TestObject",
                position = new Vector3(10,20,30),
                rotation = Quaternion.identity,
                color = Color.red
            };

            string json = SimpleJson.SerializeObject(data);
            Debug.Log($"Serialized: {json}");

            var restored = SimpleJson.DeserializeObject<GameData>(json);

            bool isCorrect = restored.name == "TestObject" &&
                            Vector3.Distance(restored.position,data.position) < 0.001f &&
                            Quaternion.Dot(restored.rotation,data.rotation) > 0.999f;

            Debug.Log($"Name: {restored.name}");
            Debug.Log($"Position: {restored.position}");
            Debug.Log($"Rotation: {restored.rotation}");
            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 5：Transform 数据
        void TestTransformData()
        {
            Debug.Log("\n--- Test 5: Transform Data ---");

            var transformData = new TransformData
            {
                position = new Vector3(5,10,15),
                rotation = Quaternion.Euler(30,60,90),
                scale = Vector3.one * 2
            };

            string json = SimpleJson.SerializeObject(transformData);
            Debug.Log($"JSON: {json}");

            var restored = SimpleJson.DeserializeObject<TransformData>(json);

            bool isCorrect = Vector3.Distance(restored.position,transformData.position) < 0.001f &&
                            Vector3.Distance(restored.scale,transformData.scale) < 0.001f;

            Debug.Log($"Position: {restored.position}");
            Debug.Log($"Scale: {restored.scale}");
            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 6：嵌套 Unity 对象
        void TestNestedUnityObjects()
        {
            Debug.Log("\n--- Test 6: Nested Unity Objects ---");

            var containerData = new ContainerData
            {
                name = "Container",
                items = new List<Vector3>
            {
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6),
                new Vector3(7, 8, 9)
            }
            };

            // 注意：List 可能不被 JsonUtility 支持，但数组应该支持
            var containerWithArray = new ContainerDataWithArray
            {
                name = "Container",
                positions = new Vector3[]
                {
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6)
                }
            };

            string json = SimpleJson.SerializeObject(containerWithArray);
            Debug.Log($"JSON: {json}");

            var restored = SimpleJson.DeserializeObject<ContainerDataWithArray>(json);

            bool isCorrect = restored.name == "Container" &&
                            restored.positions != null &&
                            restored.positions.Length == 2;

            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 7：Unity 对象数组
        void TestUnityObjectArray()
        {
            Debug.Log("\n--- Test 7: Unity Object Array ---");

            var data = new VectorArrayData
            {
                vectors = new Vector3[]
                {
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1)
                }
            };

            string json = SimpleJson.SerializeObject(data);
            Debug.Log($"JSON: {json}");

            var restored = SimpleJson.DeserializeObject<VectorArrayData>(json);

            bool isCorrect = restored.vectors != null && restored.vectors.Length == 3;

            Debug.Log($"Array Length: {(restored.vectors != null ? restored.vectors.Length : 0)}");
            Debug.Log($"Result: {(isCorrect ? "✓ PASS" : "✗ FAIL")}");
        }

        // ✅ 测试 8：P0 BUG 修复验证 - 非字符串键字典
        void TestP0BugFix_NonStringKeyDictionary()
        {
            Debug.Log("\n--- Test 8: P0 BUG FIX - Non-String Key Dictionary ---");

            // 修复前：key 会被丢弃
            // 修复后：key 会被正确序列化

            var dict = new Dictionary<object,string>
        {
            { 1, "One" },
            { 2, "Two" },
            { 3, "Three" }
        };

            string json = SimpleJson.SerializeObject(dict);
            Debug.Log($"JSON: {json}");

            // 检查 key 是否被保留
            bool keysPreserved = json.Contains("1") && json.Contains("2") && json.Contains("3");
            bool valuesPreserved = json.Contains("One") && json.Contains("Two") && json.Contains("Three");

            Debug.Log($"Keys preserved: {keysPreserved}");
            Debug.Log($"Values preserved: {valuesPreserved}");
            Debug.Log($"Result: {(keysPreserved && valuesPreserved ? "✓ PASS (BUG FIXED)" : "✗ FAIL (BUG STILL EXISTS)")}");
        }

        // ============================================================================
        // 测试数据模型
        // ============================================================================

        [System.Serializable]
        public class GameData
        {
            public string name;
            public Vector3 position;
            public Quaternion rotation;
            public Color color;
        }

        [System.Serializable]
        public class TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        [System.Serializable]
        public class ContainerData
        {
            public string name;
            public List<Vector3> items;
        }

        [System.Serializable]
        public class ContainerDataWithArray
        {
            public string name;
            public Vector3[] positions;
        }

        [System.Serializable]
        public class VectorArrayData
        {
            public Vector3[] vectors;
        }
    }

    // ============================================================================
    // 性能测试 - Unity Serialization vs SimpleJson
    // ============================================================================



    public class UnitySerializationPerformanceTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("========== Performance Tests ==========");

            BenchmarkVector3Serialization();
            BenchmarkComplexObjectSerialization();

            Debug.Log("========== Performance Tests Completed ==========");
        }

        void BenchmarkVector3Serialization()
        {
            Debug.Log("\n--- Benchmark: Vector3 Serialization ---");

            int iterations = 10000;
            Vector3 vec = new Vector3(1.5f,2.5f,3.5f);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string json = SimpleJson.SerializeObject(vec);
            }
            sw.Stop();

            Debug.Log($"Iterations: {iterations}");
            Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");
            Debug.Log($"Per operation: {(double)sw.ElapsedMilliseconds / iterations:F4}ms");
        }

        void BenchmarkComplexObjectSerialization()
        {
            Debug.Log("\n--- Benchmark: Complex Object Serialization ---");

            int iterations = 1000;
            var data = new List<GameData>();
            for (int i = 0; i < 100; i++)
            {
                data.Add(new GameData
                {
                    name = $"Object_{i}",
                    position = new Vector3(i,i * 2,i * 3),
                    rotation = Quaternion.Euler(i,i * 2,i * 3),
                    color = new Color(i / 100f,i / 100f,i / 100f)
                });
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string json = SimpleJson.SerializeObject(data);
            }
            sw.Stop();

            Debug.Log($"Iterations: {iterations}");
            Debug.Log($"Objects per iteration: {data.Count}");
            Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");
            Debug.Log($"Per operation: {(double)sw.ElapsedMilliseconds / iterations:F4}ms");
        }

        [System.Serializable]
        public class GameData
        {
            public string name;
            public Vector3 position;
            public Quaternion rotation;
            public Color color;
        }
    }

}


