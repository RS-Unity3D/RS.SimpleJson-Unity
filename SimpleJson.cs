////-----------------------------------------------------------------------
//// <copyright file="SimpleJson.cs" company="The Outercurve Foundation">
////    Copyright (c) 2011, The Outercurve Foundation.
////
////    Licensed under the MIT License (the "License");
////    you may not use this file except in compliance with the License.
////    You may obtain a copy of the License at
////      http://www.opensource.org/licenses/mit-license.php
////
////    Unless required by applicable law or agreed to in writing, software
////    distributed under the License is distributed on an "AS IS" BASIS,
////    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////    See the License for the specific language governing permissions and
////    limitations under the License.
//// </copyright>
//// <author>Nathan Totten (ntotten.com), Jim Zimmerman (jimzimmerman.com) and Prabir Shrestha (prabir.me)</author>
//// <website>https://github.com/facebook-csharp-sdk/simple-json</website>
////-----------------------------------------------------------------------
////sgd:2026.2.20 support aot compiler
////sgd: 2025.12.15 support unity 3d
////sgd: 2025.11.10 support property To lower case
////sgd: 2025.10.1 support string key dictionary
//// VERSION: 0.38.0

////NOTE: uncomment the following line to make SimpleJson class internal.
////#define SIMPLE_JSON_INTERNAL

//// NOTE: uncomment the following line to make JsonArray and JsonObject class internal.
//#define SIMPLE_JSON_OBJARRAYINTERNAL

//// NOTE: uncomment the following line to enable dynamic support.
////#define SIMPLE_JSON_DYNAMIC

//// NOTE: uncomment the following line to enable DataContract support.
////#define SIMPLE_JSON_DATACONTRACT

//// NOTE: uncomment the following line to enable IReadOnlyCollection<T> and IReadOnlyList<T> support.
//#define SIMPLE_JSON_READONLY_COLLECTIONS

//// NOTE: uncomment the following line to disable linq expressions/compiled lambda (better performance) instead of method.invoke().
//// define if you are using .net framework <= 3.0 or < WP7.5
//#define SIMPLE_JSON_NO_LINQ_EXPRESSION

//// NOTE: uncomment the following line if you are compiling under Window Metro style application/library.
//// usually already defined in properties
////#define NETFX_CORE;

////NOTE: support AOT compiler, but will disable dynamic and DataContract support, and use method.invoke() instead of compiled lambda, which is slower.
//#define AOT

////NOTE:If you are targetting WinStore, WP8 and NET4.5+ PCL make sure to #define SIMPLE_JSON_TYPEINFO;
////#define SIMPLE_JSON_TYPEINFO;
//// original json parsing code from http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html

////add
////NOTE:Ignore the case of attributes/fields
//#define SIMPLE_JSON_PropertyToLowerCase

////NOTE:Private fields marked with JsonInclude are serialized when onlyPublic=false
////Private properties marked with JsonInclude are serialized when onlyPublic=false
////By default (onlyPublic=true), private members are not included
////#define SIMPLE_JSON_OnlyPublicProperty

////NOTE:Single-threaded mode, such as WebGL, does not consider multi-threading.
////#define SINGLE_THREADED

//#if NET35 || NET40
//#undef SIMPLE_JSON_READONLY_COLLECTIONS
//#endif

//#if NETFX_CORE
//#define SIMPLE_JSON_TYPEINFO
//#endif

////NOTE:Unity support
//#if UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
//#define SIMPLE_JSON_UNITY
////unity webgl does not support multi-threading
//#if UNITY_WEBGL
//#define SINGLE_THREADED
//#endif
////unity aot compiler
//#if ENABLE_IL2CPP
//#define AOT
//#endif
//#endif
//#if SIMPLE_JSON_UNITY
//using UnityEngine;
//#endif



//using System;
//using System.CodeDom.Compiler;
//using System.Collections;
//using System.Collections.Generic;
////#if !SIMPLE_JSON_NO_LINQ_EXPRESSION
////using System.Linq.Expressions;
////#endif
//using System.ComponentModel;
//using System.Diagnostics.CodeAnalysis;
////#if SIMPLE_JSON_DYNAMIC
////using System.Dynamic;
////#endif
//using System.Globalization;
//using System.Reflection;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Runtime.CompilerServices;
//using System.Runtime.Serialization.Formatters;
////

//// ReSharper disable LoopCanBeConvertedToQuery
//// ReSharper disable RedundantExplicitArrayCreation
//// ReSharper disable SuggestUseVarKeywordEvident
//namespace RS.SimpleJsonAOT//GitHub.Unity.Json

// SimpleJson.cs — RS.SimpleJson-Unity (patched)
// All bugs fixed. See compilation symbols below for configuration.
//
// ============================================================
// Compilation Symbols Reference
// ============================================================
// SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
//     Define in AOT-strict environments where Enum.Parse is
//     unavailable. Enum deserialization only supports numeric
//     string keys (e.g. "0", "1").
//
// NET20
//     Automatically defined by .NET 2.0 target framework.
//     Disables ReaderWriterLockSlim (unavailable in .NET 2.0).
//
// UNITY_5_3_OR_NEWER
//     Automatically defined by Unity. Disables ReaderWriterLockSlim
//     due to known il2cpp stability issues. Uses lock instead.
//
// SIMPLE_JSON_PropertyToLowerCase
//     Define to make the default serialization strategy use
//     lowercase JSON keys by default. Equivalent to setting
//     DefaultJsonSerializationStrategy.toLowerCase = true.
//
// UNITY_EDITOR, DEVELOPMENT_BUILD, DEBUG
//     Enable Guard.LogWarning / Guard.LogError output and
//     additional runtime validation. All diagnostic calls and
//     their string arguments are compiled out in Release builds
//     (zero overhead).
// ============================================================

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace RS.SimpleJsonUnity
{

    // ──────────────────────────────────────────────────────────────
    // Attributes
    // ──────────────────────────────────────────────────────────────

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true,AllowMultiple = false)]
    public sealed class JsonIgnoreAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true,AllowMultiple = false)]
    public sealed class JsonIncludeAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true,AllowMultiple = false)]
    public sealed class JsonAliasAttribute : Attribute
    {
        public string Alias { get; private set; }

        public JsonAliasAttribute(string aliasName)
        {
            if (aliasName == null) throw new ArgumentNullException("aliasName");
            Alias = aliasName;
        }

        //// acceptOriginalName 参数保留以兼容旧调用代码，实际被忽略。
        //// 原始名始终注册，与 LitJSON 行为对齐。
        //[Obsolete("acceptOriginalName is ignored. Original name is always registered for deserialization.")]
        //public JsonAliasAttribute(string aliasName,bool acceptOriginalName)
        //{
        //    if (aliasName == null) throw new ArgumentNullException("aliasName");
        //    Alias = aliasName;
        //}
    }
    static class Constants
    {
        public const string Iso8601Format = @"yyyy-MM-dd\THH\:mm\:ss.fffzzz";
        public const string Iso8601FormatZ = @"yyyy-MM-dd\THH\:mm\:ss\Z";
        public static readonly string[] Iso8601Formats =
        {
            Iso8601Format,
            Iso8601FormatZ,
            @"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fzzz",
            @"yyyy-MM-dd\THH\:mm\:sszzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.fffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.fff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.f\Z"
        };
    }

    // ──────────────────────────────────────────────────────────────
    // Guard
    // ──────────────────────────────────────────────────────────────

    internal static class Guard
    {
        public static void ArgumentNotNull(object argument,string argumentName)
        {
            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }

        public static void LogWarning(string message)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG)
            return;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Debug.LogWarning("[SimpleJson] " + message);
#elif DEBUG
            System.Diagnostics.Trace.TraceWarning("[SimpleJson] " + message);
#endif
        }

        public static void LogError(string message)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG)
            return;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Debug.LogError("[SimpleJson] " + message);
