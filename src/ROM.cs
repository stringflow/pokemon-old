using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

public class ROM {

    public const int BankSize = 0x4000;

    public byte[] Data;
    public byte[] Header;

    public SYM Symbols;

    public byte HeaderChecksum {
        get { return Header[0x14d]; }
    }

    public int GlobalChecksum {
        get { return Header[0x14e] << 8 | Header[0x14f]; }
    }

    /*
        The raw ROM image is rearranged in the following way:
            home bank
            bank 0
            0xff * 0x4000 (vram / sram)
            0x00 * 0x4000 (wram)
            home bank
            bank 1
            0xff * 0x4000 (vram / sram)
            0x00 * 0x4000 (wram)
            ...
            (repeat until bank ff, so wrapping as needed)
        
        Rearranging the ROM this way serves multiple purposes:
            (1) with glitched ROM access reading around 0x7fxx could run into disabled VRAM which should return 0xff.
            (2) with glitched ROM access you might try to read from banks >3f.
            (3) allows ROM address and CPU breakpoints to use the same value. (bank << 16 | address)
    */

    public ROM(string path) {
        byte[] contents = File.ReadAllBytes(path);
        int numBanks = contents.Length / BankSize;

        Data = new byte[BankSize * 4 * 0x100];
        Header = contents.Subarray(0, 0x150);

        for(int bank = 0, offset = 0; bank <= 0xff; bank++) {
            Array.Copy(contents, 0, Data, offset, BankSize);                            // home
            offset += BankSize;
            Array.Copy(contents, (bank % numBanks) * BankSize, Data, offset, BankSize); // bank n
            offset += BankSize;
            Array.Fill(Data, (byte) 0xff, offset, BankSize);                            // vram / sram
            offset += BankSize;
            Array.Fill(Data, (byte) 0x00, offset, BankSize);                            // wram
            offset += BankSize;
        }
    }

    public bool HeaderChecksumMatches() {
        int csum = 0;
        for(int i = 0x134; i < 0x14d; i++) {
            csum -= Header[i] + 1;
        }
        return (csum & 0xff) == HeaderChecksum;
    }

    public byte this[int index] {
        get { return Data[index]; }
        set { Data[index] = value; }
    }

    // Returns a stream of the ROM data seeked to the specified offset.
    public ByteStream From(int offset) {
        ByteStream stream = new ByteStream(Data);
        stream.Seek(offset, SeekOrigin.Begin);
        return stream;
    }

    public ushort u16le(int offset) {
        return (ushort) (Data[offset] | (Data[offset + 1] << 8));
    }

    public ushort u16be(int offset) {
        return (ushort) ((Data[offset] << 8) | Data[offset]);
    }

    public int u24le(int offset) {
        return (ushort) (Data[offset] | (Data[offset + 1] << 8) | (Data[offset + 2] << 16));
    }

    public int u24be(int offset) {
        return (ushort) ((Data[offset] << 16) | (Data[offset + 1] << 8) | Data[offset + 2]);
    }

    public uint u32le(int offset) {
        return (uint) (Data[offset] | (Data[offset + 1] << 8) | (Data[offset + 2] << 16) | (Data[offset + 3] << 24));
    }

    public uint u32be(int offset) {
        return (uint) ((Data[offset] << 24) | (Data[offset + 1] << 16) | (Data[offset + 2] << 8) | Data[offset + 3]);
    }

    public byte[] Subarray(int offset, int length) {
        return Data.Subarray(offset, length);
    }

    public byte this[string addr] {
        get { return Data[Symbols[addr]]; }
        set { Data[Symbols[addr]] = value; }
    }

    public ByteStream From(string addr) {
        return From(Symbols[addr]);
    }

    public ushort u16le(string addr) {
        return u16le(Symbols[addr]);
    }

    public ushort u16be(string addr) {
        return u16be(Symbols[addr]);
    }

    public int u24le(string addr) {
        return u24le(Symbols[addr]);
    }

    public int u24be(string addr) {
        return u24be(Symbols[addr]);
    }

    public uint u32be(string addr) {
        return u32be(Symbols[addr]);
    }

    public uint u32le(string addr) {
        return u32le(Symbols[addr]);
    }

    public byte[] Subarray(string addr, int length) {
        return Subarray(Symbols[addr], length);
    }
}

public class ByteStream : MemoryStream {

    public bool LowerNybble;

    public ByteStream() : base() {
    }

    public ByteStream(byte[] data) : base(data) {
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

    // Reads until the value of 'terminator' is encountered. Returns all bytes read including the terminator.
    public byte[] Until(byte terminator) {
        int length = 0;
        do {
            length++;
        } while(ReadByte() != terminator);
        Seek(-length, SeekOrigin.Current);
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

public class SYM : Map<string, int> {

    public SYM(string file) : base() {
        string[] lines = File.ReadAllLines(file);
        for(int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            // Any line starting with a semicolon is a comment.
            if(line.StartsWith(";")) continue;

            // Format: bb:aaaa label
            Match match = Regex.Match(line, "([0-9-a-f]+):([0-9-a-f]+) (.+)");
            byte bank = Convert.ToByte(match.Groups[1].Value, 16);
            ushort addr = Convert.ToUInt16(match.Groups[2].Value, 16);
            string label = match.Groups[3].Value;
            Add(label, bank << 16 | addr);
        }
    }
}