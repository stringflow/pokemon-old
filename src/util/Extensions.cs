using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public static class Extensions {

    // Returns a sub-section of an array starting at 'index' and consisting of 'length' elements.
    public static T[] Subarray<T>(this T[] source, int index, int length) {
        T[] subarray = new T[length];
        Array.Copy(source, index, subarray, 0, length);
        return subarray;
    }

    public static byte u8(this byte[] data, int offset) {
        return data[offset];
    }

    public static ushort u16le(this byte[] data, int offset) {
        return (ushort) (data[offset] | (data[offset + 1] << 8));
    }

    public static ushort u16be(this byte[] data, int offset) {
        return (ushort) ((data[offset] << 8) | data[offset]);
    }

    public static int u24le(this byte[] data, int offset) {
        return (ushort) (data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16));
    }

    public static int u24be(this byte[] data, int offset) {
        return (ushort) ((data[offset] << 16) | (data[offset + 1] << 8) | data[offset + 2]);
    }

    public static uint u32le(this byte[] data, int offset) {
        return (uint) (data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    public static uint u32be(this byte[] data, int offset) {
        return (uint) ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    public static T ReadStruct<T>(this byte[] array, int index, bool bigEndian = false) where T : unmanaged {
        if(bigEndian) EndianSwap(typeof(T), array);
        int structSize = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(structSize);
        Marshal.Copy(array, index, ptr, structSize);
        T str = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);
        return str;
    }

    public static byte[] ToBytes<T>(this T str, bool bigEndian = false) where T : unmanaged {
        int size = Marshal.SizeOf(str);
        byte[] array = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, array, 0, size);
        Marshal.FreeHGlobal(ptr);
        if(bigEndian) EndianSwap(typeof(T), array);
        return array;
    }

    private static void EndianSwap(Type type, byte[] data) {
        foreach(FieldInfo field in type.GetFields()) {
            Type fieldType = field.FieldType;
            if(field.IsStatic || fieldType == typeof(string)) continue;
            int offset = Marshal.OffsetOf(type, field.Name).ToInt32();
            object[] attr = field.GetCustomAttributes(typeof(FixedBufferAttribute), false);
            if(attr.Length == 0) Array.Reverse(data, offset, Marshal.SizeOf(fieldType));
        }
    }
}