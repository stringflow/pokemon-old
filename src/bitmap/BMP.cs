using System;
using System.Runtime.InteropServices;

public static class BMP {

    public const ushort BMPSignature = 0x4D42;
    public static readonly int HeaderSize = Marshal.SizeOf<BMPHeader>();
    public static readonly int InfoHeaderSize = HeaderSize - 14;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BMPHeader {

        public ushort Signature;
        public uint FileSize;
        public ushort Reserved1;
        public ushort Reserved2;
        public uint DataOffset;
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitsPerPixel;
        public uint Compression;
        public uint ImageSize;
        public uint HorzResolution;
        public uint VertResolution;
        public uint NumColors;
        public uint NumImportantColors;
    }

    public static void Decode(ReadStream data, Bitmap dest) {
        BMPHeader header = data.Struct<BMPHeader>();
        Debug.Assert(header.Signature == BMPSignature, "Specified file was not a BMP file.");
        Debug.Assert(header.Size == InfoHeaderSize, "The BMP file contains an unsupported info header.");
        Debug.Assert(header.Compression == 0, "Only uncompressed BMP files are supported.");
        Debug.Assert(header.BitsPerPixel == 32, "Only 32-bit colors are supported.");
        Debug.Assert(header.FileSize - header.DataOffset == header.Width * header.Height * 4, "The BMP file is missing pixel data.");
        dest.Width = header.Width;
        dest.Height = header.Height;
        dest.Pixels = RemapData(data.Read(dest.Width * dest.Height * 4), dest.Width, dest.Height);
    }

    public static byte[] Encode(Bitmap bitmap) {
        byte[] data = new byte[bitmap.Pixels.Length + HeaderSize];
        BMPHeader header = new BMPHeader {
            Signature = BMPSignature,
            FileSize = (uint) data.Length,
            DataOffset = (uint) HeaderSize,
            Size = (uint) (InfoHeaderSize),
            Width = bitmap.Width,
            Height = bitmap.Height,
            Planes = 1,
            BitsPerPixel = 32,
        };
        Array.Copy(header.ToBytes(), 0, data, 0, HeaderSize);
        Array.Copy(RemapData(bitmap.Pixels, bitmap.Width, bitmap.Height), 0, data, HeaderSize, bitmap.Pixels.Length);
        return data;
    }

    // Switches the red and blue channels and flips the image horizontally.
    private static byte[] RemapData(byte[] src, int width, int height) {
        byte[] dest = new byte[width * height * 4];
        for(int i = 0; i < width * height; i++) {
            int x = i % width;
            int y = height - 1 - i / width;
            int pixelOffset = (x + y * width) * 4;
            dest[pixelOffset + 0] = src[i * 4 + 2];
            dest[pixelOffset + 1] = src[i * 4 + 1];
            dest[pixelOffset + 2] = src[i * 4 + 0];
            dest[pixelOffset + 3] = src[i * 4 + 3];
        }
        return dest;
    }
}