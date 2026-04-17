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
//// <website>https://github.com/facebook-csharp-sdk/simple-json</website>
/// RS.SimpleJson-Unity is a fork of the original SimpleJson library, with modifications to support Unity and additional features.
/// <Author>andyhebear</Author>
/// <website>https://github.com/RS-Unity3D/RS.SimpleJson-Unity</website>
////-----------------------------------------------------------------------
////sgd:2026.4.30 Add Circular Reference Detection support
////sgd:2026.4.13 Improve code quality
////sgd:2026.4.2 be compatible with simplejson v2.0.0 as much as possible
////sgd:2026.3.20 refactoring
////sgd:2026.2.20 support aot compiler
////sgd: 2025.12.15 support unity 3d
////sgd: 2025.11.10 support property To lower case
////sgd: 2025.10.1 support string key dictionary
//// VERSION: 2.2.0.0

////NOTE:need AOT support,need #define SIMPLE_JSON_AOT
//#define SIMPLE_JSON_AOT

////NOTE: uncomment the following line to make SimpleJson class internal.
////#define SIMPLE_JSON_INTERNAL

//// NOTE: uncomment the following line to make JsonArray and JsonObject class internal.
//#define SIMPLE_JSON_OBJARRAYINTERNAL

// NOTE: uncomment the following line to enable dynamic support.
//#define SIMPLE_JSON_DYNAMIC

////NOTE: uncomment the following line to make ReflectionUtils class public.
//#define SIMPLE_JSON_REFLECTION_UTILS_PUBLIC

//// NOTE: uncomment the following line to enable DataContract support.
#define SIMPLE_JSON_DATACONTRACT

//// NOTE: uncomment the following line to enable IReadOnlyCollection<T> and IReadOnlyList<T> support.
//#define SIMPLE_JSON_READONLY_COLLECTIONS

//// NOTE: uncomment the following line if you are compiling under Window Metro style application/library.
//// usually already defined in properties
////#define NETFX_CORE;

////NOTE:If you are targetting WinStore, WP8 and NET4.5+ PCL make sure to #define SIMPLE_JSON_TYPEINFO;
////#define SIMPLE_JSON_TYPEINFO;
//// original json parsing code from http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html

//NOTE:SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
//#define SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE

//NOTE:#define SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE, Ignore the case of property/field names during deserialization
//     Define to make the default serialization strategy ignore
//     property/field name casing during deserialization.
//     Serialization always preserves original casing.
//     Equivalent to setting DefaultJsonSerializationStrategy.ignoreLowerCaseForDeserialization = true.
//
#define SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE

#if NET20
#undef SIMPLE_JSON_DATACONTRACT
#endif

#if NET20 || NET35 || NET40
#undef SIMPLE_JSON_READONLY_COLLECTIONS
#undef SIMPLE_JSON_AOT
#endif
//NOTE:.NET Framework not support AOT
#if NET45 || NET46 || NET46 || NET47 || NET48
#undef SIMPLE_JSON_AOT
#endif
//#if NETFX_CORE
//#define SIMPLE_JSON_TYPEINFO
//#endif

//#define SIMPLE_JSON_UNITY
//#define SIMPLE_JSON_WEBGL
//NOTE:Unity support
#if SIMPLE_JSON_UNITY
using UnityEngine;
#endif


// NET20
//     Automatically defined by .NET 2.0 target framework.
//     Disables ReaderWriterLockSlim (unavailable in .NET 2.0).
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

#if SIMPLE_JSON_UNITY
using UnityEngine;
 //#endif 
namespace UnityEngine { 

}
#endif
namespace RS.SimpleJsonUnity
{
    #region .NET 2.0
#if NET20
    // All these delegate are built-in .NET 3.5
    // Comment/Remove them when compiling to .NET 3.5 to avoid ambiguity.

    public delegate void Action();
    //public delegate void Action<T>(T arg);
    public delegate void Action<T1, T2>(T1 arg1,T2 arg2);
    public delegate void Action<T1, T2, T3>(T1 arg1,T2 arg2,T3 arg3);
    //public delegate void Action<T1, T2, T3, T4>(T1 arg1,T2 arg2,T3 arg3,T4 arg4);

    public delegate TResult Func<TResult>();
    public delegate TResult Func<T, TResult>(T arg);
    //public delegate TResult Func<T1, T2, TResult>(T1 arg1,T2 arg2);
    //public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1,T2 arg2,T3 arg3);
    //public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1,T2 arg2,T3 arg3,T4 arg4);
#elif NET35

#endif
    #endregion

    // ──────────────────────────────────────────────────────────────
    // Attributes
    // ──────────────────────────────────────────────────────────────
    #region Attributes
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
        public string[] Aliases { get; private set; }
        public bool AcceptOriginalName { get; private set; }

        public JsonAliasAttribute(params string[] aliases)
        {
            if (aliases == null) throw new ArgumentNullException("aliases");
            if (aliases.Length == 0) throw new ArgumentException("At least one alias is required.","aliases");
            Aliases = aliases;
            AcceptOriginalName = true;
        }

        public JsonAliasAttribute(bool acceptOriginalName,params string[] aliases)
        {
            if (aliases == null) throw new ArgumentNullException("aliases");
            if (aliases.Length == 0) throw new ArgumentException("At least one alias is required.","aliases");
            Aliases = aliases;
            AcceptOriginalName = acceptOriginalName;
        }

        [Obsolete("Use Aliases property instead. This property returns the first alias for backward compatibility.")]
        public string Alias
        {
            get { return Aliases != null && Aliases.Length > 0 ? Aliases[0] : null; }
        }
    }
    #endregion

    static class Constants
    {
        /// <summary>
        /// 日期时间的 ISO 8601 格式字符串数组，包含多种常见变体以提高兼容性。
        /// </summary>
        internal static readonly string[] Iso8601Format = new string[]
                  {
                     @"yyyy-MM-dd\THH:mm:ss.FFFFFFF\Z",
                     @"yyyy-MM-dd\THH:mm:ss.FFFFFFFK",
                     @"yyyy-MM-dd\THH:mm:ss\Z",
                     @"yyyy-MM-dd\THH:mm:ssK"
                 };
    }

    // ──────────────────────────────────────────────────────────────
    // Guard
    // ──────────────────────────────────────────────────────────────
    #region Guard
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
            Console.WriteLine("[SimpleJson] " + message);
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
            Console.WriteLine("[SimpleJson] ERROR: " + message);
#endif
        }
    }
    #endregion
    // ──────────────────────────────────────────────────────────────
    // TypeCacheKey  （缓存复合 key，区分 ignoreLowerCaseDeserialization 和 useJsonAlias 状态）
    // ──────────────────────────────────────────────────────────────
    #region TypeCacheKey
    public struct TypeCacheKey : IEquatable<TypeCacheKey>
    {
        public readonly Type Type;
        public readonly bool IgnoreLowerCase;
        public readonly bool UseJsonAlias;

        public TypeCacheKey(Type type,bool ignoreLowerCase)
        {
            Type = type;
            IgnoreLowerCase = ignoreLowerCase;
            UseJsonAlias = false;
        }

        public TypeCacheKey(Type type,bool ignoreLowerCase,bool useJsonAlias)
        {
            Type = type;
            IgnoreLowerCase = ignoreLowerCase;
            UseJsonAlias = useJsonAlias;
        }

        public bool Equals(TypeCacheKey other)
        {
            return Type == other.Type
                && IgnoreLowerCase == other.IgnoreLowerCase
                && UseJsonAlias == other.UseJsonAlias;
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
                h ^= IgnoreLowerCase.GetHashCode();
                h = (h * 397) ^ UseJsonAlias.GetHashCode();
                return h;
            }
        }
    }
    #endregion

#if NET20

    /// <summary>
    /// .NET 2.0 兼容的简易 HashSet<T>（核心功能：唯一元素、添加/移除/包含判断、遍历）
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class HashSet<T> : IEnumerable<T>
    {
        // 底层存储：利用 Dictionary 的键唯一性
        private readonly Dictionary<T,object> _dictionary;
        // 占位值（复用一个对象，减少内存分配）
        private static readonly object _placeholder = new object();

        /// <summary>
        /// 初始化空的 HashSet
        /// </summary>
        public HashSet()
        {
            _dictionary = new Dictionary<T,object>();
        }

        /// <summary>
        /// 初始化并添加初始集合
        /// </summary>
        /// <param name="collection">初始元素集合</param>
        public HashSet(IEnumerable<T> collection)
        {
            _dictionary = new Dictionary<T,object>();
            foreach (T item in collection)
            {
                Add(item);
            }
        }

        /// <summary>
        /// 获取集合中元素的数量
        /// </summary>
        public int Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// 添加元素（符合官方设计：新增成功返回 true，已存在返回 false）
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>添加成功返回 true，元素已存在返回 false</returns>
        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item))
            {
                return false; // 元素已存在，添加失败
            }
            _dictionary.Add(item,_placeholder);
            return true; // 元素新增成功
        }

        /// <summary>
        /// 移除元素（不存在则忽略）
        /// </summary>
        /// <param name="item">要移除的元素</param>
        public void Remove(T item)
        {
            _dictionary.Remove(item);
        }

        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <param name="item">要检查的元素</param>
        /// <returns>存在返回 true，否则 false</returns>
        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        /// <summary>
        /// 清空所有元素
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// 实现枚举器，支持 foreach 遍历
        /// </summary>
        /// <returns>元素枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            // 遍历 Dictionary 的键（即 HashSet 的元素）
            foreach (T key in _dictionary.Keys)
            {
                yield return key;
            }
        }

        /// <summary>
        /// 非泛型枚举器（IEnumerable 接口实现）
        /// </summary>
        /// <returns>非泛型枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
#endif
    // ──────────────────────────────────────────────────────────────
    // ThreadSafeDictionary
    // ──────────────────────────────────────────────────────────────
    #region ThreadSafeDictionary
    public sealed class ThreadSafeDictionary<TKey, TValue>
        : IDictionary<TKey,TValue>
    {
        private readonly Dictionary<TKey,TValue> _dict =
            new Dictionary<TKey,TValue>();

#if SIMPLE_JSON_WEBGL
        // WebGL 单线程：完全无锁，零开销
        public TValue this[TKey key]
        {
            get { return _dict[key]; }
            set { _dict[key] = value; }
        }
        public bool TryGetValue(TKey key,out TValue value)
        {
            return _dict.TryGetValue(key,out value);
        }
        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }
        public void Add(TKey key,TValue value)
        {
            _dict[key] = value;
        }
        public bool Remove(TKey key)
        {
            return _dict.Remove(key);
        }
        public void Clear()
        {
            _dict.Clear();
        }
        public int Count
        {
            get { return _dict.Count; }
        }
        public ICollection<TKey> Keys
        {
            get { return new List<TKey>(_dict.Keys); }
        }
        public ICollection<TValue> Values
        {
            get { return new List<TValue>(_dict.Values); }
        }
        public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator()
        {
            return new List<KeyValuePair<TKey,TValue>>(_dict).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

#elif NET20 || SIMPLE_JSON_UNITY
        private readonly object _lock = new object();

        public TValue this[TKey key]
        {
            get { lock (_lock) { return _dict[key]; } }
            set { lock (_lock) { _dict[key] = value; } }
        }
        public bool TryGetValue(TKey key,out TValue value)
        {
            lock (_lock) { return _dict.TryGetValue(key,out value); }
        }
        public bool ContainsKey(TKey key)
        {
            lock (_lock) { return _dict.ContainsKey(key); }
        }
        public void Add(TKey key,TValue value)
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
        public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey,TValue>> snapshot;
            lock (_lock)
            {
                snapshot = new List<KeyValuePair<TKey,TValue>>(_dict);
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
    #endregion
    // ──────────────────────────────────────────────────────────────
    // ReflectionUtils
    // ──────────────────────────────────────────────────────────────
    #region ReflectionUtils
    [GeneratedCode("reflection-utils","1.0.0")]
#if SIMPLE_JSON_REFLECTION_UTILS_PUBLIC
    public
#else
    public
#endif
    static class ReflectionUtils
    {


        public const BindingFlags PUBLIC_INSTANCE = BindingFlags.Public | BindingFlags.Instance;
        public const BindingFlags NONPUBLIC_INSTANCE = BindingFlags.NonPublic | BindingFlags.Instance;

        // ── Attribute 辅助 ──────────────────────────────────────────

        public static bool HasAttribute<T>(MemberInfo member) where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T),true).Length > 0;
        }

        public static T GetAttribute<T>(MemberInfo member) where T : Attribute
        {
            object[] attrs = member.GetCustomAttributes(typeof(T),true);
            return attrs.Length > 0 ? (T)attrs[0] : null;
        }

        public static string GetFirstAlias(JsonAliasAttribute aliasAttr)
        {
            if (aliasAttr != null && aliasAttr.Aliases != null && aliasAttr.Aliases.Length > 0)
                return aliasAttr.Aliases[0];
            return null;
        }
        public static Attribute GetAttribute(MemberInfo info,Type type)
        {
#if SIMPLE_JSON_TYPEINFO
            if (info == null || type == null || !info.IsDefined(type))
                return null;
            return info.GetCustomAttribute(type);
#else
            if (info == null || type == null || !Attribute.IsDefined(info,type))
                return null;
            return Attribute.GetCustomAttribute(info,type);
#endif
        }

        public static Attribute GetAttribute(Type objectType,Type attributeType)
        {
#if SIMPLE_JSON_TYPEINFO
            if (objectType == null || attributeType == null || !objectType.GetTypeInfo().IsDefined(attributeType))
                return null;
            return objectType.GetTypeInfo().GetCustomAttribute(attributeType);
#else
            if (objectType == null || attributeType == null || !Attribute.IsDefined(objectType,attributeType))
                return null;
            return Attribute.GetCustomAttribute(objectType,attributeType);
#endif
        }
        // ── SIMPLE_JSON_TYPEINFO 支持 ─────────────────────────────

