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

    public string Title {
        get {
            ReadStream data = From(0x134);
            string title = "";
            while(true) {
                byte b = data.u8();
                if(b >= 128 || b == 0) break;
                title += (char) b;
            }
            return title;
        }
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
    public ReadStream From(int offset) {
        ReadStream stream = new ReadStream(Data);
        stream.Seek(offset, SeekOrigin.Begin);
        return stream;
    }

    public ushort u16le(int offset) {
        return Data.u16le(offset);
    }

    public ushort u16be(int offset) {
        return Data.u16be(offset);
    }

    public int u24le(int offset) {
        return Data.u24le(offset);
    }

    public int u24be(int offset) {
        return Data.u16be(offset);
    }

    public uint u32le(int offset) {
        return Data.u32le(offset);
    }

    public uint u32be(int offset) {
        return Data.u32be(offset);
    }

    public byte[] Subarray(int offset, int length) {
        return Data.Subarray(offset, length);
    }

    public byte this[string addr] {
        get { return Data[Symbols[addr]]; }
        set { Data[Symbols[addr]] = value; }
    }

    public ReadStream From(string addr) {
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

public class SYM : BiDictionary<string, int> {

    public SYM(string file) : base() {
        string[] lines = File.ReadAllLines(file);
        int previousBank = -1;
        int previousAddr = -1;
        string previousLabel = "";
        for(int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            // Any line starting with a semicolon is a comment.
            if(line.StartsWith(";")) continue;

            // Format: bb:aaaa label
            Match match = Regex.Match(line, "([0-9-a-f]+):([0-9-a-f]+) (.+)");
            int bank = Convert.ToInt32(match.Groups[1].Value, 16);
            int addr = Convert.ToInt32(match.Groups[2].Value, 16);
            string label = match.Groups[3].Value;
            Add(label, bank << 16 | addr);

            if(previousBank != -1) {
                for(int newAddr = previousAddr + 1; newAddr < addr && newAddr <= 0x8000; newAddr++) {
                    int offset = (newAddr - previousAddr);
                    int newAddress = previousBank << 16 | newAddr;
                    Add(string.Format("{0}+{1:x4}", previousLabel, offset), newAddress);
                }
            }

            previousBank = bank;
            previousAddr = addr;
            previousLabel = label;
        }
    }
}