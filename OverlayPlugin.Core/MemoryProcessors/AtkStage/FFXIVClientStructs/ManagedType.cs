using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkGui.FFXIVClientStructs
{
    /// <summary>
    /// This class acts as a proxy to FFXIV process memory, based on a struct.
    /// <para />
    /// Attempting to access a non-pointer field of the struct will return that property.
    /// <para />
    /// Attempting to access a pointer field of the struct will automatically read the process memory to fetch that struct,
    /// then return a new ManagedType entry for that property.
    /// </summary>
    /// <typeparam name="T">
    /// An unmanaged struct representing FFXIV process memory.
    /// </typeparam>
    public class ManagedType<T> : DynamicObject where T : unmanaged
    {
        /// <summary>
        /// Wraps type <typeparamref name="type"/> as a ManagedType<<typeparamref name="type"/>>
        /// </summary>
        /// <param name="type">The type to wrap</param>
        /// <returns>The wrapped type</returns>
        public static Type ManagedTypify(Type type)
        {
            return typeof(ManagedType<>)
                .MakeGenericType(type);
        }

        /// <summary>
        /// Get an instance of ManagedType via pointer when the <typeparamref name="T"/> is unknown at compile time.
        /// </summary>
        /// <param name="ptr">Pointer to FFXIV memory</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="type">The struct type to read from memory</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/> with a <typeparamref name="T"/> of <paramref name="type"/></returns>
        public static dynamic GetDynamicManagedTypeFromIntPtr(IntPtr ptr, FFXIVMemory memory, Type type, Dictionary<IntPtr, object> readPtrMap = null)
        {
            return ManagedTypify(type)
                .GetMethod("GetManagedTypeFromIntPtr")
                .Invoke(null, new object[] { ptr, memory, readPtrMap });
        }

        /// <summary>
        /// Get an instance of ManagedType via pointer when the <typeparamref name="T"/> is known at compile time.
        /// </summary>
        /// <param name="ptr">Pointer to FFXIV memory</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/> instance read from memory</returns>
        public static ManagedType<T> GetManagedTypeFromIntPtr(IntPtr ptr, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap = null)
        {
            if (readPtrMap == null)
            {
                readPtrMap = new Dictionary<IntPtr, object>();
            }
            if (readPtrMap.ContainsKey(ptr))
            {
                return (ManagedType<T>)readPtrMap[ptr];
            }
            return new ManagedType<T>(ptr, memory, readPtrMap);
        }

        /// <summary>
        /// Wraps an already-read instance of <typeparamref name="T"/> when the <typeparamref name="T"/> is unknown at compile time.
        /// </summary>
        /// <param name="baseObj">Base object to wrap</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="type">The struct type to read from memory</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/> with a <typeparamref name="T"/> of <paramref name="type"/></returns>
        public static dynamic GetDynamicManagedTypeFromBaseType(IntPtr ptr, object baseObj, FFXIVMemory memory, Type type, Dictionary<IntPtr, object> readPtrMap = null)
        {
            return ManagedTypify(type)
                .GetMethod("GetManagedTypeFromBaseType")
                .Invoke(null, new object[] { ptr, baseObj, memory, readPtrMap });
        }

        /// <summary>
        /// Wraps an already-read instance of <typeparamref name="T"/> when the <typeparamref name="T"/> is known at compile time.
        /// </summary>
        /// <param name="baseObj">Base object to wrap</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/></returns>
        public static ManagedType<T> GetManagedTypeFromBaseType(IntPtr ptr, T baseObj, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap = null)
        {
            if (readPtrMap == null)
            {
                readPtrMap = new Dictionary<IntPtr, object>();
            }
            return new ManagedType<T>(ptr, baseObj, memory, readPtrMap);
        }

        /// <summary>
        /// Pointer to this object in FFXIV memory
        /// </summary>
        [IgnoreDataMember]
        public readonly IntPtr ptr;

        [IgnoreDataMember]
        private readonly FFXIVMemory memory;
        /// <summary>
        /// Track if we've read the object from memory already, so we don't read multiple times
        /// </summary>
        [IgnoreDataMember]
        private bool haveReadBaseObj = false;
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are pointers which need read from memory when accessed
        /// </summary>
        [IgnoreDataMember]
        private Dictionary<string, IntPtr> ptrMap = new Dictionary<string, IntPtr>();
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are pointers and which have been read from memory
        /// </summary>
        [IgnoreDataMember]
        private Dictionary<string, object> objMap = new Dictionary<string, object>();
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are not pointers, and their read values
        /// </summary>
        [IgnoreDataMember]
        private Dictionary<string, object> valMap = new Dictionary<string, object>();
        /// <summary>
        /// Map of pointers of objects read as part of the topmost object in this tree, to avoid reading objects multiple times
        /// </summary>
        [IgnoreDataMember]
        private Dictionary<IntPtr, object> readPtrMap = new Dictionary<IntPtr, object>();
        /// <summary>
        /// Cached raw version of this instance
        /// </summary>
        [IgnoreDataMember]
        private T rawObject;

        /// <summary>
        /// Internal-only constructor from a pointer
        /// </summary>
        /// <param name="ptr">Pointer to game memory</param>
        /// <param name="memory">Must be valid for the lifecycle of this object, unless <see cref="ToJObject"/> is called to fully read the object tree from memory</param>
        /// <param name="readPtrMap">Map of already read pointers. Must not be null</param>
        private ManagedType(IntPtr ptr, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap)
        {
            this.ptr = ptr;
            this.memory = memory;
            this.readPtrMap = readPtrMap;
            if (ptr.ToInt64() == 0)
            {
                throw new Exception($"NPE! {ptr.ToInt64():X}, {typeof(T).Name}");
            }
            if (readPtrMap.ContainsKey(ptr))
            {
                throw new Exception($"Already read this object! {ptr.ToInt64():X}, {typeof(T).Name}");
            }
            readPtrMap[ptr] = this;
        }

        /// <summary>
        /// Internal-only constructor from an already-existing object
        /// </summary>
        /// <param name="baseObj">Base object to wrap</param>
        /// <param name="memory">Must be valid for the lifecycle of this object, unless <see cref="ToJObject"/> is called to fully read the object tree from memory</param>
        /// <param name="readPtrMap">Map of already read pointers. Must not be null</param>
        private ManagedType(IntPtr ptr, T baseObj, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap)
        {
            this.ptr = ptr;
            this.memory = memory;
            this.readPtrMap = readPtrMap;
            readPtrMap[ptr] = this;

            haveReadBaseObj = true;
            rawObject = baseObj;
            ReadBaseObj();
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1 && indexes[0].GetType() == typeof(string))
            {
                return TryGetField((string)indexes[0], out result);
            }
            return base.TryGetIndex(binder, indexes, out result);
        }

        /// <summary>
        /// Override from <see cref="DynamicObject"/>, called by the .NET runtime when attempting to read a non-declared field on the object
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var success = TryGetField(binder.Name, out result);
            if (success)
            {
                return true;
            }
            return base.TryGetMember(binder, out result);
        }

        /// <summary>
        /// Override from <see cref="DynamicObject"/>. Uses fooMap fields to intercept runtime field access and return the underlying values.
        /// <para/>
        /// If <paramref name="name"/> is referencing a pointer, it will return a new <see cref="ManagedType{T}"/> instance for that field instead
        /// </summary>
        /// <param name="name">The field name that's being accessed</param>
        /// <param name="result">The value that's read, otherwise null</param>
        /// <returns>True on success, False on failure</returns>
        private bool TryGetField(string name, out object result)
        {
            // Make sure we've read the base object from FFXIV memory
            if (!haveReadBaseObj)
            {
                ReadBaseObjFromMemory();
            }

            // If the field is a pointer
            if (ptrMap.ContainsKey(name))
            {
                // And we haven't already read the pointer value from FFXIV memory
                if (!objMap.ContainsKey(name))
                {
                    var ptr = ptrMap[name];
                    // Make sure we're not trying to read a null pointer
                    if (ptr.ToInt64() == 0)
                    {
                        objMap[name] = null;
                        result = null;
                        return true;
                    }
                    // If this pointer was already read by another object in this object tree
                    if (readPtrMap.ContainsKey(ptr))
                    {
                        // Return the already-read object
                        result = readPtrMap[ptr];
                        // And store it off locally to properly early-out next time
                        objMap[name] = result;
                        return true;
                    }
                    // If we haven't already bailed, we need to read this object from memory.
                    // Wrap the pointer, set it as the out param, and store it off for future calls.
                    var obj = GetDynamicManagedTypeFromIntPtr(ptr, memory, typeof(T).GetField(name).FieldType.GetElementType(), readPtrMap);
                    objMap[name] = obj;
                    result = obj;
                    return true;
                }
                // We've already read this pointer, return the cached value
                result = objMap[name];
                return true;
            }
            else if (valMap.ContainsKey(name))
            {
                // This is a plain value on the struct, return it directly
                result = valMap[name];
                return true;
            }
            // Field not found
            result = null;
            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (!haveReadBaseObj)
            {
                ReadBaseObjFromMemory();
            }
            var members = new List<string>(ptrMap.Keys);
            members.AddRange(valMap.Keys);
            return members;
        }

        [IgnoreDataMember]
        private static readonly List<string> SkipIterators = new List<string>() {
            "FFXIV.Component.GUI.AtkResNode",
            "FFXIV.Client.Graphics.Render.Notifier",
        };

        /// <summary>
        /// Reads the base object from FFXIV memory
        /// </summary>
        private unsafe void ReadBaseObjFromMemory()
        {
            haveReadBaseObj = true;
            var objSize = Marshal.SizeOf(typeof(T));

            if (ptr.ToInt64() == 0)
            {
                return;
            }

            byte[] source = memory.GetByteArray(ptr, objSize);

            fixed (byte* p = source)
            {
                rawObject = *(T*)&p[0];
            }
            ReadBaseObj();
        }

        /// <summary>
        /// Populates the fooMap fields with values
        /// </summary>
        private unsafe void ReadBaseObj()
        {
            foreach (var field in typeof(T).GetFields())
            {
                // Skip these types entirely, they chain way too deep and cause issues.
                if (SkipIterators.Exists((iter) => field.FieldType.FullName.EndsWith(iter)))
                {
                    continue;
                }
                // Check to see if this field is a fixed native type buffer, e.g. `fixed byte foo[20]`
                FixedBufferAttribute fixedBuffer = (FixedBufferAttribute)field.GetCustomAttribute(typeof(FixedBufferAttribute));
                if (field.FieldType.IsPointer)
                {
                    // `GetValue` returns an object of type `Pointer` when called against a pointer field, need to unbox it
                    var value = Pointer.Unbox((Pointer)field.GetValue(rawObject));
                    // Treat some pointers differently by just returning the memory address
                    if (
                        // <*>** pointers are multidimensional arrays
                        field.FieldType.GetElementType().IsPointer ||
                        // void* pointers are unknown data types
                        field.FieldType.GetElementType() == typeof(void)
                    )
                    {
                        // Store this pointer off as a ulong in the value map instead
                        valMap[field.Name] = (ulong)value;
                    }
                    else
                    {
                        // Store this pointer off in the pointer map as an object to be read later if needed
                        ptrMap.Add(field.Name, new IntPtr((long)value));
                    }
                }
                else if (fixedBuffer != null)
                {
                    // Fixed buffers get dealt with slightly differently.
                    // Determine the offset to this buffer
                    var offset = GetOffset(typeof(T), field.Name);
                    // Determine the count of elements in the array
                    var elementTypeSize = Marshal.SizeOf(fixedBuffer.ElementType);
                    var elementCount = fixedBuffer.Length / elementTypeSize;
                    // Read the fixed buffer to an array

                    var array = Array.CreateInstance(fixedBuffer.ElementType, elementCount);
                    fixed (void* tPtr = &rawObject)
                    {
                        byte* fixedPtr = ((byte*)tPtr) + offset;
                        for (int i = 0; i < elementCount; ++i)
                        {
                            // Cast this pointer to the correct type and add it to the array at the correct position
                            array.SetValue(DynamicCast(fixedBuffer.ElementType, &fixedPtr[i]), i);
                        }
                    }
                    valMap[field.Name] = array;
                }
                else if (field.FieldType.Name == "Utf8String")
                {
                    object str = field.GetValue(rawObject);
                    long strPtr = (long)Pointer.Unbox(str.GetType().GetField("StringPtr").GetValue(str));
                    long strLen = ((long)str.GetType().GetField("BufUsed").GetValue(str)) - 1;
                    var bytes = memory.GetByteArray(new IntPtr(strPtr), (int)strLen);
                    valMap[field.Name] = System.Text.Encoding.UTF8.GetString(bytes);
                }
                else if (IsConcreteGeneric(field.FieldType))
                {
                    valMap[field.Name] = ConcreteGenericToManaged(field, field.GetValue(rawObject));
                }
                else if (
                    field.FieldType.IsPrimitive
                    || field.FieldType.IsEnum
                )
                {
                    // Primitives and enums get stored normally
                    valMap[field.Name] = field.GetValue(rawObject);
                }
                else
                {
                    // Objects get wrapped
                    dynamic obj = GetDynamicManagedTypeFromBaseType(ptr + GetOffset(typeof(T), field.Name), field.GetValue(rawObject), memory, field.FieldType, readPtrMap);
                    valMap[field.Name] = obj;
                }
            }
        }

        /// <summary>
        /// Given a pointer to a native type at address <paramref name="v"/>, cast that pointer to the underlying object
        /// </summary>
        /// <param name="elementType">The type of the element at the pointer at address <paramref name="v"/></param>
        /// <param name="v">Pointer to the element</param>
        /// <returns>Value at address <paramref name="v"/> cast to type <paramref name="elementType"/></returns>
        private unsafe object DynamicCast(Type elementType, byte* v)
        {
            switch (elementType.Name)
            {
                case "Boolean":
                    return *(Boolean*)v;
                case "Byte":
                    return *(Byte*)v;
                case "SByte":
                    return *(SByte*)v;
                case "Int16":
                    return *(Int16*)v;
                case "UInt16":
                    return *(UInt16*)v;
                case "Int32":
                    return *(Int32*)v;
                case "UInt32":
                    return *(UInt32*)v;
                case "Int64":
                    return *(Int64*)v;
                case "UInt64":
                    return *(UInt64*)v;
                case "IntPtr":
                    return *(IntPtr*)v;
                case "UIntPtr":
                    return *(UIntPtr*)v;
                case "Char":
                    return *(Char*)v;
                case "Double":
                    return *(Double*)v;
                case "Single":
                    return *(Single*)v;
                default:
                    // Should be impossible
                    return null;
            }
        }

        /// <summary>
        /// Same as <see cref="NetworkProcessors.NetworkParser.GetOffset(Type, string)"/>
        /// </summary>
        public static int GetOffset(Type type, string property)
        {
            int offset = 0;

            foreach (var prop in type.GetFields())
            {
                var customOffset = prop.GetCustomAttribute<FieldOffsetAttribute>();
                if (customOffset != null)
                {
                    offset = customOffset.Value;
                }

                if (prop.Name == property)
                {
                    break;
                }

                if (prop.FieldType.IsEnum)
                {
                    offset += Marshal.SizeOf(Enum.GetUnderlyingType(prop.FieldType));
                }
                else
                {
                    offset += Marshal.SizeOf(prop.FieldType);
                }
            }

            return offset;
        }

        /// <summary>
        /// Converts this <see cref="ManagedType{T}"/> to the underlying type <typeparamref name="T"/>.
        /// Note that any pointers on this object are pointing at FFXIV game memory addresses.
        /// If you need to get the value of a pointer, use the ManagedType field accessor instead.
        /// </summary>
        /// <returns>Instance of type <typeparamref name="T"/></returns>
        public T ToType()
        {
            if (!haveReadBaseObj)
            {
                ReadBaseObjFromMemory();
            }

            return (T)rawObject;
        }

        [IgnoreDataMember]
        private static readonly string[] ConcreteGenerics = new[] {
            "StdPair",
            "StdSet",
            "StdVector",
            "StdMap",
            "StdDeque",
            "AtkLinkedList",
            "CVector",
        };

        /// <summary>
        /// Checks if the passed in type is a concrete-ified generic from StripFFXIVClientStructs
        /// </summary>
        private bool IsConcreteGeneric(Type type)
        {
            foreach (string prefix in ConcreteGenerics)
            {
                if (type.Name.StartsWith(prefix) && type.Name != prefix && type.FullName.StartsWith("FFXIVClientStructs."))
                {
                    return true;
                }
            }

            return false;
        }

        private object ConcreteGenericToManaged(FieldInfo field, object generic)
        {
            dynamic cast = GetDynamicManagedTypeFromBaseType(ptr + GetOffset(field.FieldType, field.Name), generic, memory, generic.GetType(), readPtrMap);
            if (generic.GetType().Name.StartsWith("StdPair"))
            {
                return ConcreteStdPairToManaged(cast, generic);
            }
            if (generic.GetType().Name.StartsWith("StdSet"))
            {
                return ConcreteStdSetToManaged(cast, generic);
            }
            if (generic.GetType().Name.StartsWith("StdVector"))
            {
                return ConcreteStdVectorToManaged(cast, generic);
            }
            if (generic.GetType().Name.StartsWith("StdMap"))
            {
                return ConcreteStdMapToManaged(cast, generic);
            }
            if (generic.GetType().Name.StartsWith("AtkLinkedList"))
            {
                return ConcreteAtkLinkedListToManaged(cast, generic);
            }
            return null;
        }

        private object ConcreteStdPairToManaged(dynamic cast, object generic)
        {
            Type keyType = ManagedTypify(generic.GetType().GetField("Item1").FieldType.GetElementType());
            Type valType = ManagedTypify(generic.GetType().GetField("Item2").FieldType.GetElementType());

            // @TODO: Should we check for pointer types here, or for a concrete-ified generic?
            // Neither exists in the current version of FFXIVClientStructs.
            dynamic key = cast.Item1;
            dynamic value = cast.Item2;

            return
                typeof(KeyValuePair<,>)
                .MakeGenericType(keyType, valType)
                .GetConstructor(new Type[] { keyType, valType })
                .Invoke(new object[] { key, value });
        }

        private unsafe object ConcreteStdSetToManaged(dynamic cast, object generic)
        {
            Type nodeType = generic.GetType().GetNestedType("Node");
            Type objType = nodeType.GetField("Key").GetType();

            dynamic set = typeof(HashSet<>).MakeGenericType(objType).GetConstructor(new Type[] { }).Invoke(new object[] { });

            IntPtr nodePtr = new IntPtr(cast.Head);

            for (int i = 0; i < cast.Count; ++i)
            {
                dynamic node = ManagedType<int>.GetDynamicManagedTypeFromIntPtr(nodePtr, memory, nodeType, readPtrMap);
                dynamic key = ManagedType<int>.GetDynamicManagedTypeFromIntPtr(new IntPtr(node.Key), memory, objType, readPtrMap);

                set.Add(key.ToType());
                nodePtr = new IntPtr(node.Right);
            }

            return set;
        }

        private unsafe object ConcreteStdVectorToManaged(dynamic cast, object generic)
        {
            Type objType = generic.GetType().GetField("First").GetType().GetElementType();
            int objSize = Marshal.SizeOf(objType);

            int count = (int)(((ulong)cast.Last - (ulong)cast.First) / (ulong)objSize);

            dynamic list = typeof(List<>).MakeGenericType(objType).GetConstructor(new Type[] { }).Invoke(new object[] { });

            // This way is slower (reading from memory `count` times) as opposed to doing a large single read
            // But it's less complex this way.
            IntPtr firstPtr = new IntPtr(cast.First);

            for (int i = 0; i < count; ++i)
            {
                IntPtr objPtr = new IntPtr(((long)firstPtr) + (objSize * i));
                dynamic obj = ManagedType<int>.GetDynamicManagedTypeFromIntPtr(objPtr, memory, objType, readPtrMap);
                list.Add(obj.ToType());
            }

            return list;
        }

        private unsafe object ConcreteStdMapToManaged(dynamic cast, object generic)
        {
            Type nodeType = generic.GetType().GetNestedType("Node");
            Type pairType = nodeType.GetField("KeyValuePair").FieldType;
            Type keyType = pairType.GetField("Item1").FieldType.GetElementType();
            Type valType = pairType.GetField("Item2").FieldType.GetElementType();

            dynamic dict = typeof(Dictionary<,>).MakeGenericType(ManagedTypify(keyType), ManagedTypify(valType)).GetConstructor(new Type[] { }).Invoke(new object[] { });

            if (cast.Count > 0)
            {
                dynamic currNode = cast.Head.Left;

                int count = (int)(ulong)cast.Count;

                for (int i = 1; i < count; ++i)
                {
                    currNode = currNode.Right;
                    if (currNode.IsNil)
                    {
                        continue;
                    }
                    var kvp = currNode.KeyValuePair;
                    dict[kvp.Key] = kvp.Value;
                }
            }

            return dict;
        }

        private object ConcreteAtkLinkedListToManaged(dynamic cast, object generic)
        {
            Type nodeType = generic.GetType().GetNestedType("Node");
            Type objType = nodeType.GetField("Value").FieldType.GetElementType();

            dynamic list = typeof(List<>).MakeGenericType(objType).GetConstructor(new Type[] { }).Invoke(new object[] { });

            if (cast.Count > 0)
            {
                dynamic current = cast.Start;
                list.Add(current.Value.ToType());

                for (int i = 1; i < cast.Count; ++i)
                {
                    current = current.Next;
                    list.Add(current.Value.ToType());
                }
            }

            return list;
        }
    }
}