#if SIMPLE_JSON_TYPEINFO
        public static System.Reflection.TypeInfo GetTypeInfo(Type type)
        {
            return type.GetTypeInfo();
        }
#else
        public static Type GetTypeInfo(Type type)
        {
            return type;
        }
#endif

        public static bool IsTypeGeneric(Type type)
        {
            return GetTypeInfo(type).IsGenericType;
        }

        public static bool IsAssignableFrom(Type type1,Type type2)
        {
            return GetTypeInfo(type1).IsAssignableFrom(GetTypeInfo(type2));
        }

        public static Type[] GetGenericTypeArguments(Type type)
        {
#if SIMPLE_JSON_TYPEINFO
            return type.GetTypeInfo().GenericTypeArguments;
#else
            return type.GetGenericArguments();
#endif
        }

        public static IEnumerable<Type> GetImplementedInterfaces(Type type)
        {
#if SIMPLE_JSON_TYPEINFO
            return type.GetTypeInfo().ImplementedInterfaces;
#else
            return type.GetInterfaces();
#endif
        }



        // ── 类型判断 ─────────────────────────────────────────────

        public static bool IsTypeGenericeCollectionInterface(Type type)
        {
            if (!IsTypeGeneric(type))
                return false;

            Type genericDefinition = type.GetGenericTypeDefinition();

            return (genericDefinition == typeof(IList<>)
                || genericDefinition == typeof(ICollection<>)
                || genericDefinition == typeof(IEnumerable<>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                || genericDefinition == typeof(IReadOnlyCollection<>)
                || genericDefinition == typeof(IReadOnlyList<>)
#endif
                );
        }

        public static bool IsTypeDictionary(Type type)
        {
            // 非泛型 IDictionary（Hashtable、SortedList 等）
#if SIMPLE_JSON_TYPEINFO
                if (typeof(IDictionary<,>).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    return true;
#else
            if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
                return true;
#endif

            // 泛型 IDictionary<,>（Dictionary<K,V>、SortedDictionary<K,V> 等）
            foreach (Type iface in GetImplementedInterfaces(type))
            {
                if (iface.IsGenericType)
                {
                    Type genericDef = iface.GetGenericTypeDefinition();
                    if (genericDef == typeof(IDictionary<,>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                        || genericDef == typeof(IReadOnlyDictionary<,>)
#endif
                    )
                        return true;
                }
            }
            return false;
        }

        public static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
            //return GetTypeInfo(type).IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static object ToNullableType(object obj,Type nullableType)
        {
            return obj == null ? null : Convert.ChangeType(obj,Nullable.GetUnderlyingType(nullableType),CultureInfo.InvariantCulture);
        }
        public static Type GetGenericListElementType(Type type)
        {
            foreach (Type implementedInterface in GetImplementedInterfaces(type))
            {
                if (IsTypeGeneric(implementedInterface) &&
                    implementedInterface.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return GetGenericTypeArguments(implementedInterface)[0];
                }
            }
            return GetGenericTypeArguments(type)[0];
        }

#if SIMPLE_JSON_TYPEINFO
        public static IEnumerable<PropertyInfo> GetProperties(Type type,BindingFlags bindingAttr)
        {
            var result = new List<PropertyInfo>();
            foreach (PropertyInfo p in type.GetRuntimeProperties())
            {
                MethodInfo m = p.GetMethod;
                if (m == null) m = p.SetMethod;
                if (m == null) continue;
                bool match = true;
                if ((bindingAttr & BindingFlags.Public) != 0 && !m.IsPublic) match = false;
                if ((bindingAttr & BindingFlags.NonPublic) != 0 && m.IsPublic) match = false;
                if ((bindingAttr & BindingFlags.Static) != 0 && !m.IsStatic) match = false;
                if ((bindingAttr & BindingFlags.Instance) != 0 && m.IsStatic) match = false;
                if (match) result.Add(p);
            }
            return result;
        }

        public static IEnumerable<FieldInfo> GetFields(Type type,BindingFlags bindingAttr)
        {
            var result = new List<FieldInfo>();
            foreach (FieldInfo f in type.GetRuntimeFields())
            {
                bool match = true;
                if ((bindingAttr & BindingFlags.Public) != 0 && !f.IsPublic) match = false;
                if ((bindingAttr & BindingFlags.NonPublic) != 0 && f.IsPublic) match = false;
                if ((bindingAttr & BindingFlags.Static) != 0 && !f.IsStatic) match = false;
                if ((bindingAttr & BindingFlags.Instance) != 0 && f.IsStatic) match = false;
                if (match) result.Add(f);
            }
            return result;
        }

        public static MethodInfo GetGetterMethod(PropertyInfo property)
        {
            return property.GetMethod;
        }

        public static MethodInfo GetSetterMethod(PropertyInfo property)
        {
            return property.SetMethod;
        }

        public static IEnumerable<ConstructorInfo> GetConstructors(Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors;
        }
#else
        public static IEnumerable<PropertyInfo> GetProperties(Type type,BindingFlags bindingAttr)
        {
            return type.GetProperties(bindingAttr);
        }

        public static IEnumerable<FieldInfo> GetFields(Type type,BindingFlags bindingAttr)
        {
            return type.GetFields(bindingAttr);
        }

        public static MethodInfo GetGetterMethod(PropertyInfo property)
        {
            return property.GetGetMethod(true);
        }

        public static MethodInfo GetSetterMethod(PropertyInfo property)
        {
            return property.GetSetMethod(true);
        }

        public static IEnumerable<ConstructorInfo> GetConstructors(Type type)
        {
            return type.GetConstructors();
        }
#endif

        public static bool IsValueType(Type type)
        {
            return GetTypeInfo(type).IsValueType;
        }

        //-------------------------------------
        public delegate object GetDelegate(object source);
        public delegate void SetDelegate(object source,object value);
        public static ConstructorInfo GetConstructorInfo(Type type,params Type[] argsType)
        {
            IEnumerable<ConstructorInfo> constructorInfos = GetConstructors(type);
            int i;
            bool matches;
            foreach (ConstructorInfo constructorInfo in constructorInfos)
            {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (argsType.Length != parameters.Length)
                    continue;

                i = 0;
                matches = true;
                foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
                {
                    if (parameterInfo.ParameterType != argsType[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return constructorInfo;
            }

            return null;
        }
        public static MethodInfo GetGetterMethodInfo(PropertyInfo propertyInfo)
        {
#if SIMPLE_JSON_TYPEINFO
                return propertyInfo.GetMethod;
#else
            return propertyInfo.GetGetMethod(true);
#endif
        }
        public static MethodInfo GetSetterMethodInfo(PropertyInfo propertyInfo)
        {
#if SIMPLE_JSON_TYPEINFO
                return propertyInfo.SetMethod;
#else
            return propertyInfo.GetSetMethod(true);
#endif
        }
        public static ConstructorDelegate GetContructor(ConstructorInfo constructorInfo)
        {
            return GetConstructorByReflection(constructorInfo);
        }
        public static ConstructorDelegate GetContructor(Type type,params Type[] argsType)
        {
            return GetConstructorByReflection(type,argsType);
        }
        public static ConstructorDelegate GetConstructorByReflection(ConstructorInfo constructorInfo)
        {
            return delegate (object[] args) { return constructorInfo.Invoke(args); };
        }

        public static ConstructorDelegate GetConstructorByReflection(Type type,params Type[] argsType)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type,argsType);
            // if it's a value type (i.e., struct), it won't have a default constructor, so use Activator instead
            return constructorInfo == null ? (type.IsValueType ? GetConstructorForValueType(type) : null) : GetConstructorByReflection(constructorInfo);
        }
        static ConstructorDelegate GetConstructorForValueType(Type type)
        {
            return delegate (object[] args) { return Activator.CreateInstance(type); };
        }
        public static GetDelegate GetGetMethod(PropertyInfo propertyInfo)
        {
            return GetGetMethodByReflection(propertyInfo);
        }
        public static GetDelegate GetGetMethod(FieldInfo fieldInfo)
        {
            return GetGetMethodByReflection(fieldInfo);
        }
        public static GetDelegate GetGetMethodByReflection(PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo = GetGetterMethodInfo(propertyInfo);
            return delegate (object source) { return methodInfo.Invoke(source,EmptyObjects); };
        }

        public static GetDelegate GetGetMethodByReflection(FieldInfo fieldInfo)
        {
            return delegate (object source) { return fieldInfo.GetValue(source); };
        }
        public static SetDelegate GetSetMethod(PropertyInfo propertyInfo)
        {
            return GetSetMethodByReflection(propertyInfo);
        }
        public static SetDelegate GetSetMethod(FieldInfo fieldInfo)
        {
            return GetSetMethodByReflection(fieldInfo);
        }
        public static SetDelegate GetSetMethodByReflection(PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo = GetSetterMethodInfo(propertyInfo);
            return delegate (object source,object value) { methodInfo.Invoke(source,new object[] { value }); };
        }

        public static SetDelegate GetSetMethodByReflection(FieldInfo fieldInfo)
        {
            return delegate (object source,object value) { fieldInfo.SetValue(source,value); };
        }

        static readonly object[] EmptyObjects = new object[] { };
        //---构造函数-----------------------------
        public delegate object ConstructorDelegate(params object[] args);
        internal delegate TValue ThreadSafeDictionaryValueFactory<TKey, TValue>(TKey key);
        // ── ConstructorCache 缓存访问 ────────────────────────────────
        static readonly Type[] EmptyTypes = new Type[0];
        static readonly Type[] ArrayConstructorParameterTypes = new Type[] { typeof(int) };

        internal static IDictionary<Type,ConstructorDelegate> ConstructorCache = new ThreadSafeReadOnlyDictionary<Type,ConstructorDelegate>(ContructorDelegateFactory);
        static ReflectionUtils.ConstructorDelegate ContructorDelegateFactory(Type key)
        {
            return ReflectionUtils.GetContructor(key,(key.IsArray || ReflectionUtils.IsAssignableFrom(typeof(IList),key)) ? ArrayConstructorParameterTypes : EmptyTypes);
        }
        internal sealed class ThreadSafeReadOnlyDictionary<TKey, TValue> : IDictionary<TKey,TValue>
        {
            private readonly object _lock = new object();
            private readonly ThreadSafeDictionaryValueFactory<TKey,TValue> _valueFactory;
            private Dictionary<TKey,TValue> _dictionary;

            public ThreadSafeReadOnlyDictionary(ThreadSafeDictionaryValueFactory<TKey,TValue> valueFactory)
            {
                _valueFactory = valueFactory;
            }

            private TValue Get(TKey key)
            {
                if (_dictionary == null)
                    return AddValue(key);
                TValue value;
                if (!_dictionary.TryGetValue(key,out value))
                    return AddValue(key);
                return value;
            }

            private TValue AddValue(TKey key)
            {
                TValue value = _valueFactory(key);
#if !SIMPLE_JSON_WEBGL
                lock (_lock)
#endif
                {
                    if (_dictionary == null)
                    {
                        _dictionary = new Dictionary<TKey,TValue>();
                        _dictionary[key] = value;
                    }
                    else
                    {
                        TValue val;
                        if (_dictionary.TryGetValue(key,out val))
                            return val;
                        Dictionary<TKey,TValue> dict = new Dictionary<TKey,TValue>(_dictionary);
                        dict[key] = value;
                        _dictionary = dict;
                    }
                }
                return value;
            }

            public void Add(TKey key,TValue value)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(TKey key)
            {
                return _dictionary.ContainsKey(key);
            }

            public ICollection<TKey> Keys
            {
                get { return _dictionary.Keys; }
            }

            public bool Remove(TKey key)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(TKey key,out TValue value)
            {
                value = this[key];
                return true;
            }

            public ICollection<TValue> Values
            {
                get { return _dictionary.Values; }
            }

            public TValue this[TKey key]
            {
                get { return Get(key); }
                set { throw new NotImplementedException(); }
            }

            public void Add(KeyValuePair<TKey,TValue> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<TKey,TValue> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<TKey,TValue>[] array,int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { return _dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(KeyValuePair<TKey,TValue> item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }
        }
        internal sealed class OmitSentinel { internal OmitSentinel() { } }
        #region 构建对象辅助
        /// <summary>
        /// [不捕获异常]AOT 安全的类型创建。优先使用已注册的工厂委托，避免 Activator.CreateInstance 在 AOT 环境下失败。
        /// </summary>
        /// <param name="type">要创建实例的类型</param>
        /// <returns>类型实例，出现错误时抛异常</returns>
        internal static object SafeCreateInstance(Type type)
        {
            Func<object> factory = SimpleJson.GetRegisteredAotFactory(type);
            if (factory != null)
                return factory();

            if (type.IsInterface)
            {
                Type concreteType = MapInterfaceToConcreteType(type);
                if (concreteType != null)
                    return SafeCreateInstance(concreteType);
            }

            if (type.IsArray || ReflectionUtils.IsAssignableFrom(typeof(IList),type))
            {
                var ctor = ReflectionUtils.ConstructorCache[type];
                if (ctor == null) throw new MissingMethodException(type.Name);
                return ctor(0);
            }
            var defaultCtor = ReflectionUtils.ConstructorCache[type];
            if (defaultCtor == null) throw new MissingMethodException(type.Name);
            return defaultCtor();
        }
        /// <summary>
        ///  [不捕获异常]根据结果创建List<>/Dictionary<,>的type 成功返回对象type， 否则返回null，出现错误时抛异常
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private static Type MapInterfaceToConcreteType(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                return null;
            if (interfaceType.IsGenericType)
            {
                Type genericDef = interfaceType.GetGenericTypeDefinition();
                Type[] genericArgs = interfaceType.GetGenericArguments();

                if (genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                    || genericDef == typeof(IReadOnlyCollection<>)
                    || genericDef == typeof(IReadOnlyList<>)
#endif
                    )
                {
                    return typeof(List<>).MakeGenericType(genericArgs);
                }

                if (genericDef == typeof(IDictionary<,>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                    || genericDef == typeof(IReadOnlyDictionary<,>)
#endif
                    )
                {
                    return typeof(Dictionary<,>).MakeGenericType(genericArgs);
                }
            }
            return null;
        }

        /// <summary>
        /// [不捕获异常]AOT 安全的 List 创建。优先使用已注册工厂委托，避免 MakeGenericType。
        /// 出现错误时抛异常。
        /// </summary>
        /// <param name="elementType">列表元素类型</param>
        /// <returns>IList 实例，出现错误时抛异常</returns>
        internal static IList SafeCreateList(Type elementType)
        {
            Type fallbackType = typeof(List<>).MakeGenericType(elementType);
            Func<object> factory = SimpleJson.GetRegisteredAotFactory(fallbackType);
            if (factory != null)
            {
                return (IList)factory();
            }
            var ctor = ReflectionUtils.ConstructorCache[fallbackType];
            if (ctor == null) throw new MissingMethodException(fallbackType.Name);
            return (IList)ctor(0);
        }
        /// <summary>
        /// [不捕获异常]创建Array对象，出现错误时抛异常
        /// </summary>
        /// <param name="arrayType"></param>
        /// <param name="elementType"></param>
        /// <param name="count"></param>
        /// <returns>Array 实例，出现错误时抛异常</returns>
        internal static Array SafeCreateArrayInstance(Type arrayType,Type elementType,int count)
        {
            var ctor = ReflectionUtils.ConstructorCache[arrayType];
            if (ctor == null) throw new MissingMethodException(arrayType.Name);
            return (Array)ctor(count);
        }
        /// <summary>
        ///[不捕获异常] 创建 IDictionary 实例，出现错误时抛异常
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="valueType"></param>
        /// <returns>IDictionary 实例，出现错误时抛异常</returns>
        internal static IDictionary SafeCreateDictionary(Type keyType,Type valueType)
        {
            Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType,valueType);
            Func<object> factory = SimpleJson.GetRegisteredAotFactory(dictType);
            if (factory != null)
            {
                return (IDictionary)factory();
            }
            var ctor = ReflectionUtils.ConstructorCache[dictType];
            if (ctor == null) throw new MissingMethodException(dictType.Name);
            return (IDictionary)ctor();
        }

        /// <summary>
        /// [不捕获异常]AOT 安全的枚举值转换。直接处理各种数值类型，避免 Convert.ChangeType 在 AOT 环境下失败。
        /// </summary>
        /// <param name="value">数值（long/int/double 等）</param>
        /// <param name="enumType">目标枚举类型</param>
        /// <returns>枚举值</returns>
        internal static object SafeEnumConversionFromNumber(object value,Type enumType)
        {
            if (value == null || !enumType.IsEnum) return value;

            //return Enum.ToObject(enumType,
            //    Convert.ChangeType(
            //        Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture)),
            //        Enum.GetUnderlyingType(enumType),
            //        CultureInfo.InvariantCulture));

            Type underlyingType = Enum.GetUnderlyingType(enumType);
            long longVal;

            if (value is long l) longVal = l;
            else if (value is int i) longVal = i;
            else if (value is short s) longVal = s;
            else if (value is byte b) longVal = b;
            else if (value is sbyte sb) longVal = sb;
            else if (value is ulong ul) return Enum.ToObject(enumType,ul);
            else if (value is uint ui) return Enum.ToObject(enumType,ui);
            else if (value is ushort us) return Enum.ToObject(enumType,us);
            else if (value is double d) longVal = (long)Math.Truncate(d);
            else if (value is float f) longVal = (long)Math.Truncate(f);
            else if (value is decimal dec) longVal = (long)Math.Truncate(dec);
            else
            {
                try { longVal = Convert.ToInt64(value,CultureInfo.InvariantCulture); }
                catch { return Enum.ToObject(enumType,value); }
            }

            if (underlyingType == typeof(int)) return Enum.ToObject(enumType,(int)longVal);
            if (underlyingType == typeof(long)) return Enum.ToObject(enumType,longVal);
            if (underlyingType == typeof(short)) return Enum.ToObject(enumType,(short)longVal);
            if (underlyingType == typeof(byte)) return Enum.ToObject(enumType,(byte)longVal);
            if (underlyingType == typeof(sbyte)) return Enum.ToObject(enumType,(sbyte)longVal);
            if (underlyingType == typeof(uint)) return Enum.ToObject(enumType,(uint)longVal);
            if (underlyingType == typeof(ulong)) return Enum.ToObject(enumType,(ulong)longVal);
            if (underlyingType == typeof(ushort)) return Enum.ToObject(enumType,(ushort)longVal);

            return Enum.ToObject(enumType,longVal);
        }
        #endregion

    }

    #endregion
    [GeneratedCode("simple-json","2.0.0")]
    /// <summary>
    /// 序列化/反序列化策略接口。
    /// 实现此接口可完全自定义 SimpleJson 的序列化行为。
    /// </summary>
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
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
        [SuppressMessage("Microsoft.Design","CA1007:UseGenericsWhereAppropriate",Justification = "Need to support .NET 2")]
        bool TrySerializeNonPrimitiveObject(object input,out object output);

        /// <summary>
        /// 尝试将已解析的 JSON 值（IDictionary / IList / 基础类型）
        /// 反序列化为目标类型的实例。
        /// </summary>
        /// <param name="value">JSON 解析器输出的原始值。</param>
        /// <param name="type">目标 .NET 类型。</param>
        /// <returns>成功则返回反序列化结果对象否则为null</returns>
        object DeserializeObject(object value,Type type);
        /// <summary>
        /// 清理序列化堆栈（用于循环引用检测）。通常在每次序列化/反序列化操作完成后调用，以避免内存泄漏。
        /// </summary>
        void ClearSerializationStack();
        /// <summary>
        /// 清空 getter/setter 缓存。当类型定义发生变化时调用。
        /// </summary>
        void ClearCache();
    }

#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    class DefaultJsonSerializationStrategy : IJsonSerializerStrategy
    {
      
        internal static readonly ReflectionUtils.OmitSentinel OmitValue = new  ReflectionUtils.OmitSentinel();

        // volatile 保证多线程可见性
        // 注：在多线程中动态切换 ignoreLowerCaseForDeserialization 不是线程安全的。
        // 建议在初始化阶段一次性设定，之后不再修改。
        // ignoreLowerCaseForDeserialization=true 时：反序列化使用大小写无关匹配（OrdinalIgnoreCase），
        // 序列化始终保留原始属性名大小写。
        public volatile bool ignoreLowerCaseForDeserialization;

        // 控制序列化时是否使用 JsonAlias 的第一个别名作为输出键名
        // 默认 false（与 Newtonsoft.Json 行为一致）：序列化使用原始属性名
        // 设为 true 时：序列化使用 Aliases[0] 作为键名（如果存在 JsonAlias）
        public volatile bool useJsonAliasForSerialization;

        #region 实例字段

        // getter 缓存：序列化键名受 useJsonAlias 影响，需要复合 key
        protected readonly ThreadSafeDictionary<TypeCacheKey,
            IDictionary<string,Func<object,object>>> m_getterCache
            = new ThreadSafeDictionary<TypeCacheKey,
                IDictionary<string,Func<object,object>>>();

        // setter 缓存：反序列化 key 受 ignoreLowerCase 影响，需要复合 key
        protected readonly ThreadSafeDictionary<TypeCacheKey,
            IDictionary<string,Action<object,object>>>
            m_setterCache
            = new ThreadSafeDictionary<TypeCacheKey,
                IDictionary<string,Action<object,object>>>();

        // 缓存构建锁
#if SIMPLE_JSON_WEBGL
        // WebGL 单线程：无需锁
#else
        protected readonly object m_getterBuildLock = new object();
        protected readonly object m_setterBuildLock = new object();
#endif

        // 序列化循环引用检测栈（实例级别，线程本地存储）
#if NET20 || NET35
        // NET20/NET35: 使用 [ThreadStatic] 模拟 ThreadLocal
        [ThreadStatic]
        private static HashSet<object> s_serializationStack;

        private HashSet<object> GetSerializationStack()
        {
            if (s_serializationStack == null)
                s_serializationStack = new HashSet<object>();
            return s_serializationStack;
        }
#else
        private readonly System.Threading.ThreadLocal<HashSet<object>> m_serializationStack
            = new System.Threading.ThreadLocal<HashSet<object>>(() => new HashSet<object>());

        private HashSet<object> GetSerializationStack()
        {
            return m_serializationStack.Value;
        }
#endif
        #endregion
        public DefaultJsonSerializationStrategy()
        {
#if SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE
            ignoreLowerCaseForDeserialization = true;
#else
            ignoreLowerCaseForDeserialization = false;
#endif
            useJsonAliasForSerialization = false;
        }

        public DefaultJsonSerializationStrategy(bool ignoreLowerCaseDeserialization,bool useJsonAliasSerialization)
        {
            this.ignoreLowerCaseForDeserialization = ignoreLowerCaseDeserialization;
            this.useJsonAliasForSerialization = useJsonAliasSerialization;
        }

        public void ClearSerializationStack()
        {
            var stack = GetSerializationStack();
            if (stack != null)
                stack.Clear();
        }

        #region 方法
        // ── 字典 key 转换 ────────────────────────────────────────────

        protected virtual object ConvertDictionaryKey(string strKey,Type keyType)
        {
            if (keyType == typeof(string) || keyType == typeof(object))
                return strKey;

            if (keyType.IsEnum)
            {
#if !SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
                try { return Enum.Parse(keyType,strKey,true); }
                catch (ArgumentException)
                {
                    if (long.TryParse(strKey,out long numVal))
                        return ReflectionUtils.SafeEnumConversionFromNumber(numVal,keyType);
                    throw;
                }
#else
            if (long.TryParse(strKey,out long numVal))
                return SafeEnumConversionFromNumber(numVal,keyType);
            object numVal2 = Convert.ChangeType(
                strKey,
                Enum.GetUnderlyingType(keyType),
                CultureInfo.InvariantCulture);
            return Enum.ToObject(keyType, numVal2);
#endif
            }

            // int / long / uint / double 等数值类型
            return Convert.ChangeType(
                strKey,keyType,CultureInfo.InvariantCulture);
        }

        public virtual void ClearCache()
        {
#if SIMPLE_JSON_WEBGL
            m_getterCache.Clear();
            m_setterCache.Clear();
#else
            lock (m_getterBuildLock)
            {
                m_getterCache.Clear();
            }
            lock (m_setterBuildLock)
            {
                m_setterCache.Clear();
            }
#endif
        }



        // ── BuildGetters ────────────────────────────────────────────

        protected virtual IDictionary<string,Func<object,object>>
            BuildGetters(Type type,bool useJsonAlias,IJsonSerializerStrategy strategy)
        {
            var getters = new Dictionary<string,Func<object,object>>();
            // seen：按成员名去重，优先最派生版本（GetProperties 返回顺序保证）
            // C# 不允许同名属性和字段，seen 跨两者去重是安全的
            var seen = new HashSet<string>();

            // public 属性
            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                if (seen.Contains(p.Name)) continue;
                if (ReflectionUtils.HasAttribute<JsonIgnoreAttribute>(p)) continue;

                seen.Add(p.Name);
                PropertyInfo captured = p;

                string key = p.Name;
                if (useJsonAlias)
                {
                    string alias = ReflectionUtils.GetFirstAlias(ReflectionUtils.GetAttribute<JsonAliasAttribute>(p));
                    if (alias != null)
                        key = alias;
                }

                getters[key] = obj => captured.GetValue(obj,null);
            }

            // public 字段
            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                if (seen.Contains(f.Name)) continue;
                if (ReflectionUtils.HasAttribute<JsonIgnoreAttribute>(f)) continue;

                seen.Add(f.Name);
                FieldInfo captured = f;

                string key = f.Name;
                if (useJsonAlias)
                {
                    JsonAliasAttribute aliasAttr = ReflectionUtils.GetAttribute<JsonAliasAttribute>(f);
                    if (aliasAttr != null && aliasAttr.Aliases != null && aliasAttr.Aliases.Length > 0)
                    {
                        key = aliasAttr.Aliases[0];
                    }
                }

                getters[key] = obj => captured.GetValue(obj);
            }

            // non-public：仅 JsonInclude，JsonIgnore 优先
            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                if (seen.Contains(p.Name)) continue;
                if (!ReflectionUtils.HasAttribute<JsonIncludeAttribute>(p)) continue;
                if (ReflectionUtils.HasAttribute<JsonIgnoreAttribute>(p)) continue;

                seen.Add(p.Name);
                PropertyInfo captured = p;

                string key = p.Name;
                if (useJsonAlias)
                {
                    JsonAliasAttribute aliasAttr = ReflectionUtils.GetAttribute<JsonAliasAttribute>(p);
                    if (aliasAttr != null && aliasAttr.Aliases != null && aliasAttr.Aliases.Length > 0)
                    {
                        key = aliasAttr.Aliases[0];
                    }
                }

                getters[key] = obj => captured.GetValue(obj,null);
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (seen.Contains(f.Name)) continue;
                if (!ReflectionUtils.HasAttribute<JsonIncludeAttribute>(f)) continue;
                if (ReflectionUtils.HasAttribute<JsonIgnoreAttribute>(f)) continue;

                seen.Add(f.Name);
                FieldInfo captured = f;

                string key = f.Name;
                if (useJsonAlias)
                {
                    JsonAliasAttribute aliasAttr = ReflectionUtils.GetAttribute<JsonAliasAttribute>(f);
                    if (aliasAttr != null && aliasAttr.Aliases != null && aliasAttr.Aliases.Length > 0)
                    {
                        key = aliasAttr.Aliases[0];
                    }
                }

                getters[key] = obj => captured.GetValue(obj);
            }

            return getters;
        }

        // ── BuildSetters ────────────────────────────────────────────

        protected virtual IDictionary<string,Action<object,object>>
            BuildSetters(Type type,bool ignoreLowerCase,IJsonSerializerStrategy strategy)
        {
            // ignoreLowerCase=true 时使用大小写无关比较器，实现反序列化忽略大小写
            var setters = ignoreLowerCase
                ? new Dictionary<string,Action<object,object>>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string,Action<object,object>>();
            var seen = new HashSet<string>();

            Action<MemberInfo,Type,Action<object,object>> register =
                (member,memberType,rawSetter) =>
                {
                    if (ReflectionUtils.HasAttribute<JsonIgnoreAttribute>(member)) return;
                    if (seen.Contains(member.Name)) return;
                    seen.Add(member.Name);

                    // 带类型转换保护的 setter
                    Type capturedType = memberType;
                    MemberInfo capturedMember = member;
                    Action<object,object> safeSetter = (obj,val) =>
                    {
                        try
                        {
                            rawSetter(obj,CoerceValue(val,capturedType,strategy));
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

                    // setter key 始终使用原始属性名；
                    // ignoreLowerCase 的效果由字典的 OrdinalIgnoreCase 比较器实现
                    string originalKey = member.Name;

                    JsonAliasAttribute aliasAttr = ReflectionUtils.GetAttribute<JsonAliasAttribute>(member);
                    if (aliasAttr != null)
                    {
                        foreach (string alias in aliasAttr.Aliases)
                        {
                            setters[alias] = safeSetter;
                        }
                        if (aliasAttr.AcceptOriginalName)
                        {
                            setters[originalKey] = safeSetter;
                        }
                    }
                    else
                    {
                        setters[originalKey] = safeSetter;
                    }
                };

            // public 属性
            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            // public 字段
            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                FieldInfo captured = f;
                register(f,f.FieldType,
                    (obj,val) => captured.SetValue(obj,val));
            }

            // non-public：仅 JsonInclude
            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                if (!ReflectionUtils.HasAttribute<JsonIncludeAttribute>(p)) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (!ReflectionUtils.HasAttribute<JsonIncludeAttribute>(f)) continue;
                FieldInfo captured = f;
                register(f,f.FieldType,
                    (obj,val) => captured.SetValue(obj,val));
            }

            return setters;
        }
        protected virtual object CoerceValue(object val,Type targetType,IJsonSerializerStrategy strategy)
        {
            if (val == null) return null;
            if (targetType.IsAssignableFrom(val.GetType())) return val;

            Type underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                targetType = underlying;

            return strategy.DeserializeObject(val,targetType);
        }
        // ── MapClrMemberNameToJsonFieldName ─────────────────────────

        protected virtual string MapClrMemberNameToJsonFieldName(string clrName)
        {
            if (clrName == null) return clrName;
            // SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE 仅影响反序列化（大小写无关匹配），
            // 序列化始终保留原始属性名
            return clrName;
        }
        #endregion

        // ── GetOrBuild 缓存访问 ─────────────────────────────────────

        protected virtual IDictionary<string,Func<object,object>>
            GetOrBuildGetters(Type type)
        {
            var cacheKey = new TypeCacheKey(type,ignoreLowerCaseForDeserialization,useJsonAliasForSerialization);
            IDictionary<string,Func<object,object>> cached;
            if (m_getterCache.TryGetValue(cacheKey,out cached)) return cached;
#if SIMPLE_JSON_WEBGL
            IDictionary<string,Func<object,object>> built;
            try { built = BuildGetters(type,useJsonAliasForSerialization,this); }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError("GetOrBuildGetters: Failed for \"" +
                    type.FullName + "\". " + ex.Message);
#endif
                return new Dictionary<string,Func<object,object>>();
            }
            m_getterCache[cacheKey] = built;
            return built;
#else
            lock (m_getterBuildLock)
            {
                if (m_getterCache.TryGetValue(cacheKey,out cached)) return cached;
                IDictionary<string,Func<object,object>> built;
                try { built = BuildGetters(type,useJsonAliasForSerialization,this); }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogError("GetOrBuildGetters: Failed for \"" +
                        type.FullName + "\". " + ex.Message);
#endif
                    return new Dictionary<string,Func<object,object>>();
                }
                m_getterCache[cacheKey] = built;
                return built;
            }
#endif
        }

        protected virtual IDictionary<string,Action<object,object>>
            GetOrBuildSetters(Type type)
        {
            var cacheKey = new TypeCacheKey(type,ignoreLowerCaseForDeserialization);
            IDictionary<string,Action<object,object>> cached;
            if (m_setterCache.TryGetValue(cacheKey,out cached)) return cached;
#if SIMPLE_JSON_WEBGL
            IDictionary<string,Action<object,object>> built;
            try { built = BuildSetters(type,ignoreLowerCaseForDeserialization,this); }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError("GetOrBuildSetters: Failed for \"" +
                    type.FullName + "\". " + ex.Message);
#endif
                return new Dictionary<string,Action<object,object>>();
            }
            m_setterCache[cacheKey] = built;
            return built;
