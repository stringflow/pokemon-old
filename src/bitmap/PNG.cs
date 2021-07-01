using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;

public unsafe static class PNG {

    public static ulong PNGSignature = 0x89504E470D0A1A0A;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGHeader {
        public ulong Signature;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGChunkHeader {
        public int Length;
        public fixed byte Type[4];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGChunkFooter {
        public uint CRC;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGIHDR {
        public int Width;
        public int Height;
        public byte BitDepth;
        public byte ColorType;
        public byte CompressionMethod;
        public byte FilterMethod;
        public byte InterlaceMethod;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGIDATHeader {
        public byte ZLibMethodFlags;
        public byte AdditionalFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PNGIDATFooter {
        public uint CheckValue;
    }

    public static void Decode(ReadStream data, Bitmap dest) {
        PNGHeader header = data.Struct<PNGHeader>(true);
        Debug.Assert(header.Signature == PNGSignature, "Specified file was not a PNG file.");

        ReadStream idatStream = new ReadStream();

        while(true) {
            PNGChunkHeader chunkHeader = data.Struct<PNGChunkHeader>(true);
            string chunkType = Encoding.ASCII.GetString(chunkHeader.Type, 4);
            byte[] chunkBytes = data.Read(chunkHeader.Length);
            ReadStream chunkData = new ReadStream(chunkBytes);
            PNGChunkFooter chunkFooter = data.Struct<PNGChunkFooter>(true);

            Debug.Assert(Crc32(chunkHeader.ToBytes().Concat(chunkBytes).ToArray(), 4) == chunkFooter.CRC, chunkType + " chunk's CRC mismatched!");

            switch(chunkType) {
                case "IHDR":
                    PNGIHDR ihdr = chunkData.Struct<PNGIHDR>(true);
                    Debug.Assert(ihdr.BitDepth == 8 && ihdr.ColorType == 6 && ihdr.CompressionMethod == 0 &&
                                 ihdr.FilterMethod == 0 && ihdr.InterlaceMethod == 0, "The specified PNG file uses an unsupported format.");
                    dest.Width = ihdr.Width;
                    dest.Height = ihdr.Height;
                    dest.Pixels = new byte[ihdr.Width * ihdr.Height * 4];
                    break;
                case "IDAT":
                    idatStream.Write(chunkData.Read(chunkHeader.Length));
                    break;
                case "IEND":
                    idatStream.Seek(0, SeekOrigin.Begin);
                    PNGIDATHeader idatHeader = idatStream.Struct<PNGIDATHeader>(true);
                    byte[] idatData;
                    PNGIDATFooter idatFooter;
                    using(MemoryStream target = new MemoryStream())
                    using(DeflateStream decompressionStream = new DeflateStream(idatStream, CompressionMode.Decompress)) {
                        decompressionStream.CopyTo(target);
                        idatData = target.ToArray();
                        idatStream.Seek(-4);
                        idatFooter = idatStream.Struct<PNGIDATFooter>(true);
                    }

                    Debug.Assert(idatFooter.CheckValue == Alder32(idatData), "IDAT chunk compression check value mismatch!");
                    int scanlineSize = dest.Width * 4;
                    for(int scanline = 0; scanline < dest.Height; scanline++) {
                        int offset = scanline * scanlineSize;
                        Array.Copy(idatData, offset + scanline + 1, dest.Pixels, offset, scanlineSize);
                    }
                    return;
            }
        }
    }

    public static byte[] Encode(Bitmap bitmap) {
        WriteStream outStream = new WriteStream();

        PNGHeader header = new PNGHeader {
            Signature = PNGSignature,
        };

        outStream.Write(header, true);
        WriteChunk(outStream, "IHDR", new PNGIHDR {
            Width = bitmap.Width,
            Height = bitmap.Height,
            BitDepth = 8,
            ColorType = 6,
            CompressionMethod = 0,
            FilterMethod = 0,
            InterlaceMethod = 0,
        }.ToBytes(true));

        byte[] scanlines = new byte[bitmap.Width * bitmap.Height * 4 + bitmap.Height];
        int scanlineSize = bitmap.Width * 4;
        for(int scanline = 0; scanline < bitmap.Height; scanline++) {
            int offset = scanline * scanlineSize;
            Array.Copy(bitmap.Pixels, offset, scanlines, offset + scanline + 1, scanlineSize);
        }

        using(MemoryStream idatStream = new MemoryStream()) {
            idatStream.Write(new PNGIDATHeader() {
                ZLibMethodFlags = 0x78,
                AdditionalFlags = 0x1,
            }.ToBytes(true));

            using(DeflateStream compressionStream = new DeflateStream(idatStream, CompressionMode.Compress, true))
                compressionStream.Write(scanlines);

            idatStream.Write(new PNGIDATFooter() {
                CheckValue = Alder32(scanlines),
            }.ToBytes(true));

            WriteChunk(outStream, "IDAT", idatStream.ToArray());
        }

        WriteChunk(outStream, "IEND", new byte[0]);

        return outStream.ToArray();
    }

    private static void WriteChunk(WriteStream stream, string type, byte[] data) {
        PNGChunkHeader header = new PNGChunkHeader {
            Length = data.Length,
        };
        for(int i = 0; i < 4; i++) header.Type[i] = (byte) type[i];
        PNGChunkFooter footer = new PNGChunkFooter {
            CRC = Crc32(header.ToBytes().Concat(data).ToArray(), 4),
        };

        stream.Write(header, true);
        stream.Write(data);
        stream.Write(footer, true);
    }

    private static uint Crc32(byte[] data, int index) {
        uint crc = 0xffffffff;
        for(int i = index; i < data.Length; i++) {
            crc ^= data[i];
            for(int j = 0; j < 8; j++) {
                uint t = ~((crc & 1) - 1);
                crc = (crc >> 1) ^ (0xedb88320 & t);
            }
        }

        return ~crc;
    }

    private static uint Alder32(byte[] data) {
        const uint A32Mod = 65521;
        uint s1 = 1;
        uint s2 = 0;
        foreach(byte b in data) {
            s1 = (s1 + b) % A32Mod;
            s2 = (s2 + s1) % A32Mod;
        }

        return s2 << 16 | s1;
    }
}