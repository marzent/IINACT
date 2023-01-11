using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        /// Get an instance of ManagedType via pointer when the <typeparamref name="T"/> is unknown at compile time.
        /// </summary>
        /// <param name="ptr">Pointer to FFXIV memory</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="type">The struct type to read from memory</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/> with a <typeparamref name="T"/> of <paramref name="type"/></returns>
        public static dynamic GetDynamicManagedTypeFromIntPtr(IntPtr ptr, FFXIVMemory memory, Type type, Dictionary<IntPtr, object> readPtrMap = null)
        {
            return typeof(ManagedType<>).MakeGenericType(type)
                .GetMethod("GetManagedTypeFromIntPtr").Invoke(null, new object[] { ptr, memory, readPtrMap });
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
        public static dynamic GetDynamicManagedTypeFromBaseType(object baseObj, FFXIVMemory memory, Type type, Dictionary<IntPtr, object> readPtrMap = null)
        {
            return typeof(ManagedType<>).MakeGenericType(type)
                .GetMethod("GetManagedTypeFromBaseType").Invoke(null, new object[] { baseObj, memory, readPtrMap });
        }

        /// <summary>
        /// Wraps an already-read instance of <typeparamref name="T"/> when the <typeparamref name="T"/> is known at compile time.
        /// </summary>
        /// <param name="baseObj">Base object to wrap</param>
        /// <param name="memory">FFXIVMemory instance which should be valid for the lifetime of this object</param>
        /// <param name="readPtrMap">Used internally to track already-read pointers</param>
        /// <returns><see cref="ManagedType{T}"/></returns>
        public static ManagedType<T> GetManagedTypeFromBaseType(T baseObj, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap = null)
        {
            if (readPtrMap == null)
            {
                readPtrMap = new Dictionary<IntPtr, object>();
            }
            return new ManagedType<T>(baseObj, memory, readPtrMap);
        }

        /// <summary>
        /// Pointer to this object in FFXIV memory
        /// </summary>
        public readonly IntPtr ptr;
        private readonly FFXIVMemory memory;
        /// <summary>
        /// Track if we've read the object from memory already, so we don't read multiple times
        /// </summary>
        private bool haveReadBaseObj = false;
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are pointers which need read from memory when accessed
        /// </summary>
        private Dictionary<string, IntPtr> ptrMap = new Dictionary<string, IntPtr>();
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are pointers and which have been read from memory
        /// </summary>
        private Dictionary<string, object> objMap = new Dictionary<string, object>();
        /// <summary>
        /// Map of fields in struct <typeparamref name="T"/> which are not pointers, and their read values
        /// </summary>
        private Dictionary<string, object> valMap = new Dictionary<string, object>();
        /// <summary>
        /// Map of pointers of objects read as part of the topmost object in this tree, to avoid reading objects multiple times
        /// </summary>
        private Dictionary<IntPtr, object> readPtrMap = new Dictionary<IntPtr, object>();
        /// <summary>
        /// Cached JObject version of this instance
        /// </summary>
        private JObject jObject = null;

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
        private ManagedType(T baseObj, FFXIVMemory memory, Dictionary<IntPtr, object> readPtrMap)
        {
            haveReadBaseObj = true;
            ReadBaseObj(baseObj);

            this.memory = memory;
            this.readPtrMap = readPtrMap;
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
                ReadBaseObj();
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

        /// <summary>
        /// Reads this entire object from memory recursively
        /// </summary>
        /// <returns>A JObject representation of this object</returns>
        public JObject ToJObject()
        {
            // Make sure we've read the base object from FFXIV memory
            if (!haveReadBaseObj)
            {
                ReadBaseObj();
            }

            if (jObject != null)
            {
                return jObject;
            }
            jObject = new JObject();

            // Loop over pointers and conver them to JObjects
            foreach (var key in ptrMap.Keys)
            {
                var success = TryGetField(key, out object result);
                if (success && result != null)
                {
                    jObject[key] = ((dynamic)result).ToJObject();
                }
            }

            // Loop over non-pointers
            foreach (var key in valMap.Keys)
            {
                var success = TryGetField(key, out object result);
                if (success)
                {
                    if (result.GetType().IsPrimitive || result.GetType().IsArray)
                    {
                        // If it's a primitive or array, we can store it directly
                        jObject[key] = JToken.FromObject(result);
                    }
                    else if (result.GetType().IsEnum)
                    {
                        // If it's an enum, we need to `ToString` it because of runtime issues with Newtonsoft JSON not accepting enums directly.
                        // TODO: This probably needs further investigation/thought.
                        // We may want to for instance cast to int instead, and have a dynamic `key + "__Name"` property?
                        jObject[key] = JToken.FromObject(result.ToString());
                    }
                    else
                    {
                        // If it's a full-on object or struct, wrap it and then convert it to JObject
                        dynamic obj = GetDynamicManagedTypeFromBaseType(result, memory, typeof(T).GetField(key).FieldType, readPtrMap);
                        jObject[key] = obj.ToJObject();
                    }
                }
            }

            return jObject;
        }

        /// <summary>
        /// Reads the base object from FFXIV memory
        /// </summary>
        private unsafe void ReadBaseObj()
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
                var memory = *(T*)&p[0];

                ReadBaseObj(memory);
            }
        }

        /// <summary>
        /// Takes an already-read object <paramref name="memory"/> and populates the fooMap fields with values
        /// </summary>
        /// <param name="memory">An instance of <see cref="T"/> which is used to populate fooMap fields with values</param>
        private unsafe void ReadBaseObj(T memory)
        {
            var p = (byte*)&memory;
            foreach (var field in typeof(T).GetFields())
            {
                // Check to see if this field is a fixed native type buffer, e.g. `fixed byte foo[20]`
                FixedBufferAttribute fixedBuffer = (FixedBufferAttribute)field.GetCustomAttribute(typeof(FixedBufferAttribute));
                if (field.FieldType.IsPointer)
                {
                    // `GetValue` returns an object of type `Pointer` when called against a pointer field, need to unbox it
                    var value = Pointer.Unbox((Pointer)field.GetValue(memory));
                    // Treat some pointers differently by just returning the memory address
                    if (
                        // <*>** pointers are multidimensional arrays
                        field.FieldType.GetElementType().IsPointer ||
                        // void* pointers are unknown data types
                        field.FieldType.GetElementType() == typeof(void) ||
                        // <T>* pointers are linked lists, attempting to read them leads to a stack overflow exception
                        field.FieldType.GetElementType() == typeof(T)
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
                    byte* fixedPtr = &p[offset];
                    for (int i = 0; i < elementCount; ++i)
                    {
                        // Cast this pointer to the correct type and add it to the array at the correct position
                        array.SetValue(DynamicCast(fixedBuffer.ElementType, &fixedPtr[i]), i);
                    }
                    valMap[field.Name] = array;
                }
                else
                {
                    // This is a normal field, just store it normally
                    valMap[field.Name] = field.GetValue(memory);
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
        private int GetOffset(Type type, string property)
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
        /// Converts this <see cref="ManagedType{T}"/> to the underlying type <typeparamref name="T"/>
        /// </summary>
        /// <returns>Instance of type <typeparamref name="T"/></returns>
        public T ToType()
        {
            T obj = default;

            // Loop over the fields, reverse the logic in ReadBaseObj to assign it to the return object
            foreach (var field in typeof(T).GetFields())
            {
                if (field.FieldType.IsPointer)
                {
                    // Treat some pointers differently by just returning the memory address
                    if (
                        // <*>** pointers are multidimensional arrays
                        field.FieldType.GetElementType().IsPointer ||
                        // void* pointers are unknown data types
                        field.FieldType.GetElementType() == typeof(void) ||
                        // <T>* pointers are linked lists, attempting to read them leads to a stack overflow exception
                        field.FieldType.GetElementType() == typeof(T)
                    )
                    {
                        field.SetValue(obj, valMap[field.Name]);
                    }
                    else
                    {
                        field.SetValue(obj, ptrMap[field.Name]);
                    }
                }
                else
                {
                    field.SetValue(obj, valMap[field.Name]);
                }
            }

            return obj;
        }
    }
}