#else
            lock (m_setterBuildLock)
            {
                if (m_setterCache.TryGetValue(cacheKey,out cached)) return cached;
                IDictionary<string,Action<object,object>> built;
                try { built = BuildSetters(type,ignoreLowerCaseForDeserialization,this); }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogError("GetOrBuildSetters: Failed for \"" +
                        type.FullName + "\". " + ex.Message);
#endif
                    return new Dictionary<string,Action<object,object>>();
                }
                m_setterCache[cacheKey] = built;
                return built;
            }
#endif
        }

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
                    Constants.Iso8601Format[0],
                    CultureInfo.InvariantCulture);

                return true;
            }
            // DateTimeOffset
            if (input is DateTimeOffset)
            {
                output = ((DateTimeOffset)input).ToUniversalTime().ToString(
                    Constants.Iso8601Format[0],
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
            if (input == null) { output = null; return false; }

            Type type = input.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                output = null;
                return false;
            }

            // 集合类型（IDictionary / IList）由 SerializeValue 主流程处理，
            // 不应被当作 POCO 枚举属性（否则 List<int> 会输出 {"capacity":4,"count":3}）
            if (input is IDictionary || input is IList)
            {
                output = null;
                return false;
            }

            var stack = GetSerializationStack();

            if (stack.Contains(input))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError(
                    "TrySerializeUnknownTypes: Circular reference detected for type \"" +
                    input.GetType().FullName + "\". Object will be serialized as null.");
#endif
                output = null;
                return true;
            }

            stack.Add(input);

            var result = new JsonObject();

            IDictionary<string,Func<object,object>> getters =
                GetOrBuildGetters(type);

            foreach (KeyValuePair<string,Func<object,object>> kvp in getters)
            {
                string jsonKey = MapClrMemberNameToJsonFieldName(kvp.Key);
                object val = kvp.Value(input);

                if (object.ReferenceEquals(val,OmitValue))
                    continue;

                if (val != null)
                {
                    object exportValue;
                    if (TrySerializeNonPrimitiveObject(val,out exportValue))
                        val = exportValue;
                }

                result[jsonKey] = val;
            }

            output = result;
            return true;
        }

        // ── TryDeserializeObject ────────────────────────────────────
        public virtual object DeserializeObject(
       object value,Type type)
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
                if (type == typeof(Uri))
                {
                    //return new Uri(str); 
                    bool isValid = Uri.IsWellFormedUriString(str,UriKind.RelativeOrAbsolute);

                    Uri result;
                    if (isValid && Uri.TryCreate(str,UriKind.RelativeOrAbsolute,out result))
                        return result;

                    return null;
                }
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
                            Constants.Iso8601Format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException)
                    {
                        return Convert.ToDateTime(str,CultureInfo.InvariantCulture);
                    }
                }
                if (type == typeof(DateTimeOffset))
                {
                    try
                    {
                        return DateTimeOffset.ParseExact(str,
                           Constants.Iso8601Format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    catch (FormatException)
                    {
                        return DateTimeOffset.Parse(str,CultureInfo.InvariantCulture);
                    }
                }
                if (type == typeof(TimeSpan))
                {
                    if (long.TryParse(str,out long ticks))
                        return new TimeSpan(ticks);
#if NET20 || NET35
                    return TimeSpan.Parse(str);
#else
                    return TimeSpan.Parse(str,CultureInfo.InvariantCulture);
#endif
                }
                if (type.IsEnum)
                {
#if !SIMPLE_JSON_NO_REFLECTION_ENUM_PARSE
                    try { return Enum.Parse(type,str,true); }
                    catch (ArgumentException)
                    {
                        if (long.TryParse(str,out long numVal))
                            return ReflectionUtils.SafeEnumConversionFromNumber(numVal,type);
                        throw;
                    }
#else
                if (long.TryParse(str,out long numVal))
                    return SafeEnumConversionFromNumber(numVal,type);
                return Enum.ToObject(type,
                    Convert.ChangeType(str,
                        Enum.GetUnderlyingType(type),
                        CultureInfo.InvariantCulture));
#endif
                }
            }

            // 数值 → 目标类型
            if (value is long || value is int || value is double
                || value is ulong || value is uint
                || value is short || value is ushort
                || value is byte || value is sbyte
                || value is decimal || value is float)
            {
                if (type.IsEnum)
                    return ReflectionUtils.SafeEnumConversionFromNumber(value,type);

                if (value is double || value is float || value is decimal)
                {
                    if (type == typeof(int))
                        return (int)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(long))
                        return (long)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(short))
                        return (short)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(byte))
                        return (byte)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(sbyte))
                        return (sbyte)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(uint))
                        return (uint)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(ulong))
                        return (ulong)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                    if (type == typeof(ushort))
                        return (ushort)Math.Truncate(Convert.ToDecimal(value,CultureInfo.InvariantCulture));
                }
                else
                {
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
                }

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
                return DeserializeArray(jsonArray,type,this);
            }

            // IDictionary<string,object> → 字典或 POCO
            IDictionary<string,object> jsonObj =
                value as IDictionary<string,object>;
            if (jsonObj != null)
            {
                DeserializeFromJsonObject(jsonObj,type,out object result);
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

        protected virtual object DeserializeArray(
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
                list.Add(strategy.DeserializeObject(item,elementType));

            if (type.IsArray)
            {
                Array arr = ReflectionUtils.SafeCreateArrayInstance(type,elementType,list.Count);// Array.CreateInstance(elementType,list.Count);
                for (int i = 0; i < list.Count; i++)
                    arr.SetValue(list[i],i);
                return arr;
            }

            // List<T> 或其他 IList
            IList result;
            try
            {
                result = (IList)ReflectionUtils.SafeCreateInstance(type);
            }
            catch (MissingMethodException)
            {
                result = ReflectionUtils.SafeCreateList(elementType);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogWarning(
                    "DeserializeArray: Failed to create instance of \"" +
                    type.FullName + "\". Falling back to List<object>. " + ex.Message);
#endif
                result = new List<object>();
            }
            foreach (object item in list)
                result.Add(item);
            return result;
        }

        protected virtual bool DeserializeFromJsonObject(
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
                        "DeserializeFromJsonObject: Non-generic dictionary \"" +
                        type.FullName + "\". Keys/values as object.");
#endif
                }

                IDictionary dict;
                try
                {
                    dict = (IDictionary)ReflectionUtils.SafeCreateInstance(type);
                }
                catch (MissingMethodException)
                {
                    if (genericArgs.Length >= 2)
                        dict = ReflectionUtils.SafeCreateDictionary(keyType,valueType);
                    else
                        dict = null;
                    if (dict == null) throw;
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                    Guard.LogError(
                        "DeserializeFromJsonObject: Failed to create dictionary instance of \"" +
                        type.FullName + "\". " + ex.Message);
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
                        "DeserializeFromJsonObject: Cannot convert key \"" +
                        kvp.Key + "\" to \"" + keyType.FullName +
                        "\". Skipping. " + ex.Message);
#endif
                        continue;
                    }
                    // 索引器赋值：重复 key 后者覆盖前者，不抛异常
                    dict[dictKey] = this.DeserializeObject(
                        kvp.Value,valueType);
                }

                output = dict;
                return true;
            }

            // POCO
            object instance;
            try
            {
                instance = ReflectionUtils.SafeCreateInstance(type);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                Guard.LogError(
                    "DeserializeFromJsonObject: Failed to create instance of \"" +
                    type.FullName + "\". " + ex.Message);
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
            }

            output = instance;
            return true;
        }


    }


    // ──────────────────────────────────────────────────────────────
    // JsonObject  (IDictionary<string,object> 别名，保持原库接口)
    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Represents the json object.
    /// </summary>
    [GeneratedCode("simple-json","1.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [SuppressMessage("Microsoft.Naming","CA1710:IdentifiersShouldHaveCorrectSuffix")]
#if SIMPLE_JSON_OBJARRAYINTERNAL
    internal
#else
    public
#endif
   class JsonObject :
#if SIMPLE_JSON_DYNAMIC
 //DynamicObject,
#endif
 IDictionary<string,object>
    {
        /// <summary>
        /// The internal member dictionary.
        /// </summary>
        private readonly Dictionary<string,object> _members;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonObject"/>.
        /// </summary>
        public JsonObject()
        {
            _members = new Dictionary<string,object>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> implementation to use when comparing keys, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1"/> for the type of the key.</param>
        public JsonObject(IEqualityComparer<string> comparer)
        {
            _members = new Dictionary<string,object>(comparer);
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value></value>
        public object this[int index]
        {
            get { return GetAtIndex(_members,index); }
        }

        internal static object GetAtIndex(IDictionary<string,object> obj,int index)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (index >= obj.Count)
                throw new ArgumentOutOfRangeException("index");
            int i = 0;
            foreach (KeyValuePair<string,object> o in obj)
                if (i++ == index) return o.Value;
            return null;
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(string key,object value)
        {
            _members.Add(key,value);
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///     <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(string key)
        {
            return _members.ContainsKey(key);
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys
        {
            get { return _members.Keys; }
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            return _members.Remove(key);
        }

        /// <summary>
        /// Tries the get value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool TryGetValue(string key,out object value)
        {
            return _members.TryGetValue(key,out value);
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<object> Values
        {
            get { return _members.Values; }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        /// <value></value>
        public object this[string key]
        {
            get { return _members[key]; }
            set { _members[key] = value; }
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<string,object> item)
        {
            _members.Add(item.Key,item.Value);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _members.Clear();
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// 	<c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(KeyValuePair<string,object> item)
        {
            return _members.ContainsKey(item.Key) && _members[item.Key] == item.Value;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(KeyValuePair<string,object>[] array,int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            int num = Count;
            foreach (KeyValuePair<string,object> kvp in this)
            {
                array[arrayIndex++] = kvp;
                if (--num <= 0)
                    return;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return _members.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string,object> item)
        {
            return _members.Remove(item.Key);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string,object>> GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        /// <summary>
        /// Returns a json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return SimpleJson.SerializeObject(this);
        }

#if SIMPLE_JSON_DYNAMIC
        ///// <summary>
        ///// Provides implementation for type conversion operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that convert an object from one type to another.
        ///// </summary>
        ///// <param name="binder">Provides information about the conversion operation. The binder.Type property provides the type to which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Type returns the <see cref="T:System.String"/> type. The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for explicit conversion and false for implicit conversion.</param>
        ///// <param name="result">The result of the type conversion operation.</param>
        ///// <returns>
        ///// Always returns true.
        ///// </returns>
        //public override bool TryConvert(ConvertBinder binder, out object result)
        //{
        //    // <pex>
        //    if (binder == null)
        //        throw new ArgumentNullException("binder");
        //    // </pex>
        //    Type targetType = binder.Type;

        //    if ((targetType == typeof(IEnumerable)) ||
        //        (targetType == typeof(IEnumerable<KeyValuePair<string, object>>)) ||
        //        (targetType == typeof(IDictionary<string, object>)) ||
        //        (targetType == typeof(IDictionary)))
        //    {
        //        result = this;
        //        return true;
        //    }

        //    return base.TryConvert(binder, out result);
        //}

        ///// <summary>
        ///// Provides the implementation for operations that delete an object member. This method is not intended for use in C# or Visual Basic.
        ///// </summary>
        ///// <param name="binder">Provides information about the deletion.</param>
        ///// <returns>
        ///// Always returns true.
        ///// </returns>
        //public override bool TryDeleteMember(DeleteMemberBinder binder)
        //{
        //    // <pex>
        //    if (binder == null)
        //        throw new ArgumentNullException("binder");
        //    // </pex>
        //    return _members.Remove(binder.Name);
        //}

        ///// <summary>
        ///// Provides the implementation for operations that get a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for indexing operations.
        ///// </summary>
        ///// <param name="binder">Provides information about the operation.</param>
        ///// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] operation in C# (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class, <paramref name="indexes"/> is equal to 3.</param>
        ///// <param name="result">The result of the index operation.</param>
        ///// <returns>
        ///// Always returns true.
        ///// </returns>
        //public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        //{
        //    if (indexes == null) throw new ArgumentNullException("indexes");
        //    if (indexes.Length == 1)
        //    {
        //        result = ((IDictionary<string, object>)this)[(string)indexes[0]];
        //        return true;
        //    }
        //    result = null;
        //    return true;
        //}

        ///// <summary>
        ///// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        ///// </summary>
        ///// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        ///// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        ///// <returns>
        ///// Always returns true.
        ///// </returns>
        //public override bool TryGetMember(GetMemberBinder binder, out object result)
        //{
        //    object value;
        //    if (_members.TryGetValue(binder.Name, out value))
        //    {
        //        result = value;
        //        return true;
        //    }
        //    result = null;
        //    return true;
        //}

        ///// <summary>
        ///// Provides the implementation for operations that set a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that access objects by a specified index.
        ///// </summary>
        ///// <param name="binder">Provides information about the operation.</param>
        ///// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="indexes"/> is equal to 3.</param>
        ///// <param name="value">The value to set to the object that has the specified index. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="value"/> is equal to 10.</param>
        ///// <returns>
        ///// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
        ///// </returns>
        //public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        //{
        //    if (indexes == null) throw new ArgumentNullException("indexes");
        //    if (indexes.Length == 1)
        //    {
        //        ((IDictionary<string, object>)this)[(string)indexes[0]] = value;
        //        return true;
        //    }
        //    return base.TrySetIndex(binder, indexes, value);
        //}

        ///// <summary>
        ///// Provides the implementation for operations that set member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        ///// </summary>
        ///// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        ///// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        ///// <returns>
        ///// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        ///// </returns>
        //public override bool TrySetMember(SetMemberBinder binder, object value)
        //{
        //    // <pex>
        //    if (binder == null)
        //        throw new ArgumentNullException("binder");
        //    // </pex>
        //    _members[binder.Name] = value;
        //    return true;
        //}

        ///// <summary>
        ///// Returns the enumeration of all dynamic member names.
        ///// </summary>
        ///// <returns>
        ///// A sequence that contains dynamic member names.
        ///// </returns>
        //public override IEnumerable<string> GetDynamicMemberNames()
        //{
        //    foreach (var key in Keys)
        //        yield return key;
        //}
#endif
    }
    // ──────────────────────────────────────────────────────────────
    // JsonArray  (IList<object> 别名，保持原库接口)
    // ──────────────────────────────────────────────────────────────
    /// Represents the json array.
    /// </summary>
    [GeneratedCode("simple-json","1.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [SuppressMessage("Microsoft.Naming","CA1710:IdentifiersShouldHaveCorrectSuffix")]
#if SIMPLE_JSON_OBJARRAYINTERNAL
    internal
#else
    public
#endif
    class JsonArray : List<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        public JsonArray() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the json array.</param>
        public JsonArray(int capacity) : base(capacity) { }

        /// <summary>
        /// The json representation of the array.
        /// </summary>
        /// <returns>The json representation of the array.</returns>
        public override string ToString()
        {
            return SimpleJson.SerializeObject(this) ?? string.Empty;
        }
    }

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
        internal const int TOKEN_NONE = 0;
        internal const int TOKEN_CURLY_OPEN = 1;
        internal const int TOKEN_CURLY_CLOSE = 2;
        internal const int TOKEN_SQUARED_OPEN = 3;
        internal const int TOKEN_SQUARED_CLOSE = 4;
        internal const int TOKEN_COLON = 5;
        internal const int TOKEN_COMMA = 6;
        internal const int TOKEN_STRING = 7;
        internal const int TOKEN_NUMBER = 8;
        internal const int TOKEN_TRUE = 9;
        internal const int TOKEN_FALSE = 10;
        internal const int TOKEN_NULL = 11;
        internal const int BUILDER_CAPACITY = 2000;

        internal static readonly char[] EscapeTable;
        internal static readonly char[] EscapeCharacters = new char[] { '"', '\\',
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
            '\x08', '\x09', '\x0a', '\x0b', '\x0c', '\x0d', '\x0e', '\x0f',
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
            '\x18', '\x19', '\x1a', '\x1b', '\x1c', '\x1d', '\x1e', '\x1f'
        };

        static JsonParser()
        {
            EscapeTable = new char[93];
            EscapeTable['"'] = '"';
            EscapeTable['\\'] = '\\';
            EscapeTable['\b'] = 'b';
            EscapeTable['\f'] = 'f';
            EscapeTable['\n'] = 'n';
            EscapeTable['\r'] = 'r';
            EscapeTable['\t'] = 't';
        }
        // ── 公开入口 ────────────────────────────────────────────

        public static bool TryParse(string json,out object obj)
        {
            if (string.IsNullOrEmpty(json))
            {
                obj = null;
                return false;
            }
            bool success = true;
            char[] charArray = json.ToCharArray();
            int index = 0;
            obj = ParseValue(charArray,ref index,ref success);
            return success;
        }

        // ── 核心解析 ────────────────────────────────────────────

        static IDictionary<string,object> ParseObject(char[] json,ref int index,ref bool success)
        {
            IDictionary<string,object> table = new JsonObject();
            int token;

            // {
            NextToken(json,ref index);

            bool done = false;
            while (!done)
            {
                token = LookAhead(json,index);
                if (token == TOKEN_NONE)
                {
                    success = false;
                    return null;
                }
                else if (token == TOKEN_COMMA)
                    NextToken(json,ref index);
                else if (token == TOKEN_CURLY_CLOSE)
                {
                    NextToken(json,ref index);
                    return table;
                }
                else
                {
                    // name
                    string name = ParseString(json,ref index,ref success);
                    if (!success)
                    {
                        success = false;
                        return null;
                    }
                    // :
                    token = NextToken(json,ref index);
                    if (token != TOKEN_COLON)
                    {
                        success = false;
                        return null;
                    }
                    // value
                    object value = ParseValue(json,ref index,ref success);
                    if (!success)
                    {
                        success = false;
                        return null;
                    }
                    table[name] = value;
                }
            }
            return table;
        }

        static JsonArray ParseArray(char[] json,ref int index,ref bool success)
        {
            JsonArray array = new JsonArray();

            // [
            NextToken(json,ref index);

            bool done = false;
            while (!done)
            {
                int token = LookAhead(json,index);
                if (token == TOKEN_NONE)
                {
                    success = false;
                    return null;
                }
                else if (token == TOKEN_COMMA)
                    NextToken(json,ref index);
                else if (token == TOKEN_SQUARED_CLOSE)
                {
                    NextToken(json,ref index);
                    break;
                }
                else
                {
                    object value = ParseValue(json,ref index,ref success);
                    if (!success)
                        return null;
                    array.Add(value);
                }
            }
            return array;
        }

        static object ParseValue(char[] json,ref int index,ref bool success)
        {
            switch (LookAhead(json,index))
            {
                case TOKEN_STRING:
                    return ParseString(json,ref index,ref success);
                case TOKEN_NUMBER:
                    return ParseNumber(json,ref index,ref success);
                case TOKEN_CURLY_OPEN:
                    return ParseObject(json,ref index,ref success);
                case TOKEN_SQUARED_OPEN:
                    return ParseArray(json,ref index,ref success);
                case TOKEN_TRUE:
                    NextToken(json,ref index);
                    return true;
                case TOKEN_FALSE:
                    NextToken(json,ref index);
                    return false;
                case TOKEN_NULL:
                    NextToken(json,ref index);
                    return null;
                case TOKEN_NONE:
                    break;
            }
            success = false;
            return null;
        }

        static string ParseString(char[] json,ref int index,ref bool success)
        {
            //不初始就创建一个new StringBuilder(BUILDER_CAPACITY)，避免无效内存消耗
            StringBuilder s = null;
            char c;

            EatWhitespace(json,ref index);

            // "
            c = json[index++];

            int startIndex = index;
            bool complete = false;
            while (!complete)
            {
                if (index == json.Length)
                    break;

                c = json[index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {
                    if (s == null)
                    {
                        s = new StringBuilder(BUILDER_CAPACITY);
                        for (int i = startIndex; i < index - 1; i++)
                            s.Append(json[i]);
                    }

                    if (index == json.Length)
                        break;
                    c = json[index++];
                    if (c == '"')
                        s.Append('"');
                    else if (c == '\\')
                        s.Append('\\');
                    else if (c == '/')
                        s.Append('/');
                    else if (c == 'b')
                        s.Append('\b');
                    else if (c == 'f')
                        s.Append('\f');
                    else if (c == 'n')
                        s.Append('\n');
                    else if (c == 'r')
                        s.Append('\r');
                    else if (c == 't')
                        s.Append('\t');
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if (!(success = UInt32.TryParse(new string(json,index,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out codePoint)))
                                return "";

                            // convert the integer codepoint to a unicode char and add to string
                            if (0xD800 <= codePoint && codePoint <= 0xDBFF)  // if high surrogate
                            {
                                index += 4; // skip 4 chars
                                remainingLength = json.Length - index;
                                if (remainingLength >= 6)
                                {
                                    uint lowCodePoint;
                                    if (new string(json,index,2) == "\\u" && UInt32.TryParse(new string(json,index + 2,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out lowCodePoint))
                                    {
                                        if (0xDC00 <= lowCodePoint && lowCodePoint <= 0xDFFF)    // if low surrogate
                                        {
                                            s.Append((char)codePoint);
                                            s.Append((char)lowCodePoint);
                                            index += 6; // skip 6 chars
                                            continue;
                                        }
                                    }
                                }
                                success = false;    // invalid surrogate pair
                                return "";
                            }
                            s.Append(ConvertFromUtf32((int)codePoint));
                            // skip 4 chars
                            index += 4;
                        }
                        else
                            break;
                    }
                }
                else
                {
                    if (s != null)
                        s.Append(c);
                }
            }
            if (!complete)
            {
                success = false;
                return null;
            }

            if (s != null)
                return s.ToString();

            return new string(json,startIndex,index - startIndex - 1);
        }

        private static string ConvertFromUtf32(int utf32)
        {
            // http://www.java2s.com/Open-Source/CSharp/2.6.4-mono-.net-core/System/System/Char.cs.htm
            if (utf32 < 0 || utf32 > 0x10FFFF)
                throw new ArgumentOutOfRangeException("utf32","The argument must be from 0 to 0x10FFFF.");
            if (0xD800 <= utf32 && utf32 <= 0xDFFF)
                throw new ArgumentOutOfRangeException("utf32","The argument must not be in surrogate pair range.");
            if (utf32 < 0x10000)
                return new string((char)utf32,1);
            utf32 -= 0x10000;
            return new string(new char[] { (char)((utf32 >> 10) + 0xD800),(char)(utf32 % 0x0400 + 0xDC00) });
        }

        static object ParseNumber(char[] json,ref int index,ref bool success)
        {
            EatWhitespace(json,ref index);
            int lastIndex = GetLastIndexOfNumber(json,index);
            int charLength = (lastIndex - index) + 1;
            object returnNumber;
            string str = new string(json,index,charLength);
            if (str.IndexOf(".",StringComparison.OrdinalIgnoreCase) != -1 || str.IndexOf("e",StringComparison.OrdinalIgnoreCase) != -1)
            {
                double number;
                success = double.TryParse(new string(json,index,charLength),NumberStyles.Any,CultureInfo.InvariantCulture,out number);
                returnNumber = number;
            }
            else
            {
                long number;
                if (long.TryParse(str,NumberStyles.Any,CultureInfo.InvariantCulture,out number))
                {
                    returnNumber = number;
                }
                else
                {
                    ulong ulongNumber;
                    if (ulong.TryParse(str,NumberStyles.Any,CultureInfo.InvariantCulture,out ulongNumber))
                    {
                        returnNumber = ulongNumber;
                    }
                    else
                    {
                        double doubleNumber;
                        success = double.TryParse(str,NumberStyles.Any,CultureInfo.InvariantCulture,out doubleNumber);
                        returnNumber = doubleNumber;
                    }
                }
            }
            index = lastIndex + 1;
            return returnNumber;
        }

        static int GetLastIndexOfNumber(char[] json,int index)
        {
            int lastIndex;
            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1) break;
            return lastIndex - 1;
        }

        static void EatWhitespace(char[] json,ref int index)
        {
            for (; index < json.Length; index++)
                if (" \t\n\r\b\f".IndexOf(json[index]) == -1) break;
        }

        static int LookAhead(char[] json,int index)
        {
            int saveIndex = index;
            return NextToken(json,ref saveIndex);
        }

        [SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity")]
        static int NextToken(char[] json,ref int index)
        {
            EatWhitespace(json,ref index);
            if (index == json.Length)
                return TOKEN_NONE;
            char c = json[index];
            index++;
            switch (c)
            {
                case '{':
                    return TOKEN_CURLY_OPEN;
                case '}':
                    return TOKEN_CURLY_CLOSE;
                case '[':
                    return TOKEN_SQUARED_OPEN;
                case ']':
                    return TOKEN_SQUARED_CLOSE;
                case ',':
                    return TOKEN_COMMA;
                case '"':
                    return TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return TOKEN_NUMBER;
                case ':':
                    return TOKEN_COLON;
            }
            index--;
            int remainingLength = json.Length - index;
            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e')
                {
                    index += 5;
                    return TOKEN_FALSE;
                }
            }
            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e')
                {
                    index += 4;
                    return TOKEN_TRUE;
                }
            }
            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l')
                {
                    index += 4;
                    return TOKEN_NULL;
                }
            }
            return TOKEN_NONE;
        }
    }
    // ──────────────────────────────────────────────────────────────
    // SimpleJson 主类
    // ──────────────────────────────────────────────────────────────

#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    static class SimpleJson
    {
        private static volatile IJsonSerializerStrategy
            _currentJsonSerializerStrategy;

#if SIMPLE_JSON_WEBGL
        // WebGL 单线程：无需锁
#else
        private static readonly object _strategyLock = new object();
#endif

        // ── AOT 类型注册 ─────────────────────────────────────────────

        private static readonly Dictionary<string,Type> _aotTypeRegistry =
            new Dictionary<string,Type>();

        private static readonly Dictionary<string,Func<object>> _aotFactoryRegistry =
            new Dictionary<string,Func<object>>();

        private static readonly Dictionary<Type,Func<object>> _aotFactoryByType =
            new Dictionary<Type,Func<object>>();

#if SIMPLE_JSON_WEBGL
        // WebGL 单线程：无需锁
#else
        private static readonly object _registryLock = new object();
#endif

        /// <summary>
        /// 注册AOT类型，用于在AOT环境中创建泛型集合实例。
        /// </summary>
        /// <param name="typeName">类型名称，如 "Dictionary&lt;String,Int32&gt;"</param>
        /// <param name="type">对应的Type</param>
        public static void RegisterAotType(string typeName,Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

#if SIMPLE_JSON_WEBGL
            _aotTypeRegistry[typeName] = type;
#else
            lock (_registryLock)
            {
                _aotTypeRegistry[typeName] = type;
            }
#endif
        }

        /// <summary>
        /// 注册AOT类型及其工厂委托。在AOT环境下，Activator.CreateInstance 可能失败，
        /// 使用工厂委托可以确保类型实例的正确创建。
        /// </summary>
        /// <param name="typeName">类型名称（兼容旧接口）</param>
        /// <param name="type">对应的Type</param>
        /// <param name="factory">创建实例的工厂委托</param>
        public static void RegisterAotType(string typeName,Type type,Func<object> factory)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (factory == null)
                throw new ArgumentNullException("factory");

#if SIMPLE_JSON_WEBGL
            _aotTypeRegistry[typeName] = type;
            _aotFactoryRegistry[typeName] = factory;
            _aotFactoryByType[type] = factory;
#else
            lock (_registryLock)
            {
                _aotTypeRegistry[typeName] = type;
                _aotFactoryRegistry[typeName] = factory;
                _aotFactoryByType[type] = factory;
            }
#endif
        }

        /// <summary>
        /// 注册AOT类型及其工厂委托（推荐使用此方法）。
        /// 以 Type 对象为键，避免泛型类型 FullName 不匹配的问题。
        /// </summary>
        /// <param name="type">要注册的类型</param>
        /// <param name="factory">创建实例的工厂委托</param>
        public static void RegisterAotType(Type type,Func<object> factory)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (factory == null)
                throw new ArgumentNullException("factory");

#if SIMPLE_JSON_WEBGL
            _aotFactoryByType[type] = factory;
            _aotTypeRegistry[type.FullName] = type;
            _aotFactoryRegistry[type.FullName] = factory;
#else
            lock (_registryLock)
            {
                _aotFactoryByType[type] = factory;
                _aotTypeRegistry[type.FullName] = type;
                _aotFactoryRegistry[type.FullName] = factory;
            }
#endif
        }

        /// <summary>
        /// 获取已注册的AOT类型。
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>已注册的类型，未找到返回null</returns>
        public static Type GetRegisteredAotType(string typeName)
        {
#if SIMPLE_JSON_WEBGL
            Type type;
            return _aotTypeRegistry.TryGetValue(typeName,out type) ? type : null;
#else
            lock (_registryLock)
            {
                Type type;
                return _aotTypeRegistry.TryGetValue(typeName,out type) ? type : null;
            }
#endif
        }

        /// <summary>
        /// 获取已注册的AOT工厂委托。
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>工厂委托，未找到返回null</returns>
        public static Func<object> GetRegisteredAotFactory(string typeName)
        {
#if SIMPLE_JSON_WEBGL
            Func<object> factory;
            return _aotFactoryRegistry.TryGetValue(typeName,out factory) ? factory : null;
#else
            lock (_registryLock)
            {
                Func<object> factory;
                return _aotFactoryRegistry.TryGetValue(typeName,out factory) ? factory : null;
            }
#endif
        }

        /// <summary>
        /// 通过 Type 对象获取已注册的AOT工厂委托。
        /// </summary>
        /// <param name="type">类型对象</param>
        /// <returns>工厂委托，未找到返回null</returns>
        public static Func<object> GetRegisteredAotFactory(Type type)
        {
#if SIMPLE_JSON_WEBGL
            Func<object> factory;
            return _aotFactoryByType.TryGetValue(type,out factory) ? factory : null;
#else
            lock (_registryLock)
            {
                Func<object> factory;
                return _aotFactoryByType.TryGetValue(type,out factory) ? factory : null;
            }
#endif
        }

        /// <summary>
        /// 初始化常用的AOT类型注册。
        /// 默认支持全面的基础类型，包括数值类型、字符串、日期时间、GUID、Uri等。
        /// </summary>
        public static void InitializeCommonAotTypes()
        {
            // ──────────────────────────────────────────────────────────────
            // Dictionary<string, T> - string 键的字典
            // ──────────────────────────────────────────────────────────────
            RegisterAotType(typeof(Dictionary<string,object>),() => new Dictionary<string,object>());
            RegisterAotType(typeof(Dictionary<string,string>),() => new Dictionary<string,string>());

            // 有符号整数
            RegisterAotType(typeof(Dictionary<string,int>),() => new Dictionary<string,int>());
            RegisterAotType(typeof(Dictionary<string,long>),() => new Dictionary<string,long>());
            RegisterAotType(typeof(Dictionary<string,short>),() => new Dictionary<string,short>());
            RegisterAotType(typeof(Dictionary<string,sbyte>),() => new Dictionary<string,sbyte>());

            // 无符号整数
            RegisterAotType(typeof(Dictionary<string,uint>),() => new Dictionary<string,uint>());
            RegisterAotType(typeof(Dictionary<string,ulong>),() => new Dictionary<string,ulong>());
            RegisterAotType(typeof(Dictionary<string,ushort>),() => new Dictionary<string,ushort>());
            RegisterAotType(typeof(Dictionary<string,byte>),() => new Dictionary<string,byte>());

            // 浮点类型
            RegisterAotType(typeof(Dictionary<string,float>),() => new Dictionary<string,float>());
            RegisterAotType(typeof(Dictionary<string,double>),() => new Dictionary<string,double>());
            RegisterAotType(typeof(Dictionary<string,decimal>),() => new Dictionary<string,decimal>());

            // 其他基础类型
            RegisterAotType(typeof(Dictionary<string,bool>),() => new Dictionary<string,bool>());
            RegisterAotType(typeof(Dictionary<string,char>),() => new Dictionary<string,char>());

            // 日期时间类型
            RegisterAotType(typeof(Dictionary<string,DateTime>),() => new Dictionary<string,DateTime>());
            RegisterAotType(typeof(Dictionary<string,DateTimeOffset>),() => new Dictionary<string,DateTimeOffset>());
            RegisterAotType(typeof(Dictionary<string,TimeSpan>),() => new Dictionary<string,TimeSpan>());

            // 特殊类型
            RegisterAotType(typeof(Dictionary<string,Guid>),() => new Dictionary<string,Guid>());
            RegisterAotType(typeof(Dictionary<string,Uri>),() => new Dictionary<string,Uri>());
            RegisterAotType(typeof(Dictionary<string,Version>),() => new Dictionary<string,Version>());

            // ──────────────────────────────────────────────────────────────
            // List<T> - 列表
            // ──────────────────────────────────────────────────────────────
            RegisterAotType(typeof(List<object>),() => new List<object>());
            RegisterAotType(typeof(List<string>),() => new List<string>());

            // 有符号整数
            RegisterAotType(typeof(List<int>),() => new List<int>());
            RegisterAotType(typeof(List<long>),() => new List<long>());
            RegisterAotType(typeof(List<short>),() => new List<short>());
            RegisterAotType(typeof(List<sbyte>),() => new List<sbyte>());

            // 无符号整数
            RegisterAotType(typeof(List<uint>),() => new List<uint>());
            RegisterAotType(typeof(List<ulong>),() => new List<ulong>());
            RegisterAotType(typeof(List<ushort>),() => new List<ushort>());
            RegisterAotType(typeof(List<byte>),() => new List<byte>());

            // 浮点类型
            RegisterAotType(typeof(List<float>),() => new List<float>());
            RegisterAotType(typeof(List<double>),() => new List<double>());
            RegisterAotType(typeof(List<decimal>),() => new List<decimal>());

            // 其他基础类型
            RegisterAotType(typeof(List<bool>),() => new List<bool>());
            RegisterAotType(typeof(List<char>),() => new List<char>());

            // 日期时间类型
            RegisterAotType(typeof(List<DateTime>),() => new List<DateTime>());
            RegisterAotType(typeof(List<DateTimeOffset>),() => new List<DateTimeOffset>());
            RegisterAotType(typeof(List<TimeSpan>),() => new List<TimeSpan>());

            // 特殊类型
            RegisterAotType(typeof(List<Guid>),() => new List<Guid>());
            RegisterAotType(typeof(List<Uri>),() => new List<Uri>());
            RegisterAotType(typeof(List<Version>),() => new List<Version>());

            // ──────────────────────────────────────────────────────────────
            // Dictionary<K, V> - 非 string 键的字典
            // ──────────────────────────────────────────────────────────────
            RegisterAotType(typeof(Dictionary<int,string>),() => new Dictionary<int,string>());
            RegisterAotType(typeof(Dictionary<int,int>),() => new Dictionary<int,int>());
            RegisterAotType(typeof(Dictionary<int,object>),() => new Dictionary<int,object>());
            RegisterAotType(typeof(Dictionary<long,string>),() => new Dictionary<long,string>());
            RegisterAotType(typeof(Dictionary<Guid,string>),() => new Dictionary<Guid,string>());
            RegisterAotType(typeof(Dictionary<Guid,object>),() => new Dictionary<Guid,object>());

            // ──────────────────────────────────────────────────────────────
            // 嵌套泛型集合
            // ──────────────────────────────────────────────────────────────
            // List<List<T>>
            RegisterAotType(typeof(List<List<string>>),() => new List<List<string>>());
            RegisterAotType(typeof(List<List<int>>),() => new List<List<int>>());
            RegisterAotType(typeof(List<List<object>>),() => new List<List<object>>());

            // Dictionary<string, List<T>>
            RegisterAotType(typeof(Dictionary<string,List<string>>),() => new Dictionary<string,List<string>>());
            RegisterAotType(typeof(Dictionary<string,List<int>>),() => new Dictionary<string,List<int>>());
            RegisterAotType(typeof(Dictionary<string,List<object>>),() => new Dictionary<string,List<object>>());

            // Dictionary<string, Dictionary<K, V>>
            RegisterAotType(typeof(Dictionary<string,Dictionary<string,string>>),() => new Dictionary<string,Dictionary<string,string>>());
            RegisterAotType(typeof(Dictionary<string,Dictionary<string,int>>),() => new Dictionary<string,Dictionary<string,int>>());
            RegisterAotType(typeof(Dictionary<string,Dictionary<string,object>>),() => new Dictionary<string,Dictionary<string,object>>());

            // List<Dictionary<string, T>>
            RegisterAotType(typeof(List<Dictionary<string,string>>),() => new List<Dictionary<string,string>>());
            RegisterAotType(typeof(List<Dictionary<string,int>>),() => new List<Dictionary<string,int>>());
            RegisterAotType(typeof(List<Dictionary<string,object>>),() => new List<Dictionary<string,object>>());
        }

 

        /// <summary>
        /// jsonString 是 JSON 字符串的原始文本，包含转义字符（如 \n、\t、\\ 等）。此方法将这些转义字符转换为它们对应的实际字符，使字符串更适合在 JavaScript 中使用。
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static string EscapeToJavascriptString(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return jsonString;

            StringBuilder sb = new StringBuilder();
            char c;

            for (int i = 0; i < jsonString.Length;)
            {
                c = jsonString[i++];

                if (c == '\\')
                {
                    int remainingLength = jsonString.Length - i;
                    if (remainingLength >= 2)
                    {
                        char lookahead = jsonString[i];
                        if (lookahead == '\\')
                        {
                            sb.Append('\\');
                            ++i;
                        }
                        else if (lookahead == '"')
                        {
                            sb.Append("\"");
                            ++i;
                        }
                        else if (lookahead == 't')
                        {
                            sb.Append('\t');
                            ++i;
                        }
                        else if (lookahead == 'b')
                        {
                            sb.Append('\b');
                            ++i;
                        }
                        else if (lookahead == 'n')
                        {
                            sb.Append('\n');
                            ++i;
                        }
                        else if (lookahead == 'r')
                        {
                            sb.Append('\r');
                            ++i;
                        }
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 全局默认序列化策略。
        /// </summary>
        /// <remarks>
        /// 线程安全说明：属性本身的读写是原子的（volatile），但动态修改
        /// strategy 实例的 ignoreLowerCaseDeserialization 等设置不是线程安全的。
        /// 多线程场景建议每次调用显式传入 strategy 实例。
        /// </remarks>
        public static IJsonSerializerStrategy CurrentJsonSerializerStrategy
        {
            get
            {
                if (_currentJsonSerializerStrategy == null)
                {
#if SIMPLE_JSON_WEBGL
                    _currentJsonSerializerStrategy =
                        new DefaultJsonSerializationStrategy();
#else
                    lock (_strategyLock)
                    {
                        if (_currentJsonSerializerStrategy == null)
                            _currentJsonSerializerStrategy =
                                new DefaultJsonSerializationStrategy();
                    }
#endif
                }
                return _currentJsonSerializerStrategy;
            }
            set
            {
                Guard.ArgumentNotNull(value,"value");
                _currentJsonSerializerStrategy = value;
            }
        }

        public static void ClearReflectionCache()
        {
            CurrentJsonSerializerStrategy.ClearCache();
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
            jsonSerializerStrategy.ClearSerializationStack();

            var builder = new StringBuilder();
            SerializeValue(jsonSerializerStrategy,obj,builder);
            return builder.ToString();
        }
        #region Serialize Core
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
            if (IsNumeric(value))
            {
                return SerializeNumber(value,builder);
            }

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

        static bool SerializeString(string aString,StringBuilder builder)
        {
            // Happy path if there's nothing to be escaped. IndexOfAny is highly optimized (and unmanaged)
            if (aString.IndexOfAny(JsonParser.EscapeCharacters) == -1)
            {
                builder.Append('"');
                builder.Append(aString);
                builder.Append('"');

                return true;
            }

            builder.Append('"');
            int safeCharacterCount = 0;
            char[] charArray = aString.ToCharArray();

            for (int i = 0; i < charArray.Length; i++)
            {
                char c = charArray[i];

                // Non ascii characters are fine, buffer them up and send them to the builder
                // in larger chunks if possible. The escape table is a 1:1 translation table
                // with \0 [default(char)] denoting a safe character.
                if (Char.IsControl(c) || c == '\"' || c == '\\')
                {
                    if (safeCharacterCount > 0)
                    {
                        builder.Append(charArray,i - safeCharacterCount,safeCharacterCount);
                        safeCharacterCount = 0;
                    }

                    builder.Append('\\');
                    switch (c)
                    {
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '\"':
                            builder.Append('\"');
                            break;
                        case '\b':
                            builder.Append('b');
                            break;
                        case '\f':
                            builder.Append('f');
                            break;
                        case '\r':
                            builder.Append('r');
                            break;
                        case '\t':
                            builder.Append('t');
                            break;
                        case '\n':
                            builder.Append('n');
                            break;
                        default:
                            builder.AppendFormat("u{0:X4}",(int)c);
                            break;
                    }
                }
                else
                {
                    safeCharacterCount++;
                }
            }

            if (safeCharacterCount > 0)
            {
                builder.Append(charArray,charArray.Length - safeCharacterCount,safeCharacterCount);
            }

            builder.Append('"');
            return true;
        }
        /// <summary>
        /// Determines if a given object is numeric in any way
        /// (can be integer, double, null, etc).
        /// </summary>
       internal static bool IsNumeric(object value)
        {
            if (value is sbyte) return true;
            if (value is byte) return true;
            if (value is short) return true;
            if (value is ushort) return true;
            if (value is int) return true;
            if (value is uint) return true;
            if (value is long) return true;
            if (value is ulong) return true;
            if (value is float) return true;
            if (value is double) return true;
            if (value is decimal) return true;
            return false;
        }
        static bool SerializeNumber(object number,StringBuilder builder)
        {
            if (number is long)
                builder.Append(((long)number).ToString(CultureInfo.InvariantCulture));
            else if (number is ulong)
                builder.Append(((ulong)number).ToString(CultureInfo.InvariantCulture));
            else if (number is int)
                builder.Append(((int)number).ToString(CultureInfo.InvariantCulture));
            else if (number is uint)
                builder.Append(((uint)number).ToString(CultureInfo.InvariantCulture));
            else if (number is decimal)
                builder.Append(((decimal)number).ToString(CultureInfo.InvariantCulture));
            else if (number is float)
            {
                float fval = (float)number;
                if (float.IsNaN(fval) || float.IsInfinity(fval))
                    builder.Append("null");
                else
                    builder.Append(fval.ToString("G9",CultureInfo.InvariantCulture));
            }
            else if (number is double)
            {
                double dval = (double)number;
                if (double.IsNaN(dval) || double.IsInfinity(dval))
                    builder.Append("null");
                else
                    builder.Append(dval.ToString("G17",CultureInfo.InvariantCulture));
            }
            else
                builder.Append(Convert.ToDouble(number,CultureInfo.InvariantCulture).ToString("r",CultureInfo.InvariantCulture));
            return true;
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
        #endregion
        // ── 反序列化 ────────────────────────────────────────────────
        /// <summary>
        /// Parses the string json into a value,返回的是一个简单的 JSON 数据结构（IList<object>、IDictionary<string,object>、double、string、null、true 或 false）。如果解析失败，则抛出 InvalidOperationException 异常。建议使用 TryDeserializeObject 方法进行安全解析，避免异常开销。
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An IList&lt;object>, a IDictionary&lt;string,object>, a double, a string, null, true, or false</returns>
        public static object DeserializeObject(string json)
        {
            object obj;
            if (TryDeserializeObject(json,out obj))
                return obj;
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }
        /// <summary>
        /// Try parsing the json string into a value.解析成功返回 true，并通过 out 参数 obj 返回解析结果；解析失败返回 false，out 参数 obj 将被设置为 null。此方法不会抛出异常，适用于不确定输入格式的场景。
        /// </summary>
        /// <param name="json">
        /// A JSON string.
        /// </param>
        /// <param name="obj">
        /// The object.
        /// </param>
        /// <returns>
        /// Returns true if successfull otherwise false.
        /// </returns>
        [SuppressMessage("Microsoft.Design","CA1007:UseGenericsWhereAppropriate",Justification = "Need to support .NET 2")]
        public static bool TryDeserializeObject(string json,out object obj)
        {
            // 委托给 JsonParser（原库 JSON 解析器，保持不变）
            return JsonParser.TryParse(json,out obj);
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
            //object parsed = DeserializeObject(json);
            //return DeserializeObject(parsed,type,strategy);
            object jsonObject = DeserializeObject(json);

            if (type == null || jsonObject == null) return jsonObject;
            if (ReflectionUtils.IsAssignableFrom(jsonObject.GetType(),type))
            {
                return jsonObject;
            }
            IJsonSerializerStrategy jsonSerializerStrategy = (strategy != null ? strategy : CurrentJsonSerializerStrategy);

            return jsonSerializerStrategy.DeserializeObject(jsonObject,type);

        }


    }

} // namespace SimpleJson
#if SIMPLE_JSON_UNITY

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
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    class UnitySerializationStrategy : DefaultJsonSerializationStrategy
    {
       public UnitySerializationStrategy() : base() { }

        public UnitySerializationStrategy(bool ignoreLowerCase, bool useJsonAliasSerialization)
            : base(ignoreLowerCase, useJsonAliasSerialization) { }
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
#if DEBUG111
                //没有unity环境状态
                output = null;
                return false;
#else
                output = UnityEngine.JsonUtility.ToJson(input);
 //#endif
                return true;
#endif
            }
            return base.TrySerializeKnownTypes(input, out output);
        }

        protected override bool DeserializeFromJsonObject(
            object value, Type type, out object output)
        {
            if (IsUnityType(type))
            {
                // 情形一：value 是 JSON 字符串
                string str = value as string;
                if (!string.IsNullOrEmpty(str))
                {
#if DEBUG1111
                    //没有unity环境状态
                    output = null;
                    return false;
#else
                    output = UnityEngine.JsonUtility.FromJson(str, type);
                    return true;
#endif
                }

                // 情形二：value 已被预解析为字典（JSON 对象）
                IDictionary<string, object> dict =
                    value as IDictionary<string, object>;
                if (dict != null)
                {
                    string json = SimpleJson.SerializeObject(dict);
                    if (!string.IsNullOrEmpty(json))
                    {
#if DEBUG111
                        //没有unity环境状态
                        output = null;
                        return false;   
#else
                        output = UnityEngine.JsonUtility.FromJson(json, type);
                        return true;
#endif
                    }
                }
            }
            return base.DeserializeFromJsonObject(value, type, out output);
        }
    }
}

#endif // SIMPLE_JSON_UNITY

#if SIMPLE_JSON_DATACONTRACT

namespace RS.SimpleJsonUnity
{
    using System.Runtime.Serialization;

    /// <summary>
    /// 支持 DataContract/DataMember/IgnoreDataMember 特性的序列化策略。
    /// </summary>
    /// <remarks>
    /// 启用条件：在编译时定义 SIMPLE_JSON_DATACONTRACT 宏。
    /// 当类型标记了 [DataContract] 特性时，只有标记了 [DataMember] 的成员会被序列化。
    /// 未标记 [DataContract] 的类型将使用默认的 POCO 序列化行为。
    /// </remarks>
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    class DataContractSerializationStrategy : DefaultJsonSerializationStrategy
    {
        public DataContractSerializationStrategy() : base() { }

        public DataContractSerializationStrategy(bool ignoreLowerCase,bool useJsonAliasSerialization)
            : base(ignoreLowerCase,useJsonAliasSerialization) { }

        private static bool HasDataContract(Type type)
        {
            return type != null && type.IsDefined(typeof(DataContractAttribute),true);
        }

        private static bool HasIgnoreDataMember(MemberInfo member)
        {
            return member.IsDefined(typeof(IgnoreDataMemberAttribute),true);
        }

        private static DataMemberAttribute GetDataMemberAttribute(MemberInfo member)
        {
            object[] attrs = member.GetCustomAttributes(typeof(DataMemberAttribute),true);
            return attrs.Length > 0 ? (DataMemberAttribute)attrs[0] : null;
        }

        private static string GetDataMemberJsonKey(MemberInfo member,DataMemberAttribute dma)
        {
            if (dma != null && !string.IsNullOrEmpty(dma.Name))
                return dma.Name;
            return member.Name;
        }

        protected override IDictionary<string,Func<object,object>>
            BuildGetters(Type type,bool useJsonAlias,IJsonSerializerStrategy strategy)
        {
            if (!HasDataContract(type))
                return base.BuildGetters(type,useJsonAlias,strategy);

            var getters = new List<KeyValuePair<int,KeyValuePair<string,Func<object,object>>>>();
            var seen = new HashSet<string>();

            Action<MemberInfo,DataMemberAttribute> addGetter =
                (member,dma) =>
                {
                    if (seen.Contains(member.Name)) return;
                    if (HasIgnoreDataMember(member)) return;
                    seen.Add(member.Name);

                    string key = GetDataMemberJsonKey(member,dma);
                    int order = dma != null ? dma.Order : int.MaxValue;

                    PropertyInfo pi = member as PropertyInfo;
                    FieldInfo fi = member as FieldInfo;

                    if (pi != null)
                    {
                        PropertyInfo captured = pi;
                        bool emitDefault = dma != null && !dma.EmitDefaultValue;
                        Type memberType = pi.PropertyType;
                        object defaultValue = null;
                        if (emitDefault && memberType.IsValueType)
                        {
                            try { defaultValue = ReflectionUtils.SafeCreateInstance(memberType); }
                            catch { }
                        }
                        object capturedDefault = defaultValue;
                        bool hasDefault = capturedDefault != null;
                        getters.Add(new KeyValuePair<int,KeyValuePair<string,Func<object,object>>>(
                            order,
                            new KeyValuePair<string,Func<object,object>>(
                                key,
                                emitDefault
                                    ? (hasDefault
                                        ? (Func<object,object>)(obj =>
                                        {
                                            object val = captured.GetValue(obj,null);
                                            if (val == null || capturedDefault.Equals(val))
                                                return OmitValue;
                                            return val;
                                        })
                                        : (Func<object,object>)(obj =>
                                        {
                                            object val = captured.GetValue(obj,null);
                                            if (val == null) return OmitValue;
                                            return val;
                                        }))
                                    : (Func<object,object>)(obj => captured.GetValue(obj,null)))));
                    }
                    else if (fi != null)
                    {
                        FieldInfo captured = fi;
                        bool emitDefault = dma != null && !dma.EmitDefaultValue;
                        Type memberType = fi.FieldType;
                        object defaultValue = null;
                        if (emitDefault && memberType.IsValueType)
                        {
                            try { defaultValue = ReflectionUtils.SafeCreateInstance(memberType); }
                            catch { }
                        }
                        object capturedDefault = defaultValue;
                        bool hasDefault = capturedDefault != null;
                        getters.Add(new KeyValuePair<int,KeyValuePair<string,Func<object,object>>>(
                            order,
                            new KeyValuePair<string,Func<object,object>>(
                                key,
                                emitDefault
                                    ? (hasDefault
                                        ? (Func<object,object>)(obj =>
                                        {
                                            object val = captured.GetValue(obj);
                                            if (val == null || capturedDefault.Equals(val))
                                                return OmitValue;
                                            return val;
                                        })
                                        : (Func<object,object>)(obj =>
                                        {
                                            object val = captured.GetValue(obj);
                                            if (val == null) return OmitValue;
                                            return val;
                                        }))
                                    : (Func<object,object>)(obj => captured.GetValue(obj)))));
                    }
                };

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                DataMemberAttribute dma = GetDataMemberAttribute(p);
                if (dma == null) continue;
                addGetter(p,dma);
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(f);
                if (dma == null) continue;
                addGetter(f,dma);
            }

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanRead) continue;
                DataMemberAttribute dma = GetDataMemberAttribute(p);
                if (dma == null) continue;
                if (HasIgnoreDataMember(p)) continue;
                addGetter(p,dma);
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(f);
                if (dma == null) continue;
                if (HasIgnoreDataMember(f)) continue;
                addGetter(f,dma);
            }

            getters.Sort((a,b) => a.Key.CompareTo(b.Key));

            var result = new Dictionary<string,Func<object,object>>();
            foreach (var item in getters)
                result[item.Value.Key] = item.Value.Value;
            return result;
        }

        protected override IDictionary<string,Action<object,object>>
            BuildSetters(Type type,bool ignoreLowerCase,IJsonSerializerStrategy strategy)
        {
            if (!HasDataContract(type))
                return base.BuildSetters(type,ignoreLowerCase,strategy);

            var setters = ignoreLowerCase
                ? new Dictionary<string,Action<object,object>>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string,Action<object,object>>();
            var seen = new HashSet<string>();

            Action<MemberInfo,Type,DataMemberAttribute,Action<object,object>> register =
                (member,memberType,dma,rawSetter) =>
                {
                    if (seen.Contains(member.Name)) return;
                    if (HasIgnoreDataMember(member)) return;
                    seen.Add(member.Name);

                    Type capturedType = memberType;
                    MemberInfo capturedMember = member;
                    Action<object,object> safeSetter = (obj,val) =>
                    {
                        try
                        {
                            rawSetter(obj,CoerceValue(val,capturedType,strategy));
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG
                            Guard.LogWarning(
                            "SetValue: Cannot assign to \"" +
                            capturedMember.DeclaringType.FullName + "." +
                            capturedMember.Name + "\". " + ex.Message);
#endif
                        }
                    };

                    string jsonKey = GetDataMemberJsonKey(member,dma);
                    setters[jsonKey] = safeSetter;

                    if (dma != null && !string.IsNullOrEmpty(dma.Name))
                    {
                        setters[member.Name] = safeSetter;
                    }

                    JsonAliasAttribute aliasAttr = ReflectionUtils.GetAttribute<JsonAliasAttribute>(member);
                    if (aliasAttr != null)
                    {
                        foreach (string alias in aliasAttr.Aliases)
                            setters[alias] = safeSetter;
                        if (aliasAttr.AcceptOriginalName)
                            setters[member.Name] = safeSetter;
                    }
                };

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                DataMemberAttribute dma = GetDataMemberAttribute(p);
                if (dma == null) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,dma,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.PUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(f);
                if (dma == null) continue;
                FieldInfo captured = f;
                register(f,f.FieldType,dma,
                    (obj,val) => captured.SetValue(obj,val));
            }

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (!p.CanWrite) continue;
                DataMemberAttribute dma = GetDataMemberAttribute(p);
                if (dma == null) continue;
                if (HasIgnoreDataMember(p)) continue;
                PropertyInfo captured = p;
                register(p,p.PropertyType,dma,
                    (obj,val) => captured.SetValue(obj,val,null));
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(f);
                if (dma == null) continue;
                if (HasIgnoreDataMember(f)) continue;
                FieldInfo captured = f;
                register(f,f.FieldType,dma,
                    (obj,val) => captured.SetValue(obj,val));
            }

            return setters;
        }

        protected override bool DeserializeFromJsonObject(
            object value,Type type,out object output)
        {
            bool result = base.DeserializeFromJsonObject(value,type,out output);

            if (result && HasDataContract(type) && output != null)
            {
                ValidateRequiredMembers(value,type);
            }

            return result;
        }

        private void ValidateRequiredMembers(object value,Type type)
        {
            var jsonObj = value as IDictionary<string,object>;
            if (jsonObj == null) return;

            Action<MemberInfo,DataMemberAttribute> checkMember = (member,dma) =>
            {
                if (dma == null || !dma.IsRequired) return;
                if (HasIgnoreDataMember(member)) return;

                string jsonKey = GetDataMemberJsonKey(member,dma);
                bool found = jsonObj.ContainsKey(jsonKey);
                if (!found && !string.IsNullOrEmpty(dma.Name) && dma.Name != member.Name)
                    found = jsonObj.ContainsKey(member.Name);

                if (!found)
                {
                    throw new SerializationException(
                        string.Format(
                            "Required member '{0}' on type '{1}' was not found in JSON.",
                            member.Name,type.FullName));
                }
            };

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.PUBLIC_INSTANCE))
                checkMember(p,GetDataMemberAttribute(p));

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.PUBLIC_INSTANCE))
                checkMember(f,GetDataMemberAttribute(f));

            foreach (PropertyInfo p in ReflectionUtils.GetProperties(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(p);
                if (dma == null) continue;
                checkMember(p,dma);
            }

            foreach (FieldInfo f in ReflectionUtils.GetFields(type,ReflectionUtils.NONPUBLIC_INSTANCE))
            {
                DataMemberAttribute dma = GetDataMemberAttribute(f);
                if (dma == null) continue;
                checkMember(f,dma);
            }
        }
    }
}

#endif // SIMPLE_JSON_DATACONTRACT


// ReSharper restore LoopCanBeConvertedToQuery
// ReSharper restore RedundantExplicitArrayCreation
// ReSharper restore SuggestUseVarKeywordEvident