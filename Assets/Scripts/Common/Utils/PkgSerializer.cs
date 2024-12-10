using System;
using System.Runtime.InteropServices;

/// <summary>
/// This class was design to serialize/deserialize structs with the.
/// attribute [StructLayout(LayoutKind.Sequential, Pack = 1)].
/// </summary>
public class PkgSerializer 
{
    //BEWARE!: It might be platform-dependent; e.g., be careful with endianess
    public static T GetStruct<T>(byte[] data) where T : new()
    {
        T retval = new T();
        int size = Marshal.SizeOf(retval);
        if (size > data.Length)
            throw new ArgumentOutOfRangeException("The raw data is not valid");
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.Copy(data, 0, ptr, size);

        retval = (T)Marshal.PtrToStructure(ptr, retval.GetType());
        Marshal.FreeHGlobal(ptr);

        return retval;
    }

    public static byte[] GetBytes<T>(T data) where T : new()
    {
        int size = Marshal.SizeOf(data);
        byte[] retVal = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data, ptr, true);
        Marshal.Copy(ptr, retVal, 0, size);
        Marshal.FreeHGlobal(ptr);
        return retVal;
    }
}



//public class Serializer
//{
//    public static byte[] GetBytes(object str)
//    {
//        int size = Marshal.SizeOf(str);
//        byte[] arr = new byte[size];

//        IntPtr ptr = Marshal.AllocHGlobal(size);
//        Marshal.StructureToPtr(str, ptr, true);
//        Marshal.Copy(ptr, arr, 0, size);
//        Marshal.FreeHGlobal(ptr);
//        return arr;
//    }

//    public static T GetFromBytes<T>(byte[] arr)
//    {
//        // Pin the managed memory while, copy it out the data, then unpin it
//        GCHandle handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
//        T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
//        handle.Free();

//        return theStructure;
//    }

//    public static byte[] ObjectToByteArray(object obj)
//    {
//        if (obj == null)
//            return null;

//        BinaryFormatter bf = new BinaryFormatter();
//        MemoryStream ms = new MemoryStream();
//        bf.Serialize(ms, obj);

//        return ms.ToArray();
//    }

//    // Convert a byte array to an Object
//    public static object ByteArrayToObject(byte[] arrBytes)
//    {
//        MemoryStream memStream = new MemoryStream();
//        BinaryFormatter binForm = new BinaryFormatter();
//        memStream.Write(arrBytes, 0, arrBytes.Length);
//        memStream.Seek(0, SeekOrigin.Begin);
//        object obj = (object)binForm.Deserialize(memStream);

//        return obj;
//    }
//}




//private static void MaybeAdjustEndianness(Type type, byte[] data, Endianness endianness, int startOffset = 0)
//{
//    if ((BitConverter.IsLittleEndian) == (endianness == Endianness.LittleEndian))
//    {
//        // nothing to change => return
//        return;
//    }

//    foreach (var field in type.GetFields())
//    {
//        var fieldType = field.FieldType;
//        if (field.IsStatic)
//            // don't process static fields
//            continue;

//        if (fieldType == typeof(string))
//            // don't swap bytes for strings
//            continue;

//        var offset = Marshal.OffsetOf(type, field.Name).ToInt32();

//        // handle enums
//        if (fieldType.IsEnum)
//            fieldType = Enum.GetUnderlyingType(fieldType);

//        // check for sub-fields to recurse if necessary
//        var subFields = fieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();

//        var effectiveOffset = startOffset + offset;

//        if (subFields.Length == 0)
//        {
//            Array.Reverse(data, effectiveOffset, Marshal.SizeOf(fieldType));
//        }
//        else
//        {
//            // recurse
//            MaybeAdjustEndianness(fieldType, data, endianness, effectiveOffset);
//        }
//    }
//}

//internal static T BytesToStruct<T>(byte[] rawData, Endianness endianness) where T : struct
//{
//    T result = default(T);

//    MaybeAdjustEndianness(typeof(T), rawData, endianness);

//    GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);

//    try
//    {
//        IntPtr rawDataPtr = handle.AddrOfPinnedObject();
//        result = (T)Marshal.PtrToStructure(rawDataPtr, typeof(T));
//    }
//    finally
//    {
//        handle.Free();
//    }

//    return result;
//}

//internal static byte[] StructToBytes<T>(T data, Endianness endianness) where T : struct
//{
//    byte[] rawData = new byte[Marshal.SizeOf(data)];
//    GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
//    try
//    {
//        IntPtr rawDataPtr = handle.AddrOfPinnedObject();
//        Marshal.StructureToPtr(data, rawDataPtr, false);
//    }
//    finally
//    {
//        handle.Free();
//    }

//    MaybeAdjustEndianness(typeof(T), rawData, endianness);

//    return rawData;
//}