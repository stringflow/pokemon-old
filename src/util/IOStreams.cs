using System.Runtime.InteropServices;
using System.IO;

public class WriteStream : MemoryStream {

    public WriteStream() : base() {
    }

    public void Write(byte[] buf) {
        base.Write(buf);
    }

    public void Write(ushort[] buf) {
        foreach(ushort val in buf) {
            u16be(val);
        }
    }

    public void Write(bool[] buf) {
        foreach(bool val in buf) {
            boolean(val);
        }
    }

    public void Write<T>(T val, bool bigEndian = false) where T : unmanaged {
        Write(val.ToBytes(bigEndian));
    }

    public void Write(string val) {
        foreach(char c in val) {
            u8((byte) c);
        }
        u8(0x00);
    }

    public void boolean(bool val) {
        WriteByte(val ? (byte) 1 : (byte) 0);
    }

    public void u8(byte val) {
        WriteByte(val);
    }

    public void u16le(ushort val) {
        WriteByte((byte) (val & 0xff));
        WriteByte((byte) (val >> 8));
    }

    public void u16be(ushort val) {
        WriteByte((byte) (val >> 8));
        WriteByte((byte) (val & 0xff));
    }

    public void u24le(int val) {
        WriteByte((byte) (val & 0xff));
        WriteByte((byte) (val >> 8));
        WriteByte((byte) (val >> 16));
    }

    public void u24be(int val) {
        WriteByte((byte) (val >> 16));
        WriteByte((byte) (val >> 8));
        WriteByte((byte) (val & 0xff));
    }

    public void u32le(uint val) {
        WriteByte((byte) (val & 0xff));
        WriteByte((byte) (val >> 8));
        WriteByte((byte) (val >> 16));
        WriteByte((byte) (val >> 24));
    }

    public void u32be(uint val) {
        WriteByte((byte) (val >> 24));
        WriteByte((byte) (val >> 16));
        WriteByte((byte) (val >> 8));
        WriteByte((byte) (val & 0xff));
    }
}

public class ReadStream : MemoryStream {

    public bool LowerNybble;

    public ReadStream() : base() {
    }

    public ReadStream(byte[] data) : base(data) {
    }

    // Peeks the current byte.
    public byte Peek() {
        byte ret = (byte) ReadByte();
        Seek(-1, SeekOrigin.Current);
        return ret;
    }

    // Skips 'amount' bytes in the 
    public void Seek(long amount) {
        Seek(amount, SeekOrigin.Current);
    }

    // Reads until the value of 'terminator' is encountered.
    public byte[] Until(byte terminator, bool includeTerminator = true) {
        int length = 0;
        do {
            length++;
        } while(ReadByte() != terminator);
        Seek(-length, SeekOrigin.Current);
        if(!includeTerminator) length--;
        byte[] bytes = new byte[length];
        Read(bytes);
        return bytes;
    }

    // Reads 'length' number of bytes.
    public byte[] Read(int length) {
        byte[] bytes = new byte[length];
        Read(bytes);
        return bytes;
    }

    // Reads 'length' number of ushorts in the little-endian format.
    public ushort[] ReadLE(int length) {
        ushort[] ushorts = new ushort[length];
        for(int i = 0; i < length; i++) {
            ushorts[i] = u16le();
        }
        return ushorts;
    }

    // Reads 'length' number of ushorts in the big-endian format.
    public ushort[] ReadBE(int length) {
        ushort[] ushorts = new ushort[length];
        for(int i = 0; i < length; i++) {
            ushorts[i] = u16be();
        }
        return ushorts;
    }

    // Reads 'length' number of bools.
    public bool[] ReadBools(int length) {
        bool[] bools = new bool[length];
        for(int i = 0; i < length; i++) {
            bools[i] = boolean();
        }
        return bools;
    }

    // Consumes one nybble (4 bits) worth of data.
    public byte Nybble() {
        byte ret = (byte) (LowerNybble ? u8() & 0xf : Peek() >> 4);
        LowerNybble = !LowerNybble;
        return ret;
    }

    // Consumes one byte of data.
    public byte u8() {
        return (byte) ReadByte();
    }

    // Consumes one byte of data and interprets it as a boolean.
    public bool boolean() {
        return u8() != 0;
    }

    // Consumes two bytes of data in the little-endian format.
    public ushort u16le() {
        return (ushort) (ReadByte() | (ReadByte() << 8));
    }

    // Consumes two bytes of data in the big-endian format.
    public ushort u16be() {
        return (ushort) ((ReadByte() << 8) | ReadByte());
    }

    // Consumes three bytes of data in the little-endian format.
    public int u24le() {
        return (int) (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16));
    }

    // Consumes three bytes of data in the big-endian format.
    public int u24be() {
        return (int) ((ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
    }

    // Consumes four bytes of data in the little-endian format.
    public uint u32le() {
        return (uint) (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
    }

    // Consumes four bytes of data in the big-endian format.
    public uint u32be() {
        return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
    }

    // Consumes the specified struct.
    public T Struct<T>(bool bigEndian = false) where T : unmanaged {
        return Read(Marshal.SizeOf<T>()).ReadStruct<T>(0, bigEndian);
    }
}