//-----------------------------------------------------------------------
// <copyright file="SimpleJson.cs" company="The Outercurve Foundation">
//    Copyright (c) 2011, The Outercurve Foundation.
//
//    Licensed under the MIT License (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.opensource.org/licenses/mit-license.php
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <author>andyhebear,Nathan Totten (ntotten.com), Jim Zimmerman (jimzimmerman.com) and Prabir Shrestha (prabir.me)</author>
// <website>https://github.com/facebook-csharp-sdk/simple-json</website>
// <website>https://github.com/RS-Unity3D/RS.SimpleJson-Unity/blob/v1.x/SimpleJson.cs</website>
//-----------------------------------------------------------------------
//sgd:2026.5.7 fixed clear Construct Cache and fixed time format
//sgd:2026.2.20 support aot compiler
//sgd: 2025.12.15 support unity 3d
//sgd: 2025.11.10 support property To lower case
//sgd: 2025.10.1 support string key dictionary
// VERSION: 1.2.0.0

//NOTE: uncomment the following line to make SimpleJson class internal.
//#define SIMPLE_JSON_INTERNAL

// NOTE: uncomment the following line to make JsonArray and JsonObject class internal.
#define SIMPLE_JSON_OBJARRAYINTERNAL

// NOTE: uncomment the following line to enable dynamic support.
//#define SIMPLE_JSON_DYNAMIC

// NOTE: uncomment the following line to enable DataContract support.
#define SIMPLE_JSON_DATACONTRACT

// NOTE: uncomment the following line to enable IReadOnlyCollection<T> and IReadOnlyList<T> support.
#define SIMPLE_JSON_READONLY_COLLECTIONS

// NOTE: uncomment the following line to disable linq expressions/compiled lambda (better performance) instead of method.invoke().
// define if you are using .net framework <= 3.0 or < WP7.5
#define SIMPLE_JSON_NO_LINQ_EXPRESSION

// NOTE: uncomment the following line if you are compiling under Window Metro style application/library.
// usually already defined in properties
//#define NETFX_CORE;

//NOTE: support AOT compiler, but will disable dynamic and DataContract support, and use method.invoke() instead of compiled lambda, which is slower.
#define SIMPLE_JSON_AOT

//NOTE:If you are targetting WinStore, WP8 and NET4.5+ PCL make sure to #define SIMPLE_JSON_TYPEINFO;
//#define SIMPLE_JSON_TYPEINFO;
// original json parsing code from http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html

//add
//NOTE:Ignore the case of attributes/fields
#define SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE

//NOTE:Private fields marked with JsonInclude are serialized when onlyPublic=false
//Private properties marked with JsonInclude are serialized when onlyPublic=false
//By default (onlyPublic=true), private members are not included
//#define SIMPLE_JSON_OnlyPublicProperty

//NOTE:Single-threaded mode, such as WebGL, does not consider multi-threading.
//#define SIMPLE_JSON_SINGLE_THREADED

#if NET35 || NET40
#undef SIMPLE_JSON_READONLY_COLLECTIONS
#endif

#if NETFX_CORE
#define SIMPLE_JSON_TYPEINFO
#endif

//NOTE:Unity support
#if UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
#define SIMPLE_JSON_UNITY
//unity webgl does not support multi-threading
#if UNITY_WEBGL
#define SIMPLE_JSON_SINGLE_THREADED
#endif
//unity aot compiler
#if ENABLE_IL2CPP
#define SIMPLE_JSON_AOT
#endif
#endif
#if SIMPLE_JSON_UNITY
using UnityEngine;
#endif



using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
//#if !SIMPLE_JSON_NO_LINQ_EXPRESSION
//using System.Linq.Expressions;
//#endif
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
//#if SIMPLE_JSON_DYNAMIC
//using System.Dynamic;
//#endif
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
//

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable SuggestUseVarKeywordEvident
namespace IRobotQ.Core.SimpleJsonEx//RS.SimpleJsonAOT//GitHub.Unity.Json
{
    static class Constants {
        public static readonly string[] Iso8601Formats = {
              @"yyyy-MM-dd\THH:mm:ss.FFFFFFF\Z",
             @"yyyy-MM-dd\THH:mm:ss.FFFFFFFK",
             @"yyyy-MM-dd\THH:mm:ss\Z",
             @"yyyy-MM-dd\THH:mm:ssK"
        };
    }
    [Serializable]
    internal class InstanceNotInitializedException : InvalidOperationException {
        public InstanceNotInitializedException(object the,string property) :
            base(String.Format(CultureInfo.InvariantCulture,"{0} is not correctly initialized, {1} is null",the?.GetType().Name,property)) { }

        protected InstanceNotInitializedException(SerializationInfo info,StreamingContext context) : base(info,context) { }
    }
    internal static class Guard {
        public static void NotNull(object the,object value,string propertyName) {
            if(value != null) return;
            throw new InstanceNotInitializedException(the,propertyName);
        }
        public static void ArgumentNotNull(object value,string name) {
            if(value != null) return;
            string message = String.Format(CultureInfo.InvariantCulture,"Failed Null Check on '{0}'",name);
            throw new ArgumentNullException(name,message);
        }

        public static void ArgumentNotNullOrEmpty<T>(IList<T> value,string name) {
            if(value == null) {
                string message = String.Format(CultureInfo.InvariantCulture,"Failed Null Check on '{0}'",name);
                throw new ArgumentNullException(name,message);
            }

            if(value.Count == 0) {
                string message = String.Format(CultureInfo.InvariantCulture,"Failed Empty Check on '{0}'",name);
                throw new ArgumentNullException(name,message);
            }
        }

        public static void ArgumentNonNegative(int value,string name) {
            if(value > -1) return;

            var message = String.Format(CultureInfo.InvariantCulture,"The value for '{0}' must be non-negative",name);
            throw new ArgumentException(message,name);
        }

        /// <summary>
        ///   Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name = "value">The argument value to check.</param>
        /// <param name = "name">The name of the argument.</param>
        public static void ArgumentNotNullOrWhiteSpace(string value,string name) {
            if(value != null && value.Trim().Length > 0)
                return;
            string message = String.Format(CultureInfo.InvariantCulture,"The value for '{0}' must not be empty",name);
            throw new ArgumentException(message,name);
        }

        public static void ArgumentInRange(int value,int minValue,string name) {
            if(value >= minValue) return;
            string message = String.Format(CultureInfo.InvariantCulture,
                "The value '{0}' for '{1}' must be greater than or equal to '{2}'",
                value,
                name,
                minValue);
            throw new ArgumentOutOfRangeException(name,message);
        }

        public static void ArgumentInRange(int value,int minValue,int maxValue,string name) {
            if(value >= minValue && value <= maxValue) return;
            string message = String.Format(CultureInfo.InvariantCulture,
                "The value '{0}' for '{1}' must be greater than or equal to '{2}' and less than or equal to '{3}'",
                value,
                name,
                minValue,
                maxValue);
            throw new ArgumentOutOfRangeException(name,message);
        }

        public static bool InUnitTestRunner { get; set; }
    }
    /// <summary>
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
 class JsonArray : List<object> {
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
        public override string ToString() {
            return SimpleJson.SerializeObject(this) ?? string.Empty;
        }
    }

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
 DynamicObject,
#endif
 IDictionary<string,object> {
        /// <summary>
        /// The internal member dictionary.
        /// </summary>
        private readonly Dictionary<string,object> _members;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonObject"/>.
        /// </summary>
        public JsonObject() {
            _members = new Dictionary<string,object>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> implementation to use when comparing keys, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1"/> for the type of the key.</param>
        public JsonObject(IEqualityComparer<string> comparer) {
            _members = new Dictionary<string,object>(comparer);
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value></value>
        public object this[int index] {
            get { return GetAtIndex(_members,index); }
        }

        internal static object GetAtIndex(IDictionary<string,object> obj,int index) {
            if(obj == null)
                throw new ArgumentNullException("obj");
            if(index >= obj.Count)
                throw new ArgumentOutOfRangeException("index");
            int i = 0;
            foreach(KeyValuePair<string,object> o in obj)
                if(i++ == index) return o.Value;
            return null;
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(string key,object value) {
            _members.Add(key,value);
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///     <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(string key) {
            return _members.ContainsKey(key);
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys {
            get { return _members.Keys; }
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Remove(string key) {
            return _members.Remove(key);
        }

        /// <summary>
        /// Tries the get value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool TryGetValue(string key,out object value) {
            return _members.TryGetValue(key,out value);
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<object> Values {
            get { return _members.Values; }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        /// <value></value>
        public object this[string key] {
            get { return _members[key]; }
            set { _members[key] = value; }
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<string,object> item) {
            _members.Add(item.Key,item.Value);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear() {
            _members.Clear();
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///     <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(KeyValuePair<string,object> item) {
            return _members.ContainsKey(item.Key) && _members[item.Key] == item.Value;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(KeyValuePair<string,object>[] array,int arrayIndex) {
            if(array == null) throw new ArgumentNullException("array");
            int num = Count;
            foreach(KeyValuePair<string,object> kvp in this) {
                array[arrayIndex++] = kvp;
                if(--num <= 0)
                    return;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count {
            get { return _members.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string,object> item) {
            return _members.Remove(item.Key);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string,object>> GetEnumerator() {
            return _members.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return _members.GetEnumerator();
        }

        /// <summary>
        /// Returns a json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString() {
            return SimpleJson.SerializeObject(this);
        }

#if SIMPLE_JSON_DYNAMIC
        /// <summary>
        /// Provides implementation for type conversion operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that convert an object from one type to another.
        /// </summary>
        /// <param name="binder">Provides information about the conversion operation. The binder.Type property provides the type to which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Type returns the <see cref="T:System.String"/> type. The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for explicit conversion and false for implicit conversion.</param>
        /// <param name="result">The result of the type conversion operation.</param>
        /// <returns>
        /// Alwasy returns true.
        /// </returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            // <pex>
            if (binder == null)
                throw new ArgumentNullException("binder");
            // </pex>
            Type targetType = binder.Type;

            if ((targetType == typeof(IEnumerable)) ||
                (targetType == typeof(IEnumerable<KeyValuePair<string, object>>)) ||
                (targetType == typeof(IDictionary<string, object>)) ||
                (targetType == typeof(IDictionary)))
            {
                result = this;
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that delete an object member. This method is not intended for use in C# or Visual Basic.
        /// </summary>
        /// <param name="binder">Provides information about the deletion.</param>
        /// <returns>
        /// Alwasy returns true.
        /// </returns>
        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            // <pex>
            if (binder == null)
                throw new ArgumentNullException("binder");
            // </pex>
            return _members.Remove(binder.Name);
        }

        /// <summary>
        /// Provides the implementation for operations that get a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for indexing operations.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] operation in C# (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class, <paramref name="indexes"/> is equal to 3.</param>
        /// <param name="result">The result of the index operation.</param>
        /// <returns>
        /// Alwasy returns true.
        /// </returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes == null) throw new ArgumentNullException("indexes");
            if (indexes.Length == 1)
            {
                result = ((IDictionary<string, object>)this)[(string)indexes[0]];
                return true;
            }
            result = null;
            return true;
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// Alwasy returns true.
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            object value;
            if (_members.TryGetValue(binder.Name, out value))
            {
                result = value;
                return true;
            }
            result = null;
            return true;
        }

        /// <summary>
        /// Provides the implementation for operations that set a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that access objects by a specified index.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="indexes"/> is equal to 3.</param>
        /// <param name="value">The value to set to the object that has the specified index. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="value"/> is equal to 10.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
        /// </returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes == null) throw new ArgumentNullException("indexes");
            if (indexes.Length == 1)
            {
                ((IDictionary<string, object>)this)[(string)indexes[0]] = value;
                return true;
            }
            return base.TrySetIndex(binder, indexes, value);
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // <pex>
            if (binder == null)
                throw new ArgumentNullException("binder");
            // </pex>
            _members[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>
        /// A sequence that contains dynamic member names.
        /// </returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (var key in Keys)
                yield return key;
        }
#endif
    }

    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    ///
    /// JSON uses Arrays and Objects. These correspond here to the datatypes JsonArray(IList&lt;object>) and JsonObject(IDictionary&lt;string,object>).
    /// All numbers are parsed to doubles.
    /// </summary>
    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
 static class SimpleJson {
        private const int TOKEN_NONE = 0;
        private const int TOKEN_CURLY_OPEN = 1;
        private const int TOKEN_CURLY_CLOSE = 2;
        private const int TOKEN_SQUARED_OPEN = 3;
        private const int TOKEN_SQUARED_CLOSE = 4;
        private const int TOKEN_COLON = 5;
        private const int TOKEN_COMMA = 6;
        private const int TOKEN_STRING = 7;
        private const int TOKEN_NUMBER = 8;
        private const int TOKEN_TRUE = 9;
        private const int TOKEN_FALSE = 10;
        private const int TOKEN_NULL = 11;
        private const int BUILDER_CAPACITY = 2000;

        private static readonly char[] EscapeTable;
        private static readonly char[] EscapeCharacters = new char[] { '"','\\','\b','\f','\n','\r','\t' };
        private static readonly string EscapeCharactersString = new string(EscapeCharacters);

        static SimpleJson() {
            EscapeTable = new char[93];
            EscapeTable['"'] = '"';
            EscapeTable['\\'] = '\\';
            EscapeTable['\b'] = 'b';
            EscapeTable['\f'] = 'f';
            EscapeTable['\n'] = 'n';
            EscapeTable['\r'] = 'r';
            EscapeTable['\t'] = 't';
        }
#if SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE
        ///// <summary>
        ///// Specify the serialization method that ignores the case of attributes/fields
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="model"></param>
        ///// <param name="lowerCase"></param>
        ///// <param name="onlyPublic"></param>
        ///// <returns></returns>
        //public static string ToJson<T>(this T model,bool lowerCase = false,bool onlyPublic = true)
        //{

        //    return SerializeObject(model,SimpleJsonSerializerExtensions.GetStrategy(lowerCase,onlyPublic));
        //}
        ///// <summary>
        ///// Specify the deserialization method that ignores the case of attributes/fields
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="json"></param>
        ///// <param name="lowerCase"></param>
        ///// <param name="onlyPublic"></param>
        ///// <returns></returns>
        //public static T FromJson<T>(this string json,bool lowerCase = false,bool onlyPublic = true)
        //{
        //    return DeserializeObject<T>(json,SimpleJsonSerializerExtensions.GetStrategy(lowerCase,onlyPublic));
        //}
#endif
        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An IList&lt;object>, a IDictionary&lt;string,object>, a double, a string, null, true, or false</returns>
        public static object DeserializeObject(string json) {
            object obj;
            if(TryDeserializeObject(json,out obj))
                return obj;
            throw new SerializationException("Invalid JSON string");
        }

        /// <summary>
        /// Try parsing the json string into a value.
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
        public static bool TryDeserializeObject(string json,out object obj) {
            bool success = true;
            if(json != null) {
                char[] charArray = json.ToCharArray();
                int index = 0;
                obj = ParseValue(charArray,ref index,ref success);
            }
            else
                obj = null;

            return success;
        }

        public static object DeserializeObject(string json,Type type,IJsonSerializerStrategy jsonSerializerStrategy) {
            object jsonObject = DeserializeObject(json);
            return type == null || jsonObject != null && ReflectionUtils.IsAssignableFrom(jsonObject.GetType(),type)
                       ? jsonObject
                       : (jsonSerializerStrategy ?? CurrentJsonSerializerStrategy).DeserializeObject(jsonObject,type);
        }

        public static object DeserializeObject(string json,Type type) {
            return DeserializeObject(json,type,null);
        }

        public static T DeserializeObject<T>(string json,IJsonSerializerStrategy jsonSerializerStrategy) {
            return (T)DeserializeObject(json,typeof(T),jsonSerializerStrategy);
        }

        public static T DeserializeObject<T>(string json) {
            return (T)DeserializeObject(json,typeof(T),null);
        }

        /// <summary>
        /// Converts a IDictionary&lt;string,object> / IList&lt;object> object into a JSON string
        /// </summary>
        /// <param name="json">A IDictionary&lt;string,object> / IList&lt;object></param>
        /// <param name="jsonSerializerStrategy">Serializer strategy to use</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
        public static string SerializeObject(object json,IJsonSerializerStrategy jsonSerializerStrategy) {
            StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
            bool success = SerializeValue(jsonSerializerStrategy,json,builder);
            return (success ? builder.ToString() : null);
        }

        public static string SerializeObject(object json) {
            return SerializeObject(json,CurrentJsonSerializerStrategy);
        }

        public static string EscapeToJavascriptString(string jsonString) {
            if(string.IsNullOrEmpty(jsonString))
                return jsonString;

            StringBuilder sb = new StringBuilder();
            char c;

            for(int i = 0; i < jsonString.Length;) {
                c = jsonString[i++];

                if(c == '\\') {
                    int remainingLength = jsonString.Length - i;
                    if(remainingLength >= 2) {
                        char lookahead = jsonString[i];
                        if(lookahead == '\\') {
                            sb.Append('\\');
                            ++i;
                        }
                        else if(lookahead == '"') {
                            sb.Append("\"");
                            ++i;
                        }
                        else if(lookahead == 't') {
                            sb.Append('\t');
                            ++i;
                        }
                        else if(lookahead == 'b') {
                            sb.Append('\b');
                            ++i;
                        }
                        else if(lookahead == 'n') {
                            sb.Append('\n');
                            ++i;
                        }
                        else if(lookahead == 'r') {
                            sb.Append('\r');
                            ++i;
                        }
                    }
                }
                else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        static IDictionary<string,object> ParseObject(char[] json,ref int index,ref bool success) {
            IDictionary<string,object> table = new JsonObject();
            //sgd:There might be a problem with directly ignoring case here. 直接在这里忽略大小写可能存在问题
            //IDictionary<string,object> table = new JsonObject(StringComparer.OrdinalIgnoreCase);
            int token;

            // {
            NextToken(json,ref index);

            bool done = false;
            while(!done) {
                token = LookAhead(json,index);
                if(token == TOKEN_NONE) {
                    success = false;
                    return null;
                }
                else if(token == TOKEN_COMMA)
                    NextToken(json,ref index);
                else if(token == TOKEN_CURLY_CLOSE) {
                    NextToken(json,ref index);
                    return table;
                }
                else {
                    // name
                    string name = ParseString(json,ref index,ref success);
                    if(!success) {
                        success = false;
                        return null;
                    }
                    // :
                    token = NextToken(json,ref index);
                    if(token != TOKEN_COLON) {
                        success = false;
                        return null;
                    }
                    // value
                    object value = ParseValue(json,ref index,ref success);
                    if(!success) {
                        success = false;
                        return null;
                    }
                    table[name] = value;
                }
            }
            return table;
        }

        static JsonArray ParseArray(char[] json,ref int index,ref bool success) {
            JsonArray array = new JsonArray();

            // [
            NextToken(json,ref index);

            bool done = false;
            while(!done) {
                int token = LookAhead(json,index);
                if(token == TOKEN_NONE) {
                    success = false;
                    return null;
                }
                else if(token == TOKEN_COMMA)
                    NextToken(json,ref index);
                else if(token == TOKEN_SQUARED_CLOSE) {
                    NextToken(json,ref index);
                    break;
                }
                else {
                    object value = ParseValue(json,ref index,ref success);
                    if(!success)
                        return null;
                    array.Add(value);
                }
            }
            return array;
        }

        static object ParseValue(char[] json,ref int index,ref bool success) {
            switch(LookAhead(json,index)) {
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

        static string ParseString(char[] json,ref int index,ref bool success) {
            StringBuilder s = null;
            char c;

            EatWhitespace(json,ref index);

            // "
            c = json[index++];

            int startIndex = index;
            bool complete = false;
            while(!complete) {
                if(index == json.Length)
                    break;

                c = json[index++];
                if(c == '"') {
                    complete = true;
                    break;
                }
                else if(c == '\\') {
                    if(s == null) {
                        s = new StringBuilder(BUILDER_CAPACITY);
                        for(int i = startIndex; i < index - 1; i++)
                            s.Append(json[i]);
                    }

                    if(index == json.Length)
                        break;
                    c = json[index++];
                    if(c == '"')
                        s.Append('"');
                    else if(c == '\\')
                        s.Append('\\');
                    else if(c == '/')
                        s.Append('/');
                    else if(c == 'b')
                        s.Append('\b');
                    else if(c == 'f')
                        s.Append('\f');
                    else if(c == 'n')
                        s.Append('\n');
                    else if(c == 'r')
                        s.Append('\r');
                    else if(c == 't')
                        s.Append('\t');
                    else if(c == 'u') {
                        int remainingLength = json.Length - index;
                        if(remainingLength >= 4) {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if(!(success = UInt32.TryParse(new string(json,index,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out codePoint)))
                                return "";

                            // convert the integer codepoint to a unicode char and add to string
                            if(0xD800 <= codePoint && codePoint <= 0xDBFF)  // if high surrogate
                            {
                                index += 4; // skip 4 chars
                                remainingLength = json.Length - index;
                                if(remainingLength >= 6) {
                                    uint lowCodePoint;
                                    if(new string(json,index,2) == "\\u" && UInt32.TryParse(new string(json,index + 2,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out lowCodePoint)) {
                                        if(0xDC00 <= lowCodePoint && lowCodePoint <= 0xDFFF)    // if low surrogate
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
                else {
                    if(s != null)
                        s.Append(c);
                }
            }
            if(!complete) {
                success = false;
                return null;
            }

            if(s != null)
                return s.ToString();

            return new string(json,startIndex,index - startIndex - 1);
        }

        private static string ConvertFromUtf32(int utf32) {
            // http://www.java2s.com/Open-Source/CSharp/2.6.4-mono-.net-core/System/System/Char.cs.htm
            if(utf32 < 0 || utf32 > 0x10FFFF)
                throw new ArgumentOutOfRangeException("utf32","The argument must be from 0 to 0x10FFFF.");
            if(0xD800 <= utf32 && utf32 <= 0xDFFF)
                throw new ArgumentOutOfRangeException("utf32","The argument must not be in surrogate pair range.");
            if(utf32 < 0x10000)
                return new string((char)utf32,1);
            utf32 -= 0x10000;
            return new string(new char[] { (char)((utf32 >> 10) + 0xD800),(char)(utf32 % 0x0400 + 0xDC00) });
        }

        static object ParseNumber(char[] json,ref int index,ref bool success) {
            EatWhitespace(json,ref index);
            int lastIndex = GetLastIndexOfNumber(json,index);
            int charLength = (lastIndex - index) + 1;
            object returnNumber;
            string str = new string(json,index,charLength);
            if(str.IndexOf(".",StringComparison.OrdinalIgnoreCase) != -1 || str.IndexOf("e",StringComparison.OrdinalIgnoreCase) != -1) {
                double number;
                success = double.TryParse(new string(json,index,charLength),NumberStyles.Any,CultureInfo.InvariantCulture,out number);
                returnNumber = number;
            }
            else {
                //long number;
                //success = long.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
                //returnNumber = number;
                //Support uint, ulong
                long number;
                success = long.TryParse(new string(json,index,charLength),NumberStyles.Any,CultureInfo.InvariantCulture,out number);
                if(success)
                    returnNumber = number;
                else {
                    ulong unsign_number;
                    success = ulong.TryParse(str,NumberStyles.Any,CultureInfo.InvariantCulture,out unsign_number);
                    returnNumber = unsign_number;
                }
            }
            index = lastIndex + 1;
            return returnNumber;
        }

        static int GetLastIndexOfNumber(char[] json,int index) {
            int lastIndex;
            for(lastIndex = index; lastIndex < json.Length; lastIndex++)
                if("0123456789+-.eE".IndexOf(json[lastIndex]) == -1) break;
            return lastIndex - 1;
        }

        static void EatWhitespace(char[] json,ref int index) {
            for(; index < json.Length; index++)
                if(" \t\n\r\b\f".IndexOf(json[index]) == -1) break;
        }

        static int LookAhead(char[] json,int index) {
            int saveIndex = index;
            return NextToken(json,ref saveIndex);
        }

        [SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity")]
        static int NextToken(char[] json,ref int index) {
            EatWhitespace(json,ref index);
            if(index == json.Length)
                return TOKEN_NONE;
            char c = json[index];
            index++;
            switch(c) {
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
            if(remainingLength >= 5) {
                if(json[index] == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e') {
                    index += 5;
                    return TOKEN_FALSE;
                }
            }
            // true
            if(remainingLength >= 4) {
                if(json[index] == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e') {
                    index += 4;
                    return TOKEN_TRUE;
                }
            }
            // null
            if(remainingLength >= 4) {
                if(json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l') {
                    index += 4;
                    return TOKEN_NULL;
                }
            }
            return TOKEN_NONE;
        }

        static bool SerializeValue(IJsonSerializerStrategy jsonSerializerStrategy,object value,StringBuilder builder) {
            bool success = true;
            string stringValue = value as string;
            if(stringValue != null)
                success = SerializeString(stringValue,builder);
            else {
                if(value == null) {
                    builder.Append("null");
                    return true;
                }
                IDictionary<string,object> dict = value as IDictionary<string,object>;
                //IDictionary<string, List<string>> dict2 = value as IDictionary<string, List<string>>;
                bool isTypeDictionary = false;
                if(ReflectionUtils.IsTypeDictionary(value.GetType())) {
                    isTypeDictionary = true;
                }
                if(dict != null) {
                    success = SerializeObject(jsonSerializerStrategy,dict.Keys,dict.Values,builder);
                }
                //else if (dict2 != null) {
                //    success = SerializeObject(jsonSerializerStrategy, dict2.Keys, dict2.Values, builder);
                //}
                else if(isTypeDictionary) {
                    //sgd:add dictionary key  string  support
                    // if dictionary then
                    Type ts = value.GetType();
                    object keys = ts.GetProperty("Keys").GetValue(value,null);
                    object values = ts.GetProperty("Values").GetValue(value,null);
                    //dictionary key support string               
                    Type enumerableType = typeof(IEnumerable<string>);
                    if(enumerableType.IsAssignableFrom(keys.GetType()) == false) {
                        throw new Exception("SimpleJson serialize dictionary key must string type!");
                    }
                    success = SerializeObject(jsonSerializerStrategy,(IEnumerable)keys,(IEnumerable)values,builder);
                }
                else {
                    IDictionary<string,string> stringDictionary = value as IDictionary<string,string>;
                    if(stringDictionary != null) {
                        success = SerializeObject(jsonSerializerStrategy,stringDictionary.Keys,stringDictionary.Values,builder);
                    }
                    else {
                        IEnumerable enumerableValue = value as IEnumerable;
                        if(enumerableValue != null)
                            success = SerializeArray(jsonSerializerStrategy,enumerableValue,builder);
                        else if(IsNumeric(value))
                            success = SerializeNumber(value,builder);
                        else if(value is bool)
                            builder.Append((bool)value ? "true" : "false");
                        // 添加 char 类型支持
                        else if(value is char)
                            SerializeString(new string((char)value,1),builder);
                        else {
                            object serializedObject;
                            success = jsonSerializerStrategy.TrySerializeNonPrimitiveObject(value,out serializedObject);
                            if(success)
                                SerializeValue(jsonSerializerStrategy,serializedObject,builder);
                        }
                    }
                }
            }
            return success;
        }

        static bool SerializeObject(IJsonSerializerStrategy jsonSerializerStrategy,IEnumerable keys,IEnumerable values,StringBuilder builder) {
            builder.Append("{");
            IEnumerator ke = keys.GetEnumerator();
            IEnumerator ve = values.GetEnumerator();
            bool first = true;
            while(ke.MoveNext() && ve.MoveNext()) {
                object key = ke.Current;
                object value = ve.Current;
                if(!first)
                    builder.Append(",");
                string stringKey = key as string;
                if(stringKey != null)
                    SerializeString(stringKey,builder);
                else
                    // FIX: 序列化 key 而不是 value
                    if(!SerializeValue(jsonSerializerStrategy,key,builder)) return false;
                builder.Append(":");
                if(!SerializeValue(jsonSerializerStrategy,value,builder))
                    return false;
                first = false;
            }
            builder.Append("}");
            return true;
        }

        static bool SerializeArray(IJsonSerializerStrategy jsonSerializerStrategy,IEnumerable anArray,StringBuilder builder) {
            builder.Append("[");
            bool first = true;
            foreach(object value in anArray) {
                if(!first)
                    builder.Append(",");
                if(!SerializeValue(jsonSerializerStrategy,value,builder))
                    return false;
                first = false;
            }
            builder.Append("]");
            return true;
        }

        static bool SerializeString(string aString,StringBuilder builder) {
            // Happy path if there's nothing to be escaped. IndexOfAny is highly optimized (and unmanaged)
            if(aString.IndexOfAny(EscapeCharacters) == -1) {
                builder.Append('"');
                builder.Append(aString);
                builder.Append('"');

                return true;
            }

            builder.Append('"');
            int safeCharacterCount = 0;
            char[] charArray = aString.ToCharArray();

            for(int i = 0; i < charArray.Length; i++) {
                char c = charArray[i];

                // Non ascii characters are fine, buffer them up and send them to the builder
                // in larger chunks if possible. The escape table is a 1:1 translation table
                // with \0 [default(char)] denoting a safe character.
                if(c >= EscapeTable.Length || EscapeTable[c] == default(char)) {
                    safeCharacterCount++;
                }
                else {
                    if(safeCharacterCount > 0) {
                        builder.Append(charArray,i - safeCharacterCount,safeCharacterCount);
                        safeCharacterCount = 0;
                    }

                    builder.Append('\\');
                    builder.Append(EscapeTable[c]);
                }
            }

            if(safeCharacterCount > 0) {
                builder.Append(charArray,charArray.Length - safeCharacterCount,safeCharacterCount);
            }

            builder.Append('"');
            return true;
        }

        static bool SerializeNumber(object number,StringBuilder builder) {
            // 添加对所有数值类型的显式支持，避免精度丢失
            if(number is long)
                builder.Append(((long)number).ToString(CultureInfo.InvariantCulture));
            else if(number is ulong)
                builder.Append(((ulong)number).ToString(CultureInfo.InvariantCulture));
            else if(number is int)
                builder.Append(((int)number).ToString(CultureInfo.InvariantCulture));
            else if(number is uint)
                builder.Append(((uint)number).ToString(CultureInfo.InvariantCulture));
            else if(number is short)
                builder.Append(((short)number).ToString(CultureInfo.InvariantCulture));
            else if(number is ushort)
                builder.Append(((ushort)number).ToString(CultureInfo.InvariantCulture));
            else if(number is sbyte)
                builder.Append(((sbyte)number).ToString(CultureInfo.InvariantCulture));
            else if(number is byte)
                builder.Append(((byte)number).ToString(CultureInfo.InvariantCulture));
            else if(number is decimal)
                builder.Append(((decimal)number).ToString(CultureInfo.InvariantCulture));
            else if(number is float)
                builder.Append(((float)number).ToString(CultureInfo.InvariantCulture));
            else
                builder.Append(Convert.ToDouble(number,CultureInfo.InvariantCulture).ToString("r",CultureInfo.InvariantCulture));
            return true;
        }

        /// <summary>
        /// Determines if a given object is numeric in any way
        /// (can be integer, double, null, etc).
        /// </summary>
        static bool IsNumeric(object value) {
            if(value is sbyte) return true;
            if(value is byte) return true;
            if(value is short) return true;
            if(value is ushort) return true;
            if(value is int) return true;
            if(value is uint) return true;
            if(value is long) return true;
            if(value is ulong) return true;
            if(value is float) return true;
            if(value is double) return true;
            if(value is decimal) return true;
            return false;
        }

        private static IJsonSerializerStrategy _currentJsonSerializerStrategy;
        public static IJsonSerializerStrategy CurrentJsonSerializerStrategy {
            get {
                return _currentJsonSerializerStrategy ??
                    (_currentJsonSerializerStrategy =
#if SIMPLE_JSON_UNITY
    UnityJsonSerializerStrategy
//#elif SIMPLE_JSON_DATACONTRACT
// DataContractJsonSerializerStrategy
#else
 PocoJsonSerializerStrategy
#endif
);
            }
            set {
                _currentJsonSerializerStrategy = value;
            }
        }

        private static PocoJsonSerializerStrategy _pocoJsonSerializerStrategy;
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static PocoJsonSerializerStrategy PocoJsonSerializerStrategy {
            get {
                return _pocoJsonSerializerStrategy ?? (_pocoJsonSerializerStrategy = new DefaultJsonSerializationStrategy());

            }
        }
#if SIMPLE_JSON_UNITY
        internal static UnitySerializationStrategy _unityJsonSerializerStrategy;
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Advanced)]
        public static UnitySerializationStrategy UnityJsonSerializerStrategy {
            get {
                return _unityJsonSerializerStrategy ?? (_unityJsonSerializerStrategy = new UnitySerializationStrategy());
            }
        }
        //#elif SIMPLE_JSON_DATACONTRACT

        //        private static DataContractJsonSerializerStrategy _dataContractJsonSerializerStrategy;
        //        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Advanced)]
        //        public static DataContractJsonSerializerStrategy DataContractJsonSerializerStrategy {
        //            get {
        //                return _dataContractJsonSerializerStrategy ?? (_dataContractJsonSerializerStrategy = new DataContractJsonSerializerStrategy());
        //            }
        //        }

#endif

        private static readonly Dictionary<string,Type> _aotTypeRegistry = new Dictionary<string,Type>();
        private static readonly object _registryLock = new object();

        public static void RegisterAotType(string typeName,Type type) {
            if(type == null)
                throw new ArgumentNullException(nameof(type));

            lock(_registryLock) {
                _aotTypeRegistry[typeName] = type;
            }
        }

        public static Type GetRegisteredAotType(string typeName) {
            lock(_registryLock) {
                return _aotTypeRegistry.TryGetValue(typeName,out var type) ? type : null;
            }
        }

        public static void InitializeCommonAotTypes() {
            RegisterAotType("Dictionary<String,Object>",typeof(Dictionary<string,object>));
            RegisterAotType("Dictionary<String,String>",typeof(Dictionary<string,string>));
            RegisterAotType("Dictionary<String,Int32>",typeof(Dictionary<string,int>));
            RegisterAotType("Dictionary<String,Int64>",typeof(Dictionary<string,long>));
            RegisterAotType("Dictionary<String,Boolean>",typeof(Dictionary<string,bool>));
            RegisterAotType("Dictionary<String,Double>",typeof(Dictionary<string,double>));
            RegisterAotType("List<Object>",typeof(List<object>));
            RegisterAotType("List<String>",typeof(List<string>));
            RegisterAotType("List<Int32>",typeof(List<int>));
            RegisterAotType("List<Int64>",typeof(List<long>));
            RegisterAotType("List<Boolean>",typeof(List<bool>));
            RegisterAotType("List<Double>",typeof(List<double>));
        }

        public static bool IsAotEnvironment {
            get {
#if UNITY_EDITOR
                return false;
#elif SIMPLE_JSON_AOT
                return true;
#else
                return false;
#endif
            }
        }

        public static void ClearReflectionCache() {
            if(CurrentJsonSerializerStrategy is PocoJsonSerializerStrategy strategy) {
                strategy.ClearCache();
            }
        }
    }

    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
 interface IJsonSerializerStrategy {
        [SuppressMessage("Microsoft.Design","CA1007:UseGenericsWhereAppropriate",Justification = "Need to support .NET 2")]
        bool TrySerializeNonPrimitiveObject(object input,out object output);
        object DeserializeObject(object value,Type type);
    }
    /// <summary>
    /// base Json Serializer Strategy
    /// </summary>
    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    abstract class PocoJsonSerializerStrategy : IJsonSerializerStrategy {
        internal IDictionary<Type,ReflectionUtils.ConstructorDelegate> ConstructorCache;
        internal IDictionary<Type,IDictionary<string,ReflectionUtils.GetDelegate>> GetCache;
        internal IDictionary<Type,IDictionary<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>>> SetCache;

        internal static readonly Type[] EmptyTypes = new Type[0];
        internal static readonly Type[] ArrayConstructorParameterTypes = new Type[] { typeof(int) };

        private static readonly string[] Iso8601Format = Constants.Iso8601Formats;
        protected bool onlyPublic = true;
        public PocoJsonSerializerStrategy() {
            ConstructorCache = new ReflectionUtils.ThreadSafeDictionary<Type,ReflectionUtils.ConstructorDelegate>(ContructorDelegateFactory);
            GetCache = new ReflectionUtils.ThreadSafeDictionary<Type,IDictionary<string,ReflectionUtils.GetDelegate>>(GetterValueFactory);
            SetCache = new ReflectionUtils.ThreadSafeDictionary<Type,IDictionary<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>>>(SetterValueFactory);
        }

        protected virtual string MapClrMemberNameToJsonFieldName(string clrPropertyName) {
            return clrPropertyName;
        }

        internal virtual ReflectionUtils.ConstructorDelegate ContructorDelegateFactory(Type key) {
            return ReflectionUtils.GetContructor(key,(key.IsArray || ReflectionUtils.IsAssignableFrom(typeof(IList),key)) ? ArrayConstructorParameterTypes : EmptyTypes);
        }

        internal virtual IDictionary<string,ReflectionUtils.GetDelegate> GetterValueFactory(Type type) {
            IDictionary<string,ReflectionUtils.GetDelegate> result = new Dictionary<string,ReflectionUtils.GetDelegate>();
            // This prevents an group of exceptions thrown when a *thrown* exception is caught and serialized into JSON.
            // Throwing adds a properties which cannot be got via the reflective code used and, even if these are bypassed,
            // adds properties which when traversed cause an infinite loop - resulting in a stack overflow.
            if(type.FullName == "System.Reflection.RuntimeMethodInfo" && type.IsSealed && type.IsNotPublic) {
                return result;
            }
            foreach(PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
                if(propertyInfo.CanRead) {
                    MethodInfo getMethod = ReflectionUtils.GetGetterMethodInfo(propertyInfo);
                    if(!CanAddProperty(propertyInfo,getMethod))
                        continue;
                    // JsonAliasAttribute: 序列化时使用别名作为 JSON key
                    JsonAliasAttribute alias = (JsonAliasAttribute)ReflectionUtils.GetAttribute(propertyInfo,typeof(JsonAliasAttribute));
                    string jsonKey = alias != null ? alias.Alias : MapClrMemberNameToJsonFieldName(propertyInfo.Name);
                    result[jsonKey] = ReflectionUtils.GetGetMethod(propertyInfo);
                }
            }
            foreach(FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
                if(!CanAddField(fieldInfo))
                    continue;
                // JsonAliasAttribute: 序列化时使用别名作为 JSON key
                JsonAliasAttribute alias = (JsonAliasAttribute)ReflectionUtils.GetAttribute(fieldInfo,typeof(JsonAliasAttribute));
                string jsonKey = alias != null ? alias.Alias : MapClrMemberNameToJsonFieldName(fieldInfo.Name);
                result[jsonKey] = ReflectionUtils.GetGetMethod(fieldInfo);
            }
            return result;
        }

        internal virtual IDictionary<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>> SetterValueFactory(Type type) {
            IDictionary<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>> result = new Dictionary<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>>();
            foreach(PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
                if(propertyInfo.CanWrite) {
                    MethodInfo setMethod = ReflectionUtils.GetSetterMethodInfo(propertyInfo);
                    if(!CanAddProperty(propertyInfo,setMethod))
                        continue;
                    var setter = new KeyValuePair<Type,ReflectionUtils.SetDelegate>(propertyInfo.PropertyType,ReflectionUtils.GetSetMethod(propertyInfo));
                    // JsonAliasAttribute: 反序列化时使用别名作为 JSON key
                    JsonAliasAttribute alias = (JsonAliasAttribute)ReflectionUtils.GetAttribute(propertyInfo,typeof(JsonAliasAttribute));
                    if(alias != null) {
                        result[alias.Alias] = setter;
                        // AcceptOriginal: 同时也接受原始属性名（camelCase/原名）
                        if(alias.AcceptOriginal)
                            result[MapClrMemberNameToJsonFieldName(propertyInfo.Name)] = setter;
                    }
                    else {
                        result[MapClrMemberNameToJsonFieldName(propertyInfo.Name)] = setter;
                    }
                }
            }
            foreach(FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
                if(fieldInfo.IsInitOnly || !CanAddField(fieldInfo))
                    continue;
                var setter = new KeyValuePair<Type,ReflectionUtils.SetDelegate>(fieldInfo.FieldType,ReflectionUtils.GetSetMethod(fieldInfo));
                // JsonAliasAttribute: 反序列化时使用别名作为 JSON key
                JsonAliasAttribute alias = (JsonAliasAttribute)ReflectionUtils.GetAttribute(fieldInfo,typeof(JsonAliasAttribute));
                if(alias != null) {
                    result[alias.Alias] = setter;
                    // AcceptOriginal: 同时也接受原始字段名
                    if(alias.AcceptOriginal)
                        result[MapClrMemberNameToJsonFieldName(fieldInfo.Name)] = setter;
                }
                else {
                    result[MapClrMemberNameToJsonFieldName(fieldInfo.Name)] = setter;
                }
            }
            return result;
        }

        protected virtual bool CanAddField(FieldInfo field) {
            if(field.IsStatic)
                return false;
            if(ReflectionUtils.GetAttribute(field,typeof(JsonIgnoreAttribute)) != null)
                return false;
            if(ReflectionUtils.GetAttribute(field,typeof(CompilerGeneratedAttribute)) != null)
                return false;
            //sgd
            if(ReflectionUtils.GetAttribute(field,typeof(JsonIncludeAttribute)) != null) {
                //sgd:jsoninclude֧ 
                return true;
            }
            else if(onlyPublic && field.IsPublic == false) {
                //sgd:only public
                return false;
            }
            return true;
        }

        protected virtual bool CanAddProperty(PropertyInfo property,MethodInfo method) {
            if(method.IsStatic)
                return false;
            if(ReflectionUtils.GetAttribute(property,typeof(JsonIgnoreAttribute)) != null)
                return false;
            // if this is an indexed getter (i.e. this[int] property getter), skip it,
            // since it's a helper-only type getter for C# syntactic sugar
#if NET35 || NET40
             if (property.GetGetMethod().GetParameters().Length > 0){
                return false;
             }
#else
            if(property.GetMethod.GetParameters().Length > 0)
                return false;
#endif
            //sgd
            if(method.IsPublic) {
                //sgd:public method
                return true;
            }
            if(ReflectionUtils.GetAttribute(property,typeof(JsonIncludeAttribute)) != null) {
                //sgd:json include
                return true;
            }
            else if(onlyPublic && method.Name.StartsWith("get_")) {
                //sgd:only public
                // getter:
                return false;
            }

            return true;
        }

        public virtual bool TrySerializeNonPrimitiveObject(object input,out object output) {
            if(input == null) {
                output = null;
                return false;
            }
            return TrySerializeKnownTypes(input,out output) || TrySerializeUnknownTypes(input,out output);
        }

        [SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity")]
        public virtual object DeserializeObject(object value,Type type) {
            if(type == null) throw new ArgumentNullException("type");
            string str = value as string;

            if(type == typeof(Guid) && string.IsNullOrEmpty(str))
                return default(Guid);

            if(value == null)
                return null;

            object obj = null;

            if(str != null) {
                if(str.Length != 0) // We know it can't be null now.
                {
                    //if (type == typeof(NPath) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(NPath)))
                    //    return new NPath(str);
                    if(type == typeof(DateTime) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(DateTime))) {
                        foreach(var format in Iso8601Format) {
                            try {
                                return DateTime.ParseExact(str,format,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                            }
                            catch(FormatException) {
                                continue;
                            }
                        }
                        throw new FormatException($"DateTime string '{str}' was not recognized as a valid DateTime.");
                    }
                    if(type == typeof(DateTimeOffset) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(DateTimeOffset))) {
                        foreach(var format in Iso8601Format) {
                            try {
                                return DateTimeOffset.ParseExact(str,format,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                            }
                            catch(FormatException) {
                                continue;
                            }
                        }
                        throw new FormatException($"DateTimeOffset string '{str}' was not recognized as a valid DateTimeOffset.");
                    }
                    // 添加 TimeSpan 反序列化支持
                    if(type == typeof(TimeSpan) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(TimeSpan))) {
                        if(long.TryParse(str,out long ticks))
                            return new TimeSpan(ticks);
                        return TimeSpan.Parse(str,CultureInfo.InvariantCulture);
                    }
                    // 添加 char 反序列化支持
                    if(type == typeof(char) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(char))) {
                        if(str.Length == 1)
                            return str[0];
                        else if(str.Length > 0)
                            return str[0];
                        return default(char);
                    }
                    if(type == typeof(Guid) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid)))
                        return new Guid(str);
                    //if (type == typeof(UriString) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(UriString)))
                    //    return new UriString(str);
                    if(type == typeof(Uri)) {
                        bool isValid = Uri.IsWellFormedUriString(str,UriKind.RelativeOrAbsolute);

                        Uri result;
                        if(isValid && Uri.TryCreate(str,UriKind.RelativeOrAbsolute,out result))
                            return result;

                        return null;
                    }

                    if(type == typeof(string))
                        return str;

                    if(type.IsEnum) {
                        if(double.TryParse(str,out double enumValue)) {
                            return Enum.ToObject(type,Convert.ChangeType(enumValue,Enum.GetUnderlyingType(type),CultureInfo.InvariantCulture));
                        }
                        return Enum.Parse(type,str,true);
                    }

                    return Convert.ChangeType(str,type,CultureInfo.InvariantCulture);
                }
                else {
                    if(type == typeof(Guid))
                        obj = default(Guid);
                    else if(ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid))
                        obj = null;
                    else
                        obj = str;
                }
                // Empty string case
                if(!ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid))
                    return str;
            }
            else if(value is bool)
                return value;

            bool valueIsLong = value is long;
            bool valueIsULong = value is ulong;
            bool valueIsDouble = value is double;
            if((valueIsLong && type == typeof(long)) || (valueIsULong && type == typeof(ulong)) || (valueIsDouble && type == typeof(double)))
                return value;
            if((valueIsDouble && type != typeof(double)) || (valueIsULong && type != typeof(ulong)) || (valueIsLong && type != typeof(long))) {
                if(type.IsEnum) {
                    obj = Enum.ToObject(type,Convert.ChangeType(value,Enum.GetUnderlyingType(type),CultureInfo.InvariantCulture));
                }
                else {
                    obj = type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(double) || type == typeof(float) || type == typeof(bool) || type == typeof(decimal) || type == typeof(byte) || type == typeof(short)
                                ? Convert.ChangeType(value,type,CultureInfo.InvariantCulture)
                                : value;
                }
            }
            else {
                IDictionary<string,object> objects = value as IDictionary<string,object>;
                if(objects != null) {
                    IDictionary<string,object> jsonObject = objects;

                    if(ReflectionUtils.IsTypeDictionary(type)) {
                        // if dictionary then
                        Type[] types = ReflectionUtils.GetGenericTypeArguments(type);
                        Type keyType = types[0];
                        Type valueType = types[1];

                        IDictionary dict;
#if UNITY_EDITOR || !SIMPLE_JSON_AOT
                        Type genericType = typeof(Dictionary<,>).MakeGenericType(keyType,valueType);
                        dict = (IDictionary)ConstructorCache[genericType]();
#else
                        dict = CreateDictionaryForAot(keyType,valueType);
#endif

                        foreach(KeyValuePair<string,object> kvp in jsonObject)
                            dict.Add(kvp.Key,DeserializeObject(kvp.Value,valueType));

                        obj = dict;
                    }
                    else {
                        if(type == typeof(object))
                            obj = value;
                        else {
                            obj = ConstructorCache[type]();
                            // 构建一个大小写不敏感的 JSON key 查找表，用于兜底匹配
                            // SetCache 中的 key 是经过 MapClrMemberNameToJsonFieldName 转换后的名字（可能是 camelCase）
                            // 而 JSON 中的 key 可能大小写不一致，TryGetValue 是精确匹配，故需要兜底
                            IDictionary<string,object> jsonObjInsensitive = null;
                            foreach(KeyValuePair<string,KeyValuePair<Type,ReflectionUtils.SetDelegate>> setter in SetCache[type]) {
                                object jsonValue;
                                if(jsonObject.TryGetValue(setter.Key,out jsonValue)) {
                                    jsonValue = DeserializeObject(jsonValue,setter.Value.Key);
                                    setter.Value.Value(obj,jsonValue);
                                }
                                else {
                                    // 大小写不敏感兜底：延迟构建，避免每次都创建
                                    if(jsonObjInsensitive == null) {
                                        jsonObjInsensitive = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                                        foreach(var kv in jsonObject)
                                            if(!jsonObjInsensitive.ContainsKey(kv.Key))
                                                jsonObjInsensitive[kv.Key] = kv.Value;
                                    }
                                    if(jsonObjInsensitive.TryGetValue(setter.Key,out jsonValue)) {
                                        jsonValue = DeserializeObject(jsonValue,setter.Value.Key);
                                        setter.Value.Value(obj,jsonValue);
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    IList<object> valueAsList = value as IList<object>;
                    if(valueAsList != null) {
                        IList<object> jsonObject = valueAsList;
                        IList list = null;

                        if(type.IsArray) {
                            list = (IList)ConstructorCache[type](jsonObject.Count);
                            int i = 0;
                            foreach(object o in jsonObject)
                                list[i++] = DeserializeObject(o,type.GetElementType());
                        }
                        else if(ReflectionUtils.IsTypeGenericeCollectionInterface(type) || ReflectionUtils.IsAssignableFrom(typeof(IList),type)) {
                            Type innerType = ReflectionUtils.GetGenericListElementType(type);
#if UNITY_EDITOR || !SIMPLE_JSON_AOT
                            list = (IList)(ConstructorCache[type] ?? ConstructorCache[typeof(List<>).MakeGenericType(innerType)])(jsonObject.Count);
#else
                            list = CreateListForAot(innerType,jsonObject.Count);
#endif
                            foreach(object o in jsonObject)
                                list.Add(DeserializeObject(o,innerType));
                        }
                        obj = list;
                    }
                }
                return obj;
            }
            if(ReflectionUtils.IsNullableType(type))
                return ReflectionUtils.ToNullableType(obj,type);
            return obj;
        }

        protected virtual object SerializeEnum(Enum p) {
            return Convert.ToInt64(p,CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Microsoft.Design","CA1007:UseGenericsWhereAppropriate",Justification = "Need to support .NET 2")]
        protected virtual bool TrySerializeKnownTypes(object input,out object output) {
            if(input == null) {
                output = null;
                return false;
            }
            Type inputType = input.GetType();
            if(ReflectionUtils.IsNullableType(inputType)) {
                inputType = Nullable.GetUnderlyingType(inputType);
            }
            bool returnValue = true;
            //if (input is NPath || input is UriString)
            //    output = input.ToString();
            if(input is DateTime)
                output = ((DateTime)input).ToUniversalTime().ToString(Iso8601Format[0],CultureInfo.InvariantCulture);
            else if(input is DateTimeOffset)
                output = ((DateTimeOffset)input).ToUniversalTime().ToString(Iso8601Format[0],CultureInfo.InvariantCulture);
            // 添加 TimeSpan 支持
            else if(input is TimeSpan)
                output = ((TimeSpan)input).Ticks.ToString(CultureInfo.InvariantCulture);
            else if(input is Guid)
                output = ((Guid)input).ToString("D");
            else if(input is Uri)
                output = input.ToString();
            else {
                Enum inputEnum = input as Enum;
                if(inputEnum != null)
                    output = SerializeEnum(inputEnum);
                else {
                    returnValue = false;
                    output = null;
                }
            }
            return returnValue;
        }
        [SuppressMessage("Microsoft.Design","CA1007:UseGenericsWhereAppropriate",Justification = "Need to support .NET 2")]
        protected virtual bool TrySerializeUnknownTypes(object input,out object output) {
            if(input == null) throw new ArgumentNullException("input");
            output = null;
            Type type = input.GetType();
            if(type.FullName == null)
                return false;
            IDictionary<string,object> obj = new JsonObject();//
            //sgd:There might be a problem with directly ignoring case here. 直接在这里忽略大小写可能存在问题
            //IDictionary<string,object> obj = new JsonObject(StringComparer.OrdinalIgnoreCase);
            IDictionary<string,ReflectionUtils.GetDelegate> getters = GetCache[type];
            if(getters == null)
                return false;
            foreach(KeyValuePair<string,ReflectionUtils.GetDelegate> getter in getters) {
                if(getter.Value != null)
                    obj.Add(MapClrMemberNameToJsonFieldName(getter.Key),getter.Value(input));
            }
            output = obj;
            return true;
        }

        public void ClearCache() {
            ConstructorCache.Clear();
            GetCache.Clear();
            SetCache.Clear();
        }

        private static IDictionary CreateDictionaryForAot(Type keyType,Type valueType) {
            // 添加更多类型组合以提高 AOT 兼容性
            if(keyType == typeof(string) && valueType == typeof(object))
                return new Dictionary<string,object>();
            if(keyType == typeof(string) && valueType == typeof(string))
                return new Dictionary<string,string>();
            if(keyType == typeof(string) && valueType == typeof(int))
                return new Dictionary<string,int>();
            if(keyType == typeof(string) && valueType == typeof(long))
                return new Dictionary<string,long>();
            if(keyType == typeof(string) && valueType == typeof(bool))
                return new Dictionary<string,bool>();
            if(keyType == typeof(string) && valueType == typeof(double))
                return new Dictionary<string,double>();
            if(keyType == typeof(string) && valueType == typeof(float))
                return new Dictionary<string,float>();
            if(keyType == typeof(string) && valueType == typeof(decimal))
                return new Dictionary<string,decimal>();
            if(keyType == typeof(string) && valueType == typeof(uint))
                return new Dictionary<string,uint>();
            if(keyType == typeof(string) && valueType == typeof(byte))
                return new Dictionary<string,byte>();
            if(keyType == typeof(string) && valueType == typeof(short))
                return new Dictionary<string,short>();
            if(keyType == typeof(string) && valueType == typeof(sbyte))
                return new Dictionary<string,sbyte>();
            if(keyType == typeof(string) && valueType == typeof(ushort))
                return new Dictionary<string,ushort>();
            if(keyType == typeof(string) && valueType == typeof(char))
                return new Dictionary<string,char>();
            if(keyType == typeof(string) && valueType == typeof(DateTime))
                return new Dictionary<string,DateTime>();
            if(keyType == typeof(string) && valueType == typeof(Guid))
                return new Dictionary<string,Guid>();

            // 首先检查注册的AOT类型
            string typeName = $"Dictionary<{keyType.Name},{valueType.Name}>";
            Type registeredType = SimpleJson.GetRegisteredAotType(typeName);
            if(registeredType != null) {
                try {
                    return (IDictionary)Activator.CreateInstance(registeredType);
                }
                catch {
                    try {
                        return (IDictionary)FormatterServices.GetUninitializedObject(registeredType);
                    }
                    catch {
                    }
                }
            }

            Type genericType = typeof(Dictionary<,>).MakeGenericType(keyType,valueType);
            try {
                return (IDictionary)Activator.CreateInstance(genericType);
            }
            catch {
                try {
                    return (IDictionary)FormatterServices.GetUninitializedObject(genericType);
                }
                catch {
                    throw new NotSupportedException(
                        $"Dictionary<{keyType.Name}, {valueType.Name}> is not supported in AOT environment. " +
                        $"Please register this type using SimpleJson.RegisterAotType().");
                }
            }
        }

        private static IList CreateListForAot(Type elementType,int capacity) {
            //添加更多类型以提高 AOT 兼容性
            if(elementType == typeof(object))
                return new List<object>(capacity);
            if(elementType == typeof(string))
                return new List<string>(capacity);
            if(elementType == typeof(int))
                return new List<int>(capacity);
            if(elementType == typeof(long))
                return new List<long>(capacity);
            if(elementType == typeof(bool))
                return new List<bool>(capacity);
            if(elementType == typeof(double))
                return new List<double>(capacity);
            if(elementType == typeof(float))
                return new List<float>(capacity);
            if(elementType == typeof(byte))
                return new List<byte>(capacity);
            if(elementType == typeof(short))
                return new List<short>(capacity);
            if(elementType == typeof(uint))
                return new List<uint>(capacity);
            if(elementType == typeof(decimal))
                return new List<decimal>(capacity);
            if(elementType == typeof(sbyte))
                return new List<sbyte>(capacity);
            if(elementType == typeof(ushort))
                return new List<ushort>(capacity);
            if(elementType == typeof(char))
                return new List<char>(capacity);
            if(elementType == typeof(DateTime))
                return new List<DateTime>(capacity);
            if(elementType == typeof(Guid))
                return new List<Guid>(capacity);

            // 首先检查注册的AOT类型
            string typeName = $"List<{elementType.Name}>";
            Type registeredType = SimpleJson.GetRegisteredAotType(typeName);
            if(registeredType != null) {
                try {
                    return (IList)Activator.CreateInstance(registeredType,capacity);
                }
                catch {
                    try {
                        return (IList)FormatterServices.GetUninitializedObject(registeredType);
                    }
                    catch {
                    }
                }
            }

            Type genericType = typeof(List<>).MakeGenericType(elementType);
            try {
                return (IList)Activator.CreateInstance(genericType,capacity);
            }
            catch {
                try {
                    return (IList)FormatterServices.GetUninitializedObject(genericType);
                }
                catch {
                    throw new NotSupportedException(
                        $"List<{elementType.Name}> is not supported in AOT environment. " +
                        $"Please register this type using SimpleJson.RegisterAotType().");
                }
            }
        }
    }

#if SIMPLE_JSON_DATACONTRACT
//    [GeneratedCode("simple-json", "1.0.0")]
//#if SIMPLE_JSON_INTERNAL
//    internal
//#else
//    public
//#endif
// class DataContractJsonSerializerStrategy : PocoJsonSerializerStrategy {
//        public DataContractJsonSerializerStrategy() {
//            GetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, ReflectionUtils.GetDelegate>>(GetterValueFactory);
//            SetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>>(SetterValueFactory);
//        }

//        internal override IDictionary<string, ReflectionUtils.GetDelegate> GetterValueFactory(Type type) {
//            bool hasDataContract = ReflectionUtils.GetAttribute(type, typeof(DataContractAttribute)) != null;
//            if (!hasDataContract)
//                return base.GetterValueFactory(type);
//            string jsonKey;
//            IDictionary<string, ReflectionUtils.GetDelegate> result = new Dictionary<string, ReflectionUtils.GetDelegate>();
//            foreach (PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
//                if (propertyInfo.CanRead) {
//                    MethodInfo getMethod = ReflectionUtils.GetGetterMethodInfo(propertyInfo);
//                    if (!getMethod.IsStatic && CanAdd(propertyInfo, out jsonKey))
//                        result[jsonKey] = ReflectionUtils.GetGetMethod(propertyInfo);
//                }
//            }
//            foreach (FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
//                if (!fieldInfo.IsStatic && CanAdd(fieldInfo, out jsonKey))
//                    result[jsonKey] = ReflectionUtils.GetGetMethod(fieldInfo);
//            }
//            return result;
//        }

//        internal override IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> SetterValueFactory(Type type) {
//            bool hasDataContract = ReflectionUtils.GetAttribute(type, typeof(DataContractAttribute)) != null;
//            if (!hasDataContract)
//                return base.SetterValueFactory(type);
//            string jsonKey;
//            IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> result = new Dictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>();
//            foreach (PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
//                if (propertyInfo.CanWrite) {
//                    MethodInfo setMethod = ReflectionUtils.GetSetterMethodInfo(propertyInfo);
//                    if (!setMethod.IsStatic && CanAdd(propertyInfo, out jsonKey))
//                        result[jsonKey] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(propertyInfo.PropertyType, ReflectionUtils.GetSetMethod(propertyInfo));
//                }
//            }
//            foreach (FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
//                if (!fieldInfo.IsInitOnly && !fieldInfo.IsStatic && CanAdd(fieldInfo, out jsonKey))
//                    result[jsonKey] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(fieldInfo.FieldType, ReflectionUtils.GetSetMethod(fieldInfo));
//            }
//            // todo implement sorting for DATACONTRACT.
//            return result;
//        }

//        private static bool CanAdd(MemberInfo info, out string jsonKey) {
//            jsonKey = null;
//            if (ReflectionUtils.GetAttribute(info, typeof(IgnoreDataMemberAttribute)) != null)
//                return false;
//            DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)ReflectionUtils.GetAttribute(info, typeof(DataMemberAttribute));
//            if (dataMemberAttribute == null)
//                return false;
//            jsonKey = string.IsNullOrEmpty(dataMemberAttribute.Name) ? info.Name : dataMemberAttribute.Name;
//            return true;
//        }
//    }

#endif

    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
        class DefaultJsonSerializationStrategy : PocoJsonSerializerStrategy {
        private bool toLowerCase = false;
        //private bool onlyPublic = true;
        internal DefaultJsonSerializationStrategy() :
#if SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE&&SIMPLE_JSON_OnlyPublicProperty
            this(true,true)
#elif SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE && !SIMPLE_JSON_OnlyPublicProperty
            this(true,false)

#elif !SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE && !SIMPLE_JSON_OnlyPublicProperty
            this(false,false)
#else
            this(false,true)
#endif
        {
        }
        public DefaultJsonSerializationStrategy(bool toLowerCase,bool onlyPublic) {
            this.toLowerCase = toLowerCase;
            this.onlyPublic = onlyPublic;
            //sgd:support DataContractAttribute/IgnoreDataMemberAttribute/DataMemberAttribute֧

#if SIMPLE_JSON_DATACONTRACT
                GetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, ReflectionUtils.GetDelegate>>(GetterValueFactory);
                SetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>>(SetterValueFactory);
#endif
        }
        #region DataContractAttribute/IgnoreDataMemberAttribute/DataMemberAttribute֧

#if SIMPLE_JSON_DATACONTRACT
            internal override IDictionary<string, ReflectionUtils.GetDelegate> GetterValueFactory(Type type) {
                bool hasDataContract = ReflectionUtils.GetAttribute(type, typeof(DataContractAttribute)) != null;
                if(!hasDataContract)
                    return base.GetterValueFactory(type);
                string jsonKey;
                IDictionary<string, ReflectionUtils.GetDelegate> result = new Dictionary<string, ReflectionUtils.GetDelegate>();
                foreach(PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
                    if(propertyInfo.CanRead) {
                        MethodInfo getMethod = ReflectionUtils.GetGetterMethodInfo(propertyInfo);
                        if(!getMethod.IsStatic && CanAdd(propertyInfo, out jsonKey))
                            result[jsonKey] = ReflectionUtils.GetGetMethod(propertyInfo);
                    }
                }
                foreach(FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
                    if(!fieldInfo.IsStatic && CanAdd(fieldInfo, out jsonKey))
                        result[jsonKey] = ReflectionUtils.GetGetMethod(fieldInfo);
                }
                return result;
            }

            internal override IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> SetterValueFactory(Type type) {
                bool hasDataContract = ReflectionUtils.GetAttribute(type, typeof(DataContractAttribute)) != null;
                if(!hasDataContract)
                    return base.SetterValueFactory(type);
                string jsonKey;
                IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> result = new Dictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>();
                foreach(PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type)) {
                    if(propertyInfo.CanWrite) {
                        MethodInfo setMethod = ReflectionUtils.GetSetterMethodInfo(propertyInfo);
                        if(!setMethod.IsStatic && CanAdd(propertyInfo, out jsonKey))
                            result[jsonKey] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(propertyInfo.PropertyType, ReflectionUtils.GetSetMethod(propertyInfo));
                    }
                }
                foreach(FieldInfo fieldInfo in ReflectionUtils.GetFields(type)) {
                    if(!fieldInfo.IsInitOnly && !fieldInfo.IsStatic && CanAdd(fieldInfo, out jsonKey))
                        result[jsonKey] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(fieldInfo.FieldType, ReflectionUtils.GetSetMethod(fieldInfo));
                }
                // todo implement sorting for DATACONTRACT.
                return result;
            }

            private static bool CanAdd(MemberInfo info, out string jsonKey) {
                jsonKey = null;
                if(ReflectionUtils.GetAttribute(info, typeof(IgnoreDataMemberAttribute)) != null)
                    return false;
                DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)ReflectionUtils.GetAttribute(info, typeof(DataMemberAttribute));
                if(dataMemberAttribute == null)
                    return false;
                jsonKey = string.IsNullOrEmpty(dataMemberAttribute.Name) ? info.Name : dataMemberAttribute.Name;
                return true;
            }
#endif
        #endregion
        //protected override bool CanAddField(FieldInfo field) {
        //    var canAdd = base.CanAddField(field);
        //    return canAdd && ((onlyPublic && field.IsPublic) || !onlyPublic);
        //}

        //protected override bool CanAddProperty(PropertyInfo property, MethodInfo method) {
        //    var canAdd = base.CanAddProperty(property, method);
        //    if(!canAdd)
        //        return false;

        //    // we always serialize public things
        //    if(method.IsPublic)
        //        return true;

        //    // if the getter is private and we're only serializing public things, skip this property
        //    if(onlyPublic && method.Name.StartsWith("get_"))
        //        return false;

        //    return true;
        //}

        protected override string MapClrMemberNameToJsonFieldName(string clrPropertyName) {
            if(!toLowerCase)
                return base.MapClrMemberNameToJsonFieldName(clrPropertyName);
            return ToJsonPropertyName(clrPropertyName);
        }
        /// <summary>
        /// Convert from PascalCase to camelCase.
        /// </summary>
        private string ToJsonPropertyName(string propertyName) {
            Guard.ArgumentNotNullOrWhiteSpace(propertyName,"propertyName");
            int i = 0;
            while(i < propertyName.Length && char.IsUpper(propertyName[i]))
                i++;
            return propertyName.Substring(0,i).ToLowerInvariant() + propertyName.Substring(i);
        }
    }

#if SIMPLE_JSON_UNITY
    [GeneratedCode("simple-json","1.0.0")]
#if SIMPLE_JSON_INTERNAL
    internal
#else
    public
#endif
    class UnitySerializationStrategy : PocoJsonSerializerStrategy {
        private bool toLowerCase = false;
        //private bool onlyPublic = true;

        internal UnitySerializationStrategy() :
#if SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE&&SIMPLE_JSON_OnlyPublicProperty
            this(true,true)
#elif SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE && !SIMPLE_JSON_OnlyPublicProperty
            this(true,false)

#elif !SIMPLE_JSON_PFPARSE_IGNORE_LOWERCASE && !SIMPLE_JSON_OnlyPublicProperty
            this(false,false)
#else
            this(false,true)
#endif
        {
        }
        public UnitySerializationStrategy(bool toLowerCase,bool onlyPublic) {
            this.toLowerCase = toLowerCase;
            this.onlyPublic = onlyPublic;
        }

        protected override bool TrySerializeKnownTypes(object input,out object output) {
            //support Unity.Mathematics               
            if(input.GetType().FullName.StartsWith("UnityEngine") ||
                input.GetType().FullName.StartsWith("Unity.")) {
                output = JsonUtility.ToJson(input);
                return true;
            }
            return base.TrySerializeKnownTypes(input,out output);
        }
        public override object DeserializeObject(object value,Type type) {
            string str = value as string;
            // BUG FIX: 原代码运算符优先级错误，&& 优先于 ||，导致条件等同于：
            // (!string.IsNullOrEmpty(str) && type.FullName.StartsWith("UnityEngine")) || type.FullName.StartsWith("Unity.")
            // 当 type 以 "Unity." 开头时，无论 value 是否为字符串，都会调用 JsonUtility.FromJson(str, type)，
            // 此时 str 可能为 null，导致 JsonUtility.FromJson 抛出异常或返回错误结果。
            if((type.FullName.StartsWith("UnityEngine") ||
                 type.FullName.StartsWith("Unity.")) &&
                !string.IsNullOrEmpty(str)) {
                return JsonUtility.FromJson(str,type);
            }
            return base.DeserializeObject(value,type);
        }
        protected override string MapClrMemberNameToJsonFieldName(string clrPropertyName) {
            if(!toLowerCase)
                return base.MapClrMemberNameToJsonFieldName(clrPropertyName);
            return ToJsonPropertyName(clrPropertyName);
        }
        /// <summary>
        /// Convert from PascalCase to camelCase.
        /// </summary>
        private string ToJsonPropertyName(string propertyName) {
            Guard.ArgumentNotNullOrWhiteSpace(propertyName,"propertyName");
            int i = 0;
            while(i < propertyName.Length && char.IsUpper(propertyName[i]))
                i++;
            return propertyName.Substring(0,i).ToLowerInvariant() + propertyName.Substring(i);
        }
    }
#endif



    // This class is meant to be copied into other libraries. So we want to exclude it from Code Analysis rules
    // that might be in place in the target project.
    [GeneratedCode("reflection-utils","1.0.0")]
#if SIMPLE_JSON_REFLECTION_UTILS_PUBLIC
        public
#else
    internal
#endif
    class ReflectionUtils {
        private static readonly object[] EmptyObjects = new object[] { };

        public delegate object GetDelegate(object source);
        public delegate void SetDelegate(object source,object value);
        public delegate object ConstructorDelegate(params object[] args);

        public delegate TValue ThreadSafeDictionaryValueFactory<TKey, TValue>(TKey key);

#if SIMPLE_JSON_TYPEINFO
            public static TypeInfo GetTypeInfo(Type type)
            {
                return type.GetTypeInfo();
            }
#else
        public static Type GetTypeInfo(Type type) {
            return type;
        }
#endif

        public static Attribute GetAttribute(MemberInfo info,Type type) {
#if SIMPLE_JSON_TYPEINFO
                if (info == null || type == null || !info.IsDefined(type))
                    return null;
                return info.GetCustomAttribute(type);
#else
            if(info == null || type == null || !Attribute.IsDefined(info,type))
                return null;
            return Attribute.GetCustomAttribute(info,type);
#endif
        }

        public static Type GetGenericListElementType(Type type) {
            IEnumerable<Type> interfaces;
#if SIMPLE_JSON_TYPEINFO
                interfaces = type.GetTypeInfo().ImplementedInterfaces;
#else
            interfaces = type.GetInterfaces();
#endif
            foreach(Type implementedInterface in interfaces) {
                if(IsTypeGeneric(implementedInterface) &&
                    implementedInterface.GetGenericTypeDefinition() == typeof(IList<>)) {
                    return GetGenericTypeArguments(implementedInterface)[0];
                }
            }
            return GetGenericTypeArguments(type)[0];
        }

        public static Attribute GetAttribute(Type objectType,Type attributeType) {

#if SIMPLE_JSON_TYPEINFO
                if (objectType == null || attributeType == null || !objectType.GetTypeInfo().IsDefined(attributeType))
                    return null;
                return objectType.GetTypeInfo().GetCustomAttribute(attributeType);
#else
            if(objectType == null || attributeType == null || !Attribute.IsDefined(objectType,attributeType))
                return null;
            return Attribute.GetCustomAttribute(objectType,attributeType);
#endif
        }

        public static Type[] GetGenericTypeArguments(Type type) {
#if SIMPLE_JSON_TYPEINFO
                return type.GetTypeInfo().GenericTypeArguments;
#else
            return type.GetGenericArguments();
#endif
        }

        public static bool IsTypeGeneric(Type type) {
            return GetTypeInfo(type).IsGenericType;
        }

        public static bool IsTypeGenericeCollectionInterface(Type type) {
            if(!IsTypeGeneric(type))
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

        public static bool IsAssignableFrom(Type type1,Type type2) {
            return GetTypeInfo(type1).IsAssignableFrom(GetTypeInfo(type2));
        }

        public static bool IsTypeDictionary(Type type) {
#if SIMPLE_JSON_TYPEINFO
                if (typeof(IDictionary<,>).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    return true;
#else
            if(typeof(System.Collections.IDictionary).IsAssignableFrom(type))
                return true;
#endif
            if(!GetTypeInfo(type).IsGenericType)
                return false;

            if(GetTypeInfo(type).IsInterface) {
                Type interfaceDefinition = GetTypeInfo(type).GetGenericTypeDefinition();
                if(interfaceDefinition == typeof(IDictionary<,>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                    || interfaceDefinition == typeof(IReadOnlyDictionary<,>)
#endif
                ) {
                    return true;
                }
            }

            IEnumerable<Type> interfaces;
#if SIMPLE_JSON_TYPEINFO
                interfaces = type.GetTypeInfo().ImplementedInterfaces;
#else
            interfaces = type.GetInterfaces();
#endif
            foreach(Type i in interfaces) {
                if(IsTypeGeneric(i)) {
                    Type interfaceDefinition = GetTypeInfo(i).GetGenericTypeDefinition();
                    if(interfaceDefinition == typeof(IDictionary<,>)
#if SIMPLE_JSON_READONLY_COLLECTIONS
                        || interfaceDefinition == typeof(IReadOnlyDictionary<,>)
#endif
                    ) {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsNullableType(Type type) {
            return GetTypeInfo(type).IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static object ToNullableType(object obj,Type nullableType) {
            return obj == null ? null : Convert.ChangeType(obj,Nullable.GetUnderlyingType(nullableType),CultureInfo.InvariantCulture);
        }

        public static bool IsValueType(Type type) {
            return GetTypeInfo(type).IsValueType;
        }

        public static IEnumerable<ConstructorInfo> GetConstructors(Type type) {
#if SIMPLE_JSON_TYPEINFO
                return type.GetTypeInfo().DeclaredConstructors;
#else
            return type.GetConstructors();
#endif
        }

        public static ConstructorInfo GetConstructorInfo(Type type,params Type[] argsType) {
            IEnumerable<ConstructorInfo> constructorInfos = GetConstructors(type);
            int i;
            bool matches;
            foreach(ConstructorInfo constructorInfo in constructorInfos) {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if(argsType.Length != parameters.Length)
                    continue;

                i = 0;
                matches = true;
                foreach(ParameterInfo parameterInfo in constructorInfo.GetParameters()) {
                    if(parameterInfo.ParameterType != argsType[i]) {
                        matches = false;
                        break;
                    }
                }

                if(matches)
                    return constructorInfo;
            }

            return null;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type) {
#if SIMPLE_JSON_TYPEINFO
                return type.GetRuntimeProperties();
#else
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static IEnumerable<FieldInfo> GetFields(Type type) {
#if SIMPLE_JSON_TYPEINFO
                return type.GetRuntimeFields();
#else
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static MethodInfo GetGetterMethodInfo(PropertyInfo propertyInfo) {
#if SIMPLE_JSON_TYPEINFO
                return propertyInfo.GetMethod;
#else
            return propertyInfo.GetGetMethod(true);
#endif
        }

        public static MethodInfo GetSetterMethodInfo(PropertyInfo propertyInfo) {
#if SIMPLE_JSON_TYPEINFO
                return propertyInfo.SetMethod;
#else
            return propertyInfo.GetSetMethod(true);
#endif
        }

        public static ConstructorDelegate GetContructor(ConstructorInfo constructorInfo) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetConstructorByReflection(constructorInfo);
#else
                return GetConstructorByExpression(constructorInfo);
#endif
        }

        public static ConstructorDelegate GetContructor(Type type,params Type[] argsType) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetConstructorByReflection(type,argsType);
#else
                return GetConstructorByExpression(type, argsType);
#endif
        }

        public static ConstructorDelegate GetConstructorByReflection(ConstructorInfo constructorInfo) {
            return delegate (object[] args) { return constructorInfo.Invoke(args); };
        }

        public static ConstructorDelegate GetConstructorByReflection(Type type,params Type[] argsType) {
            ConstructorInfo constructorInfo = GetConstructorInfo(type,argsType);
            // if it's a value type (i.e., struct), it won't have a default constructor, so use Activator instead
            return constructorInfo == null ? (type.IsValueType ? GetConstructorForValueType(type) : null) : GetConstructorByReflection(constructorInfo);
        }

        static ConstructorDelegate GetConstructorForValueType(Type type) {
            return delegate (object[] args) {
#if UNITY_EDITOR || !SIMPLE_JSON_AOT
                return Activator.CreateInstance(type);
#else
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch
                {
                    return FormatterServices.GetUninitializedObject(type);
                }
#endif
            };
        }

#if !SIMPLE_JSON_NO_LINQ_EXPRESSION

            public static ConstructorDelegate GetConstructorByExpression(ConstructorInfo constructorInfo)
            {
                ParameterInfo[] paramsInfo = constructorInfo.GetParameters();
                ParameterExpression param = Expression.Parameter(typeof(object[]), "args");
                Expression[] argsExp = new Expression[paramsInfo.Length];
                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Expression index = Expression.Constant(i);
                    Type paramType = paramsInfo[i].ParameterType;
                    Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                    Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                    argsExp[i] = paramCastExp;
                }
                NewExpression newExp = Expression.New(constructorInfo, argsExp);
                Expression<Func<object[], object>> lambda = Expression.Lambda<Func<object[], object>>(newExp, param);
                Func<object[], object> compiledLambda = lambda.Compile();
                return delegate(object[] args) { return compiledLambda(args); };
            }

            public static ConstructorDelegate GetConstructorByExpression(Type type, params Type[] argsType)
            {
                ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
                // if it's a value type (i.e., struct), it won't have a default constructor, so use Activator instead
                return constructorInfo == null ? (type.IsValueType ? GetConstructorForValueType(type) : null) : GetConstructorByExpression(constructorInfo);
            }

#endif

        public static GetDelegate GetGetMethod(PropertyInfo propertyInfo) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetGetMethodByReflection(propertyInfo);
#else
                return GetGetMethodByExpression(propertyInfo);
#endif
        }

        public static GetDelegate GetGetMethod(FieldInfo fieldInfo) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetGetMethodByReflection(fieldInfo);
#else
                return GetGetMethodByExpression(fieldInfo);
#endif
        }

        public static GetDelegate GetGetMethodByReflection(PropertyInfo propertyInfo) {
            MethodInfo methodInfo = GetGetterMethodInfo(propertyInfo);
            return delegate (object source) { return methodInfo.Invoke(source,EmptyObjects); };
        }

        public static GetDelegate GetGetMethodByReflection(FieldInfo fieldInfo) {
            return delegate (object source) { return fieldInfo.GetValue(source); };
        }

#if !SIMPLE_JSON_NO_LINQ_EXPRESSION

            public static GetDelegate GetGetMethodByExpression(PropertyInfo propertyInfo)
            {
                MethodInfo getMethodInfo = GetGetterMethodInfo(propertyInfo);
                ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                UnaryExpression instanceCast = (!IsValueType(propertyInfo.DeclaringType)) ? Expression.TypeAs(instance, propertyInfo.DeclaringType) : Expression.Convert(instance, propertyInfo.DeclaringType);
                Func<object, object> compiled = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, getMethodInfo), typeof(object)), instance).Compile();
                return delegate(object source) { return compiled(source); };
            }

            public static GetDelegate GetGetMethodByExpression(FieldInfo fieldInfo)
            {
                ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                MemberExpression member = Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo);
                GetDelegate compiled = Expression.Lambda<GetDelegate>(Expression.Convert(member, typeof(object)), instance).Compile();
                return delegate(object source) { return compiled(source); };
            }

#endif

        public static SetDelegate GetSetMethod(PropertyInfo propertyInfo) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetSetMethodByReflection(propertyInfo);
#else
                // if it's a struct, we want to use reflection, as linq expressions modify copies of the object and not the real thing
                if (propertyInfo.DeclaringType.IsValueType)
                    return GetSetMethodByReflection(propertyInfo);
                return GetSetMethodByExpression(propertyInfo);
#endif
        }

        public static SetDelegate GetSetMethod(FieldInfo fieldInfo) {
#if SIMPLE_JSON_NO_LINQ_EXPRESSION
            return GetSetMethodByReflection(fieldInfo);
#else
                // if it's a struct, we want to use reflection, as linq expressions modify copies of the object and not the real thing
                if (fieldInfo.DeclaringType.IsValueType)
                    return GetSetMethodByReflection(fieldInfo);
                return GetSetMethodByExpression(fieldInfo);
#endif
        }

        public static SetDelegate GetSetMethodByReflection(PropertyInfo propertyInfo) {
            MethodInfo methodInfo = GetSetterMethodInfo(propertyInfo);
            return delegate (object source,object value) { methodInfo.Invoke(source,new object[] { value }); };
        }

        public static SetDelegate GetSetMethodByReflection(FieldInfo fieldInfo) {
            return delegate (object source,object value) { fieldInfo.SetValue(source,value); };
        }

#if !SIMPLE_JSON_NO_LINQ_EXPRESSION

            public static SetDelegate GetSetMethodByExpression(PropertyInfo propertyInfo)
            {
                MethodInfo setMethodInfo = GetSetterMethodInfo(propertyInfo);
                ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                ParameterExpression value = Expression.Parameter(typeof(object), "value");
                UnaryExpression instanceCast = (!IsValueType(propertyInfo.DeclaringType)) ? Expression.TypeAs(instance, propertyInfo.DeclaringType) : Expression.Convert(instance, propertyInfo.DeclaringType);
                UnaryExpression valueCast = (!IsValueType(propertyInfo.PropertyType)) ? Expression.TypeAs(value, propertyInfo.PropertyType) : Expression.Convert(value, propertyInfo.PropertyType);
                Action<object, object> compiled = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, setMethodInfo, valueCast), new ParameterExpression[] { instance, value }).Compile();
                return delegate(object source, object val) { compiled(source, val); };
            }

            public static SetDelegate GetSetMethodByExpression(FieldInfo fieldInfo)
            {
                ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                ParameterExpression value = Expression.Parameter(typeof(object), "value");
                Action<object, object> compiled = Expression.Lambda<Action<object, object>>(
                    Assign(Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo), Expression.Convert(value, fieldInfo.FieldType)), instance, value).Compile();
                return delegate(object source, object val) { compiled(source, val); };
            }

            public static BinaryExpression Assign(Expression left, Expression right)
            {
#if SIMPLE_JSON_TYPEINFO
                return Expression.Assign(left, right);
#else
                MethodInfo assign = typeof(Assigner<>).MakeGenericType(left.Type).GetMethod("Assign");
                BinaryExpression assignExpr = Expression.Add(left, right, assign);
                return assignExpr;
#endif
            }

            private static class Assigner<T>
            {
                public static T Assign(ref T left, T right)
                {
                    return (left = right);
                }
            }

#endif

        public sealed class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey,TValue> {
#if SIMPLE_JSON_SINGLE_THREADED
            private readonly ThreadSafeDictionaryValueFactory<TKey,TValue> _valueFactory;
            private Dictionary<TKey,TValue> _dictionary;
#else
            private readonly object _lock = new object();
            private readonly ThreadSafeDictionaryValueFactory<TKey,TValue> _valueFactory;
            private Dictionary<TKey,TValue> _dictionary;
#endif

            public ThreadSafeDictionary(ThreadSafeDictionaryValueFactory<TKey,TValue> valueFactory) {
                _valueFactory = valueFactory;
            }

            private TValue Get(TKey key) {
                if(_dictionary == null)
                    return AddValue(key);
                TValue value;
                if(!_dictionary.TryGetValue(key,out value))
                    return AddValue(key);
                return value;
            }

            private TValue AddValue(TKey key) {
                TValue value = _valueFactory(key);
#if SIMPLE_JSON_SINGLE_THREADED
                if(_dictionary == null) {
                    _dictionary = new Dictionary<TKey,TValue>();
                }
                _dictionary[key] = value;
#else
                lock (_lock)
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
#endif
                return value;
            }

            public void Add(TKey key,TValue value) {
                throw new NotImplementedException();
            }

            public bool ContainsKey(TKey key) {
                // BUG FIX: _dictionary 在首次 Get 前可能为 null，直接调用会 NullReferenceException
                return _dictionary != null && _dictionary.ContainsKey(key);
            }

            public ICollection<TKey> Keys {
                // BUG FIX: _dictionary 为 null 时返回空集合而非抛出 NullReferenceException
                get { return _dictionary != null ? _dictionary.Keys : (ICollection<TKey>)new TKey[0]; }
            }

            public bool Remove(TKey key) {
                throw new NotImplementedException();
            }

            public bool TryGetValue(TKey key,out TValue value) {
                // BUG FIX: 原实现调用 this[key]（即 Get()），当 key 不存在时会调用 AddValue 创建并缓存新值，
                // 始终返回 true，破坏了 TryGetValue 的语义，也可能导致意外的缓存污染。
                // 正确做法：先检查字典中是否存在该 key，不存在则返回 false。
                if(_dictionary != null && _dictionary.TryGetValue(key,out value))
                    return true;
                value = default(TValue);
                return false;
            }

            public ICollection<TValue> Values {
                // BUG FIX: _dictionary 为 null 时返回空集合
                get { return _dictionary != null ? _dictionary.Values : (ICollection<TValue>)new TValue[0]; }
            }

            public TValue this[TKey key] {
                get { return Get(key); }
                set { throw new NotImplementedException(); }
            }

            public void Add(KeyValuePair<TKey,TValue> item) {
                throw new NotImplementedException();
            }

            public void Clear() {
#if SIMPLE_JSON_SINGLE_THREADED
                _dictionary.Clear();
#else
                lock(_lock) {
                    _dictionary.Clear();
                }
#endif
            }

            public bool Contains(KeyValuePair<TKey,TValue> item) {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<TKey,TValue>[] array,int arrayIndex) {
                throw new NotImplementedException();
            }

            public int Count {
                // BUG FIX: _dictionary 为 null 时返回 0
                get { return _dictionary != null ? _dictionary.Count : 0; }
            }

            public bool IsReadOnly {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(KeyValuePair<TKey,TValue> item) {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator() {
                // BUG FIX: _dictionary 为 null 时返回空枚举器
                if(_dictionary == null) yield break;
                foreach(var kv in _dictionary) yield return kv;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                if(_dictionary == null) yield break;
                foreach(var kv in _dictionary) yield return kv;
            }
        }

    }

    /// <summary>
    /// JsonIgnore： Ignore attributes/fields
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property |
                       System.AttributeTargets.Field)]
    public sealed class JsonIgnoreAttribute : Attribute {
    }
    /// <summary>
    /// JsonInclude：Include attributes/fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field,Inherited = false,AllowMultiple = false)]
    public class JsonIncludeAttribute : Attribute {
    }
    /// <summary>
    /// 序列化/反序列化时使用别名映射字段或属性名称。需要Only Public=false才能生效。
    /// AcceptOriginal=true 时，反序列化同时接受原始名称和别名。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class JsonAliasAttribute : Attribute {
        public string Alias { get; private set; }
        public bool AcceptOriginal { get; private set; }

        public JsonAliasAttribute(string aliasName,bool acceptOriginalName = false) {
            Alias = aliasName;
            AcceptOriginal = acceptOriginalName;
        }
    }
    /// <summary>
    /// Json Serializer Extensions
    /// </summary>
    public static class SimpleJsonSerializerExtensions {
#if SIMPLE_JSON_UNITY
        static UnitySerializationStrategy publicLowerCaseStrategy = new UnitySerializationStrategy(true,true);
        static UnitySerializationStrategy publicUpperCaseStrategy = new UnitySerializationStrategy(false,true);
        static UnitySerializationStrategy privateLowerCaseStrategy = new UnitySerializationStrategy(true,false);
        static UnitySerializationStrategy privateUpperCaseStrategy = new UnitySerializationStrategy(false,false);
#else
        static DefaultJsonSerializationStrategy publicLowerCaseStrategy = new DefaultJsonSerializationStrategy(true,true);
        static DefaultJsonSerializationStrategy publicUpperCaseStrategy = new DefaultJsonSerializationStrategy(false,true);
        static DefaultJsonSerializationStrategy privateLowerCaseStrategy = new DefaultJsonSerializationStrategy(true,false);
        static DefaultJsonSerializationStrategy privateUpperCaseStrategy = new DefaultJsonSerializationStrategy(false,false);
#endif
        public static string ToJson<T>(this T model,bool lowerCase = false,bool onlyPublic = true) {
            return SimpleJson.SerializeObject(model,GetStrategy(lowerCase,onlyPublic));
        }
        public static string ToJson(object model,bool lowerCase = false,bool onlyPublic = true) {
            return SimpleJson.SerializeObject(model,GetStrategy(lowerCase,onlyPublic));
        }
        public static T FromJson<T>(this string json,bool lowerCase = false,bool onlyPublic = true) {
            return SimpleJson.DeserializeObject<T>(json,GetStrategy(lowerCase,onlyPublic));
        }
        public static object FromJson(string data,Type t,bool lowerCase = false,bool onlyPublic = true) {
            return SimpleJson.DeserializeObject(data,t,GetStrategy(lowerCase,onlyPublic));
        }
        public static T FromObject<T>(this object obj,bool lowerCase = false,bool onlyPublic = true) {
            if(obj == null)
                return default(T);
            var ret = GetStrategy(lowerCase,onlyPublic).DeserializeObject(obj,typeof(T));
            if(ret is T)
                return (T)ret;
            return default(T);
        }

        internal static PocoJsonSerializerStrategy GetStrategy(bool lowerCase,bool onlyPublic) {
            if(lowerCase && onlyPublic)
                return publicLowerCaseStrategy;
            if(lowerCase && !onlyPublic)
                return privateLowerCaseStrategy;
            if(!lowerCase && onlyPublic)
                return publicUpperCaseStrategy;
            return privateUpperCaseStrategy;
        }



    }




}
// ReSharper restore LoopCanBeConvertedToQuery
// ReSharper restore RedundantExplicitArrayCreation
// ReSharper restore SuggestUseVarKeywordEvident