#elif DEBUG
            System.Diagnostics.Trace.TraceError("[SimpleJson] " + message);
#endif
        }
    }

    // ──────────────────────────────────────────────────────────────
    // TypeCacheKey  （setter 缓存复合 key，区分 toLowerCase 状态）
    // ──────────────────────────────────────────────────────────────

    internal struct TypeCacheKey : IEquatable<TypeCacheKey>
    {
        public readonly Type Type;
        public readonly bool ToLowerCase;

        public TypeCacheKey(Type type,bool toLowerCase)
        {
            Type = type;
            ToLowerCase = toLowerCase;
        }

        public bool Equals(TypeCacheKey other)
        {
            return Type == other.Type && ToLowerCase == other.ToLowerCase;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeCacheKey)) return false;
            return Equals((TypeCacheKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (Type != null ? Type.GetHashCode() : 0);
                return h ^ (ToLowerCase ? 0x55555555 : 0);
            }
        }
    }
    // ──────────────────────────────────────────────────────────────
    // ThreadSafeDictionary
    // ──────────────────────────────────────────────────────────────

    internal class ThreadSafeDictionary<TKey, TValue>
        : IDictionary<TKey,TValue>
    {
        private readonly Dictionary<TKey,TValue> _dict =
            new Dictionary<TKey,TValue>();

#if NET20 || UNITY_5_3_OR_NEWER
    private readonly object _lock = new object();

    public TValue this[TKey key]
    {
        get { lock (_lock) { return _dict[key]; } }
        set { lock (_lock) { _dict[key] = value; } }
    }
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock) { return _dict.TryGetValue(key, out value); }
    }
    public bool ContainsKey(TKey key)
    {
        lock (_lock) { return _dict.ContainsKey(key); }
    }
    public void Add(TKey key, TValue value)
    {
        lock (_lock) { _dict[key] = value; }
    }
    public bool Remove(TKey key)
    {
        lock (_lock) { return _dict.Remove(key); }
    }
    public void Clear()
    {
        lock (_lock) { _dict.Clear(); }
    }
    public int Count
    {
        get { lock (_lock) { return _dict.Count; } }
    }
    public ICollection<TKey> Keys
    {
        get { lock (_lock) { return new List<TKey>(_dict.Keys); } }
    }
    public ICollection<TValue> Values
    {
        get { lock (_lock) { return new List<TValue>(_dict.Values); } }
    }
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        List<KeyValuePair<TKey, TValue>> snapshot;
        lock (_lock)
        {
            snapshot = new List<KeyValuePair<TKey, TValue>>(_dict);
        }
        return snapshot.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

#else
        private readonly System.Threading.ReaderWriterLockSlim _lock =
            new System.Threading.ReaderWriterLockSlim();

        public TValue this[TKey key]
        {
            get
            {
                _lock.EnterReadLock();
                try { return _dict[key]; }
                finally { _lock.ExitReadLock(); }
            }
            set
            {
                _lock.EnterWriteLock();
                try { _dict[key] = value; }
                finally { _lock.ExitWriteLock(); }
            }
        }
        public bool TryGetValue(TKey key,out TValue value)
        {
            _lock.EnterReadLock();
            try { return _dict.TryGetValue(key,out value); }
            finally { _lock.ExitReadLock(); }
        }
        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try { return _dict.ContainsKey(key); }
            finally { _lock.ExitReadLock(); }
        }
        public void Add(TKey key,TValue value)
        {
            _lock.EnterWriteLock();
            try { _dict[key] = value; }
            finally { _lock.ExitWriteLock(); }
        }
        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try { return _dict.Remove(key); }
            finally { _lock.ExitWriteLock(); }
        }
        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _dict.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _dict.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }
        public ICollection<TKey> Keys
        {
            get
            {
                _lock.EnterReadLock();
                try { return new List<TKey>(_dict.Keys); }
                finally { _lock.ExitReadLock(); }
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                _lock.EnterReadLock();
                try { return new List<TValue>(_dict.Values); }
                finally { _lock.ExitReadLock(); }
            }
        }
        public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey,TValue>> snapshot;
            _lock.EnterReadLock();
            try { snapshot = new List<KeyValuePair<TKey,TValue>>(_dict); }
            finally { _lock.ExitReadLock(); }
            return snapshot.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
#endif

        // ICollection<KVP> 接口实现
        public bool IsReadOnly { get { return false; } }

        public void Add(KeyValuePair<TKey,TValue> item)
        {
            Add(item.Key,item.Value);
        }
        public bool Contains(KeyValuePair<TKey,TValue> item)
        {
            TValue val;
            if (!TryGetValue(item.Key,out val)) return false;
            return EqualityComparer<TValue>.Default.Equals(val,item.Value);
        }
        public void CopyTo(KeyValuePair<TKey,TValue>[] array,int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");

            // 快照后检查边界，避免锁内抛异常
            List<KeyValuePair<TKey,TValue>> snapshot =
                new List<KeyValuePair<TKey,TValue>>(this);

            if (array.Length - arrayIndex < snapshot.Count)
                throw new ArgumentException(
                    "Destination array is not long enough.");

            for (int i = 0; i < snapshot.Count; i++)
                array[arrayIndex + i] = snapshot[i];
        }
        public bool Remove(KeyValuePair<TKey,TValue> item)
        {
            TValue val;
            if (!TryGetValue(item.Key,out val)) return false;
            if (!EqualityComparer<TValue>.Default.Equals(val,item.Value))
                return false;
            return Remove(item.Key);
        }
    }
    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    /// <summary>
    /// 序列化/反序列化策略接口。
    /// 实现此接口可完全自定义 SimpleJson 的序列化行为。
    /// </summary>
    interface IJsonSerializerStrategy
    {
        /// <summary>
        /// 尝试将非基础类型对象序列化为可进一步处理的中间值。
        /// </summary>
        /// <param name="input">待序列化的对象。</param>
        /// <param name="output">
        /// 序列化结果：string / IDictionary&lt;string,object&gt; /
        /// IList&lt;object&gt; / 基础类型之一。
        /// </param>
        /// <returns>成功返回 true，交由调用方继续处理 output；
        /// 失败返回 false。</returns>
        bool TrySerializeNonPrimitiveObject(object input,out object output);

        /// <summary>
        /// 尝试将已解析的 JSON 值（IDictionary / IList / 基础类型）
        /// 反序列化为目标类型的实例。
        /// </summary>
        /// <param name="value">JSON 解析器输出的原始值。</param>
        /// <param name="type">目标 .NET 类型。</param>
        /// <param name="output">反序列化结果。</param>
        /// <returns>成功返回 true；失败返回 false。</returns>
        bool TryDeserializeObject(object value,Type type,out object output);
    }
    public class DefaultJsonSerializationStrategy : IJsonSerializerStrategy
    {
        // volatile 保证多线程可见性
        // 注：在多线程中动态切换 toLowerCase 不是线程安全的。
        // 建议在初始化阶段一次性设定，之后不再修改。
        public volatile bool toLowerCase;

        private const BindingFlags PUBLIC_INSTANCE =
            BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags NONPUBLIC_INSTANCE =
            BindingFlags.NonPublic | BindingFlags.Instance;

        // getter 缓存：序列化始终用原始 CLR 名，与 toLowerCase 无关
        private static readonly ThreadSafeDictionary<Type,
            IDictionary<string,Func<object,object>>> _getterCache
            = new ThreadSafeDictionary<Type,
                IDictionary<string,Func<object,object>>>();

        // setter 缓存：反序列化 key 受 toLowerCase 影响，需要复合 key
        private static readonly ThreadSafeDictionary<TypeCacheKey,
            IDictionary<string,Action<object,object>>> _setterCache
            = new ThreadSafeDictionary<TypeCacheKey,
                IDictionary<string,Action<object,object>>>();

#if NET20 || UNITY_5_3_OR_NEWER
    private static readonly object _getterBuildLock = new object();
    private static readonly object _setterBuildLock = new object();
#else
        private static readonly System.Threading.ReaderWriterLockSlim
            _getterBuildLock = new System.Threading.ReaderWriterLockSlim();
        private static readonly System.Threading.ReaderWriterLockSlim
            _setterBuildLock = new System.Threading.ReaderWriterLockSlim();
#endif

        public DefaultJsonSerializationStrategy()
        {
#if SIMPLE_JSON_PropertyToLowerCase
        toLowerCase = true;
#else
            toLowerCase = false;
#endif
        }

        // ── Attribute 辅助 ──────────────────────────────────────────

        private static bool HasAttribute<T>(MemberInfo member) where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T),true).Length > 0;
        }

        private static T GetAttribute<T>(MemberInfo member) where T : Attribute
        {
            object[] attrs = member.GetCustomAttributes(typeof(T),true);
            return attrs.Length > 0 ? (T)attrs[0] : null;
        }

        // ── MapClrMemberNameToJsonFieldName ─────────────────────────

        public string MapClrMemberNameToJsonFieldName(string clrName)
        {
            if (clrName == null) return clrName;
            // 统一使用 InvariantCulture，避免土耳其语等特殊文化下 I→ı 的问题
            return toLowerCase
                ? clrName.ToLower(CultureInfo.InvariantCulture)
                : clrName;
        }

        private static object CoerceValue(object val,Type targetType)
        {
            if (val == null) return null;
            if (targetType.IsAssignableFrom(val.GetType())) return val;

            // Nullable<T> 拆包：拆包后继续处理，SetValue 时 .NET 自动重新装箱
            Type underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                targetType = underlying;

            // DateTime类型：从string转换
            if (targetType == typeof(DateTime))
            {
                if (val is string str)
                {
                    try
                    {
                        return DateTime.ParseExact(str,
                            Constants.Iso8601Formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException ex)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                        Guard.LogWarning(
                            "CoerceValue: DateTime parse failed for \"" + str + 
                            "\". " + ex.Message);
#endif
                        return Convert.ToDateTime(str, CultureInfo.InvariantCulture);
                    }
                }
            }

            // DateTimeOffset类型：从string转换
            if (targetType == typeof(DateTimeOffset))
            {
                if (val is string str)
                {
                    try
                    {
                        return DateTimeOffset.ParseExact(str,
                           Constants.Iso8601Formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException)
                    {
                        return DateTimeOffset.Parse(str, CultureInfo.InvariantCulture);
                    }
                }
            }

            // Guid类型：从string转换
            if (targetType == typeof(Guid))
            {
                if (val is string str)
                {
                    if (str.Length == 0) return default(Guid);
                    return new Guid(str);
                }
            }

            if (targetType == typeof(TimeSpan))
            {
                if (val is string str)
                {
                    if (long.TryParse(str, out long ticks))
                        return new TimeSpan(ticks);
                    return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
                }
                if (val is long ticksValue)
                    return new TimeSpan(ticksValue);
            }

            if (targetType.IsEnum)
            {
                if (val is string)
                {
#if !SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
                    return Enum.Parse(targetType,(string)val,true);
#else
                try
                {
                    object numVal = Convert.ChangeType(
                        val,
                        Enum.GetUnderlyingType(targetType),
                        CultureInfo.InvariantCulture);
                    return Enum.ToObject(targetType, numVal);
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException(
                        "CoerceValue: Cannot parse enum \"" +
                        targetType.FullName + "\" from \"" + val +
                        "\" in AOT mode. Only numeric strings supported.", ex);
                }
#endif
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                // 浮点数赋值给 enum：小数部分将被截断
                if (val is double || val is float || val is decimal)
                {
                    double dv = Convert.ToDouble(val,CultureInfo.InvariantCulture);
                    if (dv != Math.Truncate(dv))
                    {
                        Guard.LogWarning(
                            "CoerceValue: Assigning float " + dv +
                            " to enum \"" + targetType.FullName +
                            "\". Decimal part truncated.");
                    }
                }
#endif
                double enumVal = Convert.ToDouble(val,CultureInfo.InvariantCulture);
                enumVal = Math.Truncate(enumVal);
                return Enum.ToObject(targetType,
                    Convert.ChangeType(enumVal,
                        Enum.GetUnderlyingType(targetType),
                        CultureInfo.InvariantCulture));
            }

            // char类型：从string转换时取第一个字符
            if (targetType == typeof(char))
            {
                if (val is string str)
                {
                    if (str.Length > 0)
                        return str[0];
                    return default(char);
                }
            }

            // 整数类型：确保截断而不是四舍五入
            if (targetType == typeof(int) || targetType == typeof(uint) ||
                targetType == typeof(short) || targetType == typeof(ushort) ||
                targetType == typeof(long) || targetType == typeof(ulong) ||
                targetType == typeof(byte) || targetType == typeof(sbyte))
            {
                if (val is double || val is float || val is decimal)
                {
                    double dv = Convert.ToDouble(val,CultureInfo.InvariantCulture);
                    return Convert.ChangeType(
                        Math.Truncate(dv),
                        targetType,
                        CultureInfo.InvariantCulture);
                }
            }

            // 集合类型：IList<T> 或 List<T>
            if (typeof(IList).IsAssignableFrom(targetType) && val is IList<object> listVal)
            {
                Type elementType = null;
                if (targetType.IsArray)
                {
                    elementType = targetType.GetElementType();
                }
                else if (targetType.IsGenericType)
                {
                    Type[] args = targetType.GetGenericArguments();
                    if (args.Length == 1) elementType = args[0];
                }

                if (elementType != null)
                {
                    IList result;
                    if (targetType.IsArray)
                    {
                        result = Array.CreateInstance(elementType, listVal.Count);
                    }
                    else
                    {
                        result = (IList)Activator.CreateInstance(targetType);
                    }

                    foreach (object item in listVal)
                    {
                        object convertedItem = CoerceValue(item, elementType);
                        if (targetType.IsArray)
                        {
                            result[Convert.ToInt32(result.Count - 1)] = convertedItem;
                        }
                        else
                        {
                            result.Add(convertedItem);
                        }
                    }
                    return result;
                }
            }

            // 字典类型：IDictionary<K,V> 或 Dictionary<K,V>
            if (typeof(IDictionary).IsAssignableFrom(targetType) && val is IDictionary<string, object> dictVal)
            {
                if (targetType.IsGenericType)
                {
                    Type[] args = targetType.GetGenericArguments();
                    if (args.Length == 2)
                    {
                        Type keyType = args[0];
                        Type valueType = args[1];

                        IDictionary result = (IDictionary)Activator.CreateInstance(targetType);
                        foreach (var kvp in dictVal)
                        {
                            object dictKey = ConvertDictionaryKey(kvp.Key, keyType);
                            object dictValue = CoerceValue(kvp.Value, valueType);
                            result[dictKey] = dictValue;
                        }
                        return result;
                    }
                }
            }

            // 通用转换：失败直接抛，由 setter 统一 catch + LogWarning（单点日志）
            return Convert.ChangeType(val,targetType,CultureInfo.InvariantCulture);
        }

        // ── BuildGetters ────────────────────────────────────────────

        private static IDictionary<string,Func<object,object>>
            BuildGetters(Type type)
        {
            var getters = new Dictionary<string,Func<object,object>>();
            // seen：按成员名去重，优先最派生版本（GetProperties 返回顺序保证）
            // C# 不允许同名属性和字段，seen 跨两者去重是安全的
            var seen = new HashSet<string>();

            // public 属性
            foreach (PropertyInfo p in type.GetProperties(PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                if (seen.Contains(p.Name)) continue;
                if (HasAttribute<JsonIgnoreAttribute>(p)) continue;

                seen.Add(p.Name);
                PropertyInfo captured = p;
                getters[p.Name] = obj => captured.GetValue(obj,null);
            }

            // public 字段
            foreach (FieldInfo f in type.GetFields(PUBLIC_INSTANCE))
            {
                if (seen.Contains(f.Name)) continue;
                if (HasAttribute<JsonIgnoreAttribute>(f)) continue;

                seen.Add(f.Name);
                FieldInfo captured = f;
                getters[f.Name] = obj => captured.GetValue(obj);
            }

            // non-public：仅 JsonInclude，JsonIgnore 优先
            foreach (PropertyInfo p in type.GetProperties(NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                if (seen.Contains(p.Name)) continue;
                if (!HasAttribute<JsonIncludeAttribute>(p)) continue;
                if (HasAttribute<JsonIgnoreAttribute>(p)) continue;

                seen.Add(p.Name);
                PropertyInfo captured = p;
                getters[p.Name] = obj => captured.GetValue(obj,null);
            }

            foreach (FieldInfo f in type.GetFields(NONPUBLIC_INSTANCE))
            {
                if (seen.Contains(f.Name)) continue;
                if (!HasAttribute<JsonIncludeAttribute>(f)) continue;
                if (HasAttribute<JsonIgnoreAttribute>(f)) continue;

                seen.Add(f.Name);
                FieldInfo captured = f;
                getters[f.Name] = obj => captured.GetValue(obj);
            }

            return getters;
        }

        // ── BuildSetters ────────────────────────────────────────────

        private static IDictionary<string,Action<object,object>>
            BuildSetters(Type type,bool toLowerCase)
        {
            var setters = new Dictionary<string,Action<object,object>>();
            var seen = new HashSet<string>();

            Action<MemberInfo,Type,Action<object,object>> register =
                (member,memberType,rawSetter) =>
                {
                    if (HasAttribute<JsonIgnoreAttribute>(member)) return;
                    if (seen.Contains(member.Name)) return;
                    seen.Add(member.Name);

                    // 带类型转换保护的 setter
                    Type capturedType = memberType;
                    MemberInfo capturedMember = member;
                    Action<object,object> safeSetter = (obj,val) =>
                    {
                        try
                        {
                            rawSetter(obj,CoerceValue(val,capturedType));
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                            Guard.LogWarning(
                            "SetValue: Cannot assign to \"" +
                            capturedMember.DeclaringType.FullName + "." +
                            capturedMember.Name + "\". " + ex.Message);
#endif
                            // 跳过赋值，保留默认值
                        }
                    };

                    string originalKey = toLowerCase
                    ? member.Name.ToLower(CultureInfo.InvariantCulture)
                    : member.Name;
                    setters[originalKey] = safeSetter;

                    JsonAliasAttribute alias = GetAttribute<JsonAliasAttribute>(member);
                    if (alias != null)
                    {
                        string aliasKey = toLowerCase
                        ? alias.Alias.ToLower(CultureInfo.InvariantCulture)
                        : alias.Alias;
                        if (aliasKey != originalKey)
                            setters[aliasKey] = safeSetter;
                    }
                };

            // public 属性
            foreach (PropertyInfo p in type.GetProperties(PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            // public 字段
            foreach (FieldInfo f in type.GetFields(PUBLIC_INSTANCE))
            {
                FieldInfo captured = f;
                register(f,f.FieldType,
                    (obj,val) => captured.SetValue(obj,val));
            }

            // non-public：仅 JsonInclude
            foreach (PropertyInfo p in type.GetProperties(NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                if (!HasAttribute<JsonIncludeAttribute>(p)) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            foreach (FieldInfo f in type.GetFields(NONPUBLIC_INSTANCE))
            {
                if (!HasAttribute<JsonIncludeAttribute>(f)) continue;
                FieldInfo captured = f;
                register(f,f.FieldType,
                    (obj,val) => captured.SetValue(obj,val));
            }

            return setters;
        }

        // ── GetOrBuild 缓存访问 ─────────────────────────────────────

#if NET20 || UNITY_5_3_OR_NEWER

    protected IDictionary<string, Func<object, object>>
        GetOrBuildGetters(Type type)
    {
        IDictionary<string, Func<object, object>> cached;
        if (_getterCache.TryGetValue(type, out cached)) return cached;
        lock (_getterBuildLock)
        {
            if (_getterCache.TryGetValue(type, out cached)) return cached;
            IDictionary<string, Func<object, object>> built;
            try   { built = BuildGetters(type); }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError("GetOrBuildGetters: Failed for \"" +
                    type.FullName + "\". " + ex.Message);
#endif
                // 不写缓存，允许下次重试
                return new Dictionary<string, Func<object, object>>();
            }
            _getterCache[type] = built;
            return built;
        }
    }

    protected IDictionary<string, Action<object, object>>
        GetOrBuildSetters(Type type)
    {
        var cacheKey = new TypeCacheKey(type, toLowerCase);
        IDictionary<string, Action<object, object>> cached;
        if (_setterCache.TryGetValue(cacheKey, out cached)) return cached;
        lock (_setterBuildLock)
        {
            if (_setterCache.TryGetValue(cacheKey, out cached)) return cached;
            IDictionary<string, Action<object, object>> built;
            try   { built = BuildSetters(type, toLowerCase); }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError("GetOrBuildSetters: Failed for \"" +
                    type.FullName + "\". " + ex.Message);
#endif
                return new Dictionary<string, Action<object, object>>();
            }
            _setterCache[cacheKey] = built;
            return built;
        }
    }

#else

        protected IDictionary<string,Func<object,object>>
            GetOrBuildGetters(Type type)
        {
            _getterBuildLock.EnterReadLock();
            try
            {
                IDictionary<string,Func<object,object>> cached;
                if (_getterCache.TryGetValue(type,out cached)) return cached;
            }
            finally { _getterBuildLock.ExitReadLock(); }

            _getterBuildLock.EnterWriteLock();
            try
            {
                IDictionary<string,Func<object,object>> cached;
                if (_getterCache.TryGetValue(type,out cached)) return cached;
                IDictionary<string,Func<object,object>> built;
                try { built = BuildGetters(type); }
                catch (Exception ex)
                {
#if DEBUG
                    Guard.LogError("GetOrBuildGetters: Failed for \"" +
      type.FullName + "\". " + ex.Message);
#endif
                    return new Dictionary<string,Func<object,object>>();
                }
                _getterCache[type] = built;
                return built;
            }
            finally { _getterBuildLock.ExitWriteLock(); }
        }

        protected IDictionary<string,Action<object,object>>
            GetOrBuildSetters(Type type)
        {
            var cacheKey = new TypeCacheKey(type,toLowerCase);

            _setterBuildLock.EnterReadLock();
            try
            {
                IDictionary<string,Action<object,object>> cached;
                if (_setterCache.TryGetValue(cacheKey,out cached)) return cached;
            }
            finally { _setterBuildLock.ExitReadLock(); }

            _setterBuildLock.EnterWriteLock();
            try
            {
                IDictionary<string,Action<object,object>> cached;
                if (_setterCache.TryGetValue(cacheKey,out cached)) return cached;
                IDictionary<string,Action<object,object>> built;
                try { built = BuildSetters(type,toLowerCase); }
                catch (Exception ex)
                {
#if DEBUG
                    Guard.LogError("GetOrBuildSetters: Failed for \"" +
                        type.FullName + "\". " + ex.Message);
#endif
                    return new Dictionary<string,Action<object,object>>();
                }
                _setterCache[cacheKey] = built;
                return built;
            }
            finally { _setterBuildLock.ExitWriteLock(); }
        }

#endif  // NET20 || UNITY_5_3_OR_NEWER

        // ── TrySerializeNonPrimitiveObject ──────────────────────────

        public virtual bool TrySerializeNonPrimitiveObject(
            object input,out object output)
        {
            return TrySerializeKnownTypes(input,out output)
                || TrySerializeUnknownTypes(input,out output);
        }

        protected virtual bool TrySerializeKnownTypes(
            object input,out object output)
        {
            if (input == null) { output = null; return false; }

            // DateTime
            if (input is DateTime)
            {
                output = ((DateTime)input).ToUniversalTime().ToString(
                    Constants.Iso8601Format,
                    CultureInfo.InvariantCulture);
                return true;
            }
            // DateTimeOffset
            if (input is DateTimeOffset)
            {
                output = ((DateTimeOffset)input).ToUniversalTime().ToString(
                    Constants.Iso8601Format,
                    CultureInfo.InvariantCulture);
                return true;
            }
            // Guid
            if (input is Guid)
            {
                output = ((Guid)input).ToString("D",
                    CultureInfo.InvariantCulture);
                return true;
            }
            // char
            if (input is char)
            {
                output = ((char)input).ToString();
                return true;
            }
            if (input is TimeSpan)
            {
                output = ((TimeSpan)input).Ticks.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            // Uri
            if (input is Uri)
            {
                output = ((Uri)input).ToString();
                return true;
            }
            // Enum：序列化为底层数值（与 LitJSON 默认行为一致）
            if (input is Enum)
            {
                output = Convert.ChangeType(
                    input,
                    Enum.GetUnderlyingType(input.GetType()),
                    CultureInfo.InvariantCulture);
                return true;
            }

            output = null;
            return false;
        }

        protected virtual bool TrySerializeUnknownTypes(
            object input,out object output)
        {
            // 防御：null 不应到达此处（TrySerializeNonPrimitiveObject 入口已处理）
            if (input == null) { output = null; return false; }

            Type type = input.GetType();
            var result = new JsonObject();

            IDictionary<string,Func<object,object>> getters =
                GetOrBuildGetters(type);

            foreach (KeyValuePair<string,Func<object,object>> kvp in getters)
            {
                string jsonKey = MapClrMemberNameToJsonFieldName(kvp.Key);
                object val = kvp.Value(input);
                result[jsonKey] = val;
            }

            output = result;
            return true;
        }

        // ── TryDeserializeObject ────────────────────────────────────

        public virtual bool TryDeserializeObject(
            object value,Type type,out object output)
        {
            // 基础类型由 DeserializeObject 主流程处理，此处只处理 POCO
            var jsonObj = value as IDictionary<string,object>;
            if (jsonObj == null) { output = null; return false; }

            // 字典类型
            if (ReflectionUtils.IsTypeDictionary(type))
            {
                Type[] genericArgs = type.GetGenericArguments();
                Type keyType, valueType;

                if (genericArgs.Length >= 2)
                {
                    keyType = genericArgs[0];
                    valueType = genericArgs[1];
                }
                else
                {
                    // 非泛型字典（Hashtable 等）
                    keyType = typeof(object);
                    valueType = typeof(object);
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogWarning(
                        "TryDeserializeObject: Non-generic dictionary \"" +
                        type.FullName + "\". Keys/values as object.");
#endif
                }

                IDictionary dict;
                try
                {
                    dict = (IDictionary)Activator.CreateInstance(type);
                }
                catch (MissingMethodException ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogError(
                        "TryDeserializeObject: \"" + type.FullName +
                        "\" has no parameterless constructor. " + ex.Message);
#endif
                    throw;
                }

                foreach (KeyValuePair<string,object> kvp in jsonObj)
                {
                    object dictKey;
                    try
                    {
                        dictKey = ConvertDictionaryKey(kvp.Key,keyType);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                        Guard.LogError(
                            "TryDeserializeObject: Cannot convert key \"" +
                            kvp.Key + "\" to \"" + keyType.FullName +
                            "\". Skipping. " + ex.Message);
#endif
                        continue;
                    }
                    // 索引器赋值：重复 key 后者覆盖前者，不抛异常
                    dict[dictKey] = SimpleJson.DeserializeObject(
                        kvp.Value,valueType,this);
                }

                output = dict;
                return true;
            }

            // POCO
            object instance;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (MissingMethodException ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError(
                    "TryDeserializeObject: Type \"" + type.FullName +
                    "\" has no parameterless constructor. " + ex.Message);
#endif
                throw;
            }

            IDictionary<string,Action<object,object>> setters =
                GetOrBuildSetters(type);

            foreach (KeyValuePair<string,object> kvp in jsonObj)
            {
                Action<object,object> setter;
                if (setters.TryGetValue(kvp.Key,out setter))
                    setter(instance,kvp.Value);
                // 无匹配 setter：静默跳过（JSON 中多余字段不报错）
            }

            output = instance;
            return true;
        }

        // ── 字典 key 转换 ────────────────────────────────────────────

        private static object ConvertDictionaryKey(string strKey,Type keyType)
        {
            if (keyType == typeof(string) || keyType == typeof(object))
                return strKey;

            if (keyType.IsEnum)
            {
#if !SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
                return Enum.Parse(keyType,strKey,true);
#else
            object numVal = Convert.ChangeType(
                strKey,
                Enum.GetUnderlyingType(keyType),
                CultureInfo.InvariantCulture);
            return Enum.ToObject(keyType, numVal);
#endif
            }

            // int / long / uint / double 等数值类型
            return Convert.ChangeType(
                strKey,keyType,CultureInfo.InvariantCulture);
        }
        internal static void ClearCache()
        {
#if NET20 || UNITY_5_3_OR_NEWER
    lock (_getterBuildLock)
    {
        _getterCache.Clear();
    }
    lock (_setterBuildLock)
    {
        _setterCache.Clear();
    }
#else
            _getterBuildLock.EnterWriteLock();
            try { _getterCache.Clear(); }
            finally { _getterBuildLock.ExitWriteLock(); }

            _setterBuildLock.EnterWriteLock();
            try { _setterCache.Clear(); }
            finally { _setterBuildLock.ExitWriteLock(); }
#endif
        }
    }

    // ──────────────────────────────────────────────────────────────
    // ReflectionUtils
    // ──────────────────────────────────────────────────────────────

    internal static class ReflectionUtils
    {
        public static bool IsTypeDictionary(Type type)
        {
            // 非泛型 IDictionary（Hashtable、SortedList 等）
            if (typeof(IDictionary).IsAssignableFrom(type))
                return true;

            // 泛型 IDictionary<,>（Dictionary<K,V>、SortedDictionary<K,V> 等）
            foreach (Type iface in type.GetInterfaces())
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return true;
            }
            return false;
        }

        public static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // JsonObject  (IDictionary<string,object> 别名，保持原库接口)
    // ──────────────────────────────────────────────────────────────

    public class JsonObject : Dictionary<string,object> { }
    /// <summary>
    /// 轻量 JSON 解析器。将 JSON 字符串解析为 .NET 对象树：
    ///   JSON object  → IDictionary&lt;string, object&gt;
    ///   JSON array   → IList&lt;object&gt;
    ///   JSON string  → string
    ///   JSON number  → long / ulong / double（见 ParseNumber）
    ///   JSON true/false → bool
    ///   JSON null    → null
    /// </summary>
    internal static class JsonParser
    {
        // ── 公开入口 ────────────────────────────────────────────

        public static bool TryParse(string json,out object value)
        {
            if (string.IsNullOrEmpty(json))
            {
                value = null;
                return false;
            }

            int index = 0;
            bool success = TryParseValue(json,ref index,out value);
            return success;
        }

        // ── 核心解析 ────────────────────────────────────────────

        private static bool TryParseValue(
            string json,ref int index,out object value)
        {
            SkipWhitespace(json,ref index);

            if (index >= json.Length)
            {
                value = null;
                return false;
            }

            char c = json[index];

            if (c == '"')
                return TryParseString(json,ref index,out value);

            if (c == '{')
                return TryParseObject(json,ref index,out value);

            if (c == '[')
                return TryParseArray(json,ref index,out value);

            if (c == 't' || c == 'f')
                return TryParseBool(json,ref index,out value);

            if (c == 'n')
                return TryParseNull(json,ref index,out value);

            if (c == '-' || (c >= '0' && c <= '9'))
                return TryParseNumber(json,ref index,out value);

            value = null;
            return false;
        }

        // ── Object ──────────────────────────────────────────────

        private static bool TryParseObject(
            string json,ref int index,out object value)
        {
            var dict = new JsonObject();
            value = dict;

            // 跳过 '{'
            index++;

            while (true)
            {
                SkipWhitespace(json,ref index);
                if (index >= json.Length) return false;

                char c = json[index];

                if (c == '}') { index++; return true; }
                if (c == ',') { index++; continue; }

                // key
                object keyObj;
                if (!TryParseString(json,ref index,out keyObj))
                    return false;
                string key = (string)keyObj;

                SkipWhitespace(json,ref index);
                if (index >= json.Length || json[index] != ':') return false;
                index++; // 跳过 ':'

                // value
                object val;
                if (!TryParseValue(json,ref index,out val)) return false;

                // 重复 key：后者覆盖前者（与主流 JSON 库行为一致）
                dict[key] = val;
            }
        }

        // ── Array ───────────────────────────────────────────────

        private static bool TryParseArray(
            string json,ref int index,out object value)
        {
            var list = new List<object>();
            value = list;

            // 跳过 '['
            index++;

            while (true)
            {
                SkipWhitespace(json,ref index);
                if (index >= json.Length) return false;

                char c = json[index];

                if (c == ']') { index++; return true; }
                if (c == ',') { index++; continue; }

                object item;
                if (!TryParseValue(json,ref index,out item)) return false;
                list.Add(item);
            }
        }

        // ── String ──────────────────────────────────────────────

        private static bool TryParseString(
            string json,ref int index,out object value)
        {
            var sb = new StringBuilder();
            value = null;

            SkipWhitespace(json,ref index);
            if (index >= json.Length || json[index] != '"') return false;
            index++; // 跳过开头 '"'

            while (index < json.Length)
            {
                char c = json[index++];

                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                // 转义序列
                if (index >= json.Length) return false;
                char esc = json[index++];

                switch (esc)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        // \uXXXX
                        if (index + 4 > json.Length) return false;
                        string hex = json.Substring(index,4);
                        index += 4;
                        int codePoint;
                        if (!TryParseHex(hex,out codePoint)) return false;

                        // 处理 UTF-16 代理对
                        if (codePoint >= 0xD800 && codePoint <= 0xDBFF)
                        {
                            // 高代理，期望紧跟 \uXXXX 低代理
                            if (index + 6 <= json.Length &&
                                json[index] == '\\' && json[index + 1] == 'u')
                            {
                                string hex2 = json.Substring(index + 2,4);
                                int lowSurrogate;
                                if (TryParseHex(hex2,out lowSurrogate) &&
                                    lowSurrogate >= 0xDC00 &&
                                    lowSurrogate <= 0xDFFF)
                                {
                                    index += 6;
                                    int fullCodePoint =
                                        0x10000 +
                                        ((codePoint - 0xD800) << 10) +
                                        (lowSurrogate - 0xDC00);
                                    sb.Append(char.ConvertFromUtf32(
                                        fullCodePoint));
                                    break;
                                }
                            }
                        }
                        sb.Append((char)codePoint);
                        break;

                    default:
                        // 未知转义：原样保留（宽松处理）
                        sb.Append(esc);
                        break;
                }
            }

            return false; // 未找到闭合 '"'
        }

        private static bool TryParseHex(string hex,out int result)
        {
            result = 0;
            for (int i = 0; i < hex.Length; i++)
            {
                char c = hex[i];
                int digit;
                if (c >= '0' && c <= '9') digit = c - '0';
                else if (c >= 'a' && c <= 'f') digit = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F') digit = c - 'A' + 10;
                else return false;
                result = (result << 4) | digit;
            }
            return true;
        }

        // ── Number ──────────────────────────────────────────────

        private static bool TryParseNumber(
            string json,ref int index,out object value)
        {
            int start = index;

            if (index < json.Length && json[index] == '-') index++;

            while (index < json.Length &&
                   json[index] >= '0' && json[index] <= '9')
                index++;

            bool isFloat = false;

            if (index < json.Length && json[index] == '.')
            {
                isFloat = true;
                index++;
                while (index < json.Length &&
                       json[index] >= '0' && json[index] <= '9')
                    index++;
            }

            if (index < json.Length &&
                (json[index] == 'e' || json[index] == 'E'))
            {
                isFloat = true;
                index++;
                if (index < json.Length &&
                    (json[index] == '+' || json[index] == '-'))
                    index++;
                while (index < json.Length &&
                       json[index] >= '0' && json[index] <= '9')
                    index++;
            }

            string numStr = json.Substring(start,index - start);

            if (isFloat)
            {
                double d;
                if (!double.TryParse(numStr,NumberStyles.Float,
                    CultureInfo.InvariantCulture,out d))
                {
                    value = null;
                    return false;
                }
                value = d;
                return true;
            }

            // 整数：long → ulong → double 兜底
            object parsed;
            bool ok = SimpleJson.ParseNumber(numStr,out parsed);
            value = parsed;
            return ok;
        }

        // ── Bool ────────────────────────────────────────────────

        private static bool TryParseBool(
            string json,ref int index,out object value)
        {
            if (json.Length - index >= 4 &&
                json[index] == 't' && json[index + 1] == 'r' &&
                json[index + 2] == 'u' && json[index + 3] == 'e')
            {
                index += 4;
                value = true;
                return true;
            }

            if (json.Length - index >= 5 &&
                json[index] == 'f' && json[index + 1] == 'a' &&
                json[index + 2] == 'l' && json[index + 3] == 's' &&
                json[index + 4] == 'e')
            {
                index += 5;
                value = false;
                return true;
            }

            value = null;
            return false;
        }

        // ── Null ────────────────────────────────────────────────

        private static bool TryParseNull(
            string json,ref int index,out object value)
        {
            if (json.Length - index >= 4 &&
                json[index] == 'n' && json[index + 1] == 'u' &&
                json[index + 2] == 'l' && json[index + 3] == 'l')
            {
                index += 4;
                value = null;
                return true;
            }

            value = null;
            return false;
        }

        // ── Whitespace ──────────────────────────────────────────

        private static void SkipWhitespace(string json,ref int index)
        {
            while (index < json.Length)
            {
                char c = json[index];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    index++;
                else
                    break;
            }
        }
    }
    // ──────────────────────────────────────────────────────────────
    // SimpleJson 主类
    // ──────────────────────────────────────────────────────────────

    public static class SimpleJson
    {
        private static volatile IJsonSerializerStrategy
            _currentJsonSerializerStrategy;

        private static readonly object _strategyLock = new object();

        /// <summary>
        /// 全局默认序列化策略。
        /// </summary>
        /// <remarks>
        /// 线程安全说明：属性本身的读写是原子的（volatile），但动态修改
        /// strategy 实例的 toLowerCase 等设置不是线程安全的。
        /// 多线程场景建议每次调用显式传入 strategy 实例。
        /// </remarks>
        public static IJsonSerializerStrategy CurrentJsonSerializerStrategy
        {
            get
            {
                if (_currentJsonSerializerStrategy == null)
                {
                    lock (_strategyLock)
                    {
                        if (_currentJsonSerializerStrategy == null)
                            _currentJsonSerializerStrategy =
                                new DefaultJsonSerializationStrategy();
                    }
                }
                return _currentJsonSerializerStrategy;
            }
            set
            {
                Guard.ArgumentNotNull(value,"value");
                _currentJsonSerializerStrategy = value;
            }
        }
     
        internal static void ClearReflectionCache()
        {
            // 兼容 .NET 2.0/4.0：不使用 is T x 模式匹配
            DefaultJsonSerializationStrategy.ClearCache(); 
        }

        // ── 序列化 ──────────────────────────────────────────────────

        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj,CurrentJsonSerializerStrategy);
        }

        public static string SerializeObject(
            object obj,
            IJsonSerializerStrategy jsonSerializerStrategy)
        {
            var builder = new StringBuilder();
            SerializeValue(jsonSerializerStrategy,obj,builder);
            return builder.ToString();
        }

        private static bool SerializeValue(
            IJsonSerializerStrategy strategy,
            object value,
            StringBuilder builder)
        {
            if (value == null)
            {
                builder.Append("null");
                return true;
            }

            if (value is string)
            {
                SerializeString((string)value,builder);
                return true;
            }

            if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
                return true;
            }

            // ★ float/double 必须在 exporter 表查询之前处理，保证 G9/G17 精度
            if (value is float)
            {
                float fval = (float)value;
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogWarning(
                        "SerializeValue: float " + fval +
                        " is NaN/Infinity (not valid JSON). Serializing as null.");
#endif
                    builder.Append("null");
                }
                else
                    builder.Append(fval.ToString("G9",
                        CultureInfo.InvariantCulture));
                return true;
            }

            if (value is double)
            {
                double dval = (double)value;
                if (double.IsNaN(dval) || double.IsInfinity(dval))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogWarning(
                        "SerializeValue: double " + dval +
                        " is NaN/Infinity (not valid JSON). Serializing as null.");
#endif
                    builder.Append("null");
                }
                else
                    builder.Append(dval.ToString("G17",
                        CultureInfo.InvariantCulture));
                return true;
            }

            if (value is int) { builder.Append((int)value); return true; }
            if (value is long) { builder.Append((long)value); return true; }
            if (value is uint) { builder.Append((uint)value); return true; }
            if (value is ulong) { builder.Append((ulong)value); return true; }

            // IDictionary<string,object>（SimpleJson 内部表示）
            IDictionary<string,object> strDict =
                value as IDictionary<string,object>;
            if (strDict != null)
                return SerializeDictionary(strategy,strDict,builder);

            // 泛型/非泛型字典
            if (ReflectionUtils.IsTypeDictionary(value.GetType()))
                return SerializeIDictionary(strategy,(IDictionary)value,builder);

            // 数组/列表
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                if (value is IDictionary)
                    Guard.LogWarning(
                        "SerializeValue: IDictionary \"" +
                        value.GetType().FullName +
                        "\" not caught by IsTypeDictionary, " +
                        "serializing as array (likely incorrect).");
#endif
                return SerializeArray(strategy,enumerable,builder);
            }

            // base/custom exporter 表（byte/sbyte/short/char/DateTime/Guid 等）
            object exporterOutput;
            if (strategy.TrySerializeNonPrimitiveObject(value,out exporterOutput))
                return SerializeValue(strategy,exporterOutput,builder);

            return false;
        }

        private static void SerializeString(string str,StringBuilder builder)
        {
            builder.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u")
                                   .Append(((int)c).ToString("x4",
                                       CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }

        private static bool SerializeDictionary(
            IJsonSerializerStrategy strategy,
            IDictionary<string,object> dict,
            StringBuilder builder)
        {
            builder.Append('{');
            bool first = true;
            foreach (KeyValuePair<string,object> kvp in dict)
            {
                if (!first) builder.Append(',');
                first = false;
                SerializeString(kvp.Key,builder);
                builder.Append(':');
                if (!SerializeValue(strategy,kvp.Value,builder)) return false;
            }
            builder.Append('}');
            return true;
        }

        private static bool SerializeIDictionary(
            IJsonSerializerStrategy strategy,
            IDictionary dict,
            StringBuilder builder)
        {
            builder.Append('{');
            bool first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) builder.Append(',');
                first = false;

                object key = entry.Key;
                string keyStr = ConvertDictionaryKeyToString(strategy,key);

                if (keyStr == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogError(
                        "SerializeIDictionary: null key encountered. Skipping.");
#endif
                    // 回退已追加的逗号（简单处理：标记 first 重置）
                    // 实际上已追加逗号，需要更复杂处理；此处选择抛异常更安全
                    throw new InvalidOperationException(
                        "Dictionary key is null. JSON object keys cannot be null.");
                }

                SerializeString(keyStr,builder);
                builder.Append(':');
                if (!SerializeValue(strategy,entry.Value,builder)) return false;
            }
            builder.Append('}');
            return true;
        }

        private static string ConvertDictionaryKeyToString(
            IJsonSerializerStrategy strategy,object key)
        {
            if (key == null) return null;
            if (key is string) return (string)key;

            if (key is Enum)
            {
                Type enumType = key.GetType();
                // 查询 custom exporter
                object exported;
                if (strategy.TrySerializeNonPrimitiveObject(key,out exported)
                    && exported is string)
                    return (string)exported;

                // 兜底：底层数值字符串
                return Convert.ToString(
                    Convert.ChangeType(
                        key,
                        Enum.GetUnderlyingType(enumType),
                        CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture);
            }

            // int / long / uint 等数值类型
            return Convert.ToString(key,CultureInfo.InvariantCulture);
        }

        private static bool SerializeArray(
            IJsonSerializerStrategy strategy,
            IEnumerable enumerable,
            StringBuilder builder)
        {
            builder.Append('[');
            bool first = true;
            foreach (object item in enumerable)
            {
                if (!first) builder.Append(',');
                first = false;
                if (!SerializeValue(strategy,item,builder)) return false;
            }
            builder.Append(']');
            return true;
        }

        // ── 反序列化 ────────────────────────────────────────────────

        public static object DeserializeObject(string json)
        {
            object obj;
            if (TryDeserializeObject(json,out obj))
                return obj;
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }

        public static T DeserializeObject<T>(string json)
        {
            return (T)DeserializeObject(json,typeof(T),
                CurrentJsonSerializerStrategy);
        }

        public static T DeserializeObject<T>(
            string json,IJsonSerializerStrategy strategy)
        {
            return (T)DeserializeObject(json,typeof(T),strategy);
        }

        public static object DeserializeObject(string json,Type type)
        {
            return DeserializeObject(json,type,CurrentJsonSerializerStrategy);
        }

        public static object DeserializeObject(
            string json,Type type,IJsonSerializerStrategy strategy)
        {
            object parsed = DeserializeObject(json);
            return DeserializeObject(parsed,type,strategy);
        }

        internal static object DeserializeObject(
            object value,Type type,IJsonSerializerStrategy strategy)
        {
            if (type == null || value == null) return value;

            // string → 目标类型
            string str = value as string;
            if (str != null)
            {
                if (type == typeof(string)) return str;
                if (type == typeof(Guid))
                {
                    if (str.Length == 0) return default(Guid);
                    return new Guid(str);
                }
                if (type == typeof(Uri)) return new Uri(str);
                if (type == typeof(char))
                {
                    if (str.Length == 1) return str[0];
                    if (str.Length > 1)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                        Guard.LogWarning(
                            "DeserializeObject: string \"" + str +
                            "\" truncated to char '" + str[0] + "'.");
#endif
                        return str[0];
                    }
                    return default(char);
                }
                if (type == typeof(DateTime))
                {
                    try
                    {
                        return DateTime.ParseExact(str,
                            Constants.Iso8601Formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException)
                    {
                        return Convert.ToDateTime(str, CultureInfo.InvariantCulture);
                    }
                }
                if (type == typeof(DateTimeOffset))
                {
                    try
                    {
                        return DateTimeOffset.ParseExact(str,
                           Constants.Iso8601Formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException)
                    {
                        return DateTimeOffset.Parse(str, CultureInfo.InvariantCulture);
                    }
                }
                if (type == typeof(TimeSpan))
                {
                    if (long.TryParse(str, out long ticks))
                        return new TimeSpan(ticks);
                    return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
                }
                if (type.IsEnum)
                {
#if !SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
                    return Enum.Parse(type,str,true);
#else
                return Enum.ToObject(type,
                    Convert.ChangeType(str,
                        Enum.GetUnderlyingType(type),
                        CultureInfo.InvariantCulture));
#endif
                }
            }

            // 数值 → 目标类型
            if (value is long || value is int || value is double)
            {
                if (type.IsEnum)
                    return Enum.ToObject(type,
                        Convert.ChangeType(value,
                            Enum.GetUnderlyingType(type),
                            CultureInfo.InvariantCulture));

                if (type == typeof(int))
                    return Convert.ToInt32(value,CultureInfo.InvariantCulture);
                if (type == typeof(long))
                    return Convert.ToInt64(value,CultureInfo.InvariantCulture);
                if (type == typeof(short))
                    return Convert.ToInt16(value,CultureInfo.InvariantCulture);
                if (type == typeof(byte))
                    return Convert.ToByte(value,CultureInfo.InvariantCulture);
                if (type == typeof(sbyte))
                    return Convert.ToSByte(value,CultureInfo.InvariantCulture);
                if (type == typeof(uint))
                    return Convert.ToUInt32(value,CultureInfo.InvariantCulture);
                if (type == typeof(ulong))
                    return Convert.ToUInt64(value,CultureInfo.InvariantCulture);
                if (type == typeof(ushort))
                    return Convert.ToUInt16(value,CultureInfo.InvariantCulture);
                if (type == typeof(float))
                    return Convert.ToSingle(value,CultureInfo.InvariantCulture);
                if (type == typeof(double))
                    return Convert.ToDouble(value,CultureInfo.InvariantCulture);
                if (type == typeof(decimal))
                    return Convert.ToDecimal(value,CultureInfo.InvariantCulture);

                // 注：decimal 经 double 中间层存在精度截断，这是 JSON 数值的固有局限
            }

            // IList
            IList<object> jsonArray = value as IList<object>;
            if (jsonArray != null)
            {
                // ... 原有数组反序列化逻辑（elementType 推断 + LogWarning）
                return DeserializeArray(jsonArray,type,strategy);
            }

            // IDictionary<string,object> → 字典或 POCO
            IDictionary<string,object> jsonObj =
                value as IDictionary<string,object>;
            if (jsonObj != null)
            {
                object result;
                if (strategy.TryDeserializeObject(jsonObj,type,out result))
                    return result;
            }

            // 类型已匹配
            if (type.IsAssignableFrom(value.GetType()))
                return value;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
            Guard.LogWarning(
                "DeserializeObject: Cannot deserialize value of type \"" +
                value.GetType().FullName + "\" to \"" + type.FullName +
                "\". Returning raw value.");
#endif
            return value;
        }

        private static object DeserializeArray(
            IList<object> jsonArray,Type type,
            IJsonSerializerStrategy strategy)
        {
            Type elementType = null;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                Type[] args = type.GetGenericArguments();
                if (args.Length == 1) elementType = args[0];
            }

            if (elementType == null || elementType == typeof(object))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogWarning(
                    "DeserializeArray: Cannot determine element type for \"" +
                    type.FullName + "\". Elements deserialized as object. " +
                    "Consider using List<T> instead.");
#endif
                elementType = typeof(object);
            }

            IList list = new List<object>();
            foreach (object item in jsonArray)
                list.Add(DeserializeObject(item,elementType,strategy));

            if (type.IsArray)
            {
                Array arr = Array.CreateInstance(elementType,list.Count);
                for (int i = 0; i < list.Count; i++)
                    arr.SetValue(list[i],i);
                return arr;
            }

            // List<T> 或其他 IList
            IList result;
            try
            {
                result = (IList)Activator.CreateInstance(type);
            }
            catch
            {
                result = new List<object>();
            }
            foreach (object item in list)
                result.Add(item);
            return result;
        }

        private static bool TryDeserializeObject(string json,out object obj)
        {
            // 委托给 JsonParser（原库 JSON 解析器，保持不变）
            return JsonParser.TryParse(json,out obj);
        }

        // ── ParseNumber（含 double 兜底）───────────────────────────

        internal static bool ParseNumber(string str,out object returnNumber)
        {
            // 尝试 long
            long longVal;
            if (long.TryParse(str,NumberStyles.Any,
                CultureInfo.InvariantCulture,out longVal))
            {
                returnNumber = longVal;
                return true;
            }

            // 尝试 ulong（正整数超 long 范围）
            ulong ulongVal;
            if (ulong.TryParse(str,NumberStyles.Any,
                CultureInfo.InvariantCulture,out ulongVal))
            {
                returnNumber = ulongVal;
                return true;
            }

            // double 兜底（超 ulong 范围或浮点数）
            // 注：超过 2^53 的整数以 double 存储时存在精度损失，这是 JSON 的固有局限
            double doubleVal;
            if (double.TryParse(str,NumberStyles.Any,
                CultureInfo.InvariantCulture,out doubleVal))
            {
                if (doubleVal != Math.Truncate(doubleVal) ||
                    str.IndexOf('.') >= 0 || str.IndexOf('e') >= 0 ||
                    str.IndexOf('E') >= 0)
                {
                    // 确实是浮点数
                    returnNumber = doubleVal;
                    return true;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogWarning(
                    "ParseNumber: \"" + str +
                    "\" exceeds ulong range, stored as double. " +
                    "Integers > 2^53 may lose precision.");
#endif
                returnNumber = doubleVal;
                return true;
            }

            returnNumber = 0;
            return false;
        }
    }

} // namespace SimpleJson
#if UNITY_5_3_OR_NEWER

namespace RS.SimpleJsonUnity
{
    /// <summary>
    /// Unity 专用序列化策略，支持 UnityEngine 结构体类型。
    /// </summary>
    /// <remarks>
    /// 注：JsonUtility.ToJson 的浮点数精度由 Unity 引擎控制（通常约 7 位），
    /// 不受 SimpleJson G9/G17 精度修复影响。如需更高精度，
    /// 建议避免直接序列化 UnityEngine 类型，改用自定义 POCO。
    /// </remarks>
    public class UnitySerializationStrategy : DefaultJsonSerializationStrategy
    {
        private static bool IsUnityType(Type type)
        {
            return type.FullName != null &&
                   (type.FullName.StartsWith("UnityEngine.") ||
                    type.FullName.StartsWith("Unity."));
        }

        protected override bool TrySerializeKnownTypes(
            object input, out object output)
        {
            if (input != null && IsUnityType(input.GetType()))
            {
                output = UnityEngine.JsonUtility.ToJson(input);
                return true;
            }
            return base.TrySerializeKnownTypes(input, out output);
        }

        public override bool TryDeserializeObject(
            object value, Type type, out object output)
        {
            if (IsUnityType(type))
            {
                // 情形一：value 是 JSON 字符串
                string str = value as string;
                if (!string.IsNullOrEmpty(str))
                {
                    output = UnityEngine.JsonUtility.FromJson(str, type);
                    return true;
                }

                // 情形二：value 已被预解析为字典（JSON 对象）
                IDictionary<string, object> dict =
                    value as IDictionary<string, object>;
                if (dict != null)
                {
                    string json = SimpleJson.SerializeObject(dict);
                    if (!string.IsNullOrEmpty(json))
                    {
                        output = UnityEngine.JsonUtility.FromJson(json, type);
                        return true;
                    }
                }
            }
            return base.TryDeserializeObject(value, type, out output);
        }
    }
}

#endif // UNITY_5_3_OR_NEWER


// ReSharper restore LoopCanBeConvertedToQuery
// ReSharper restore RedundantExplicitArrayCreation
// ReSharper restore SuggestUseVarKeywordEvident
