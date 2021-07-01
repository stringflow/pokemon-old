using System.Collections.Generic;

public static class LZ {

    public const int LzEnd = 0xff;
    public const int LzLiteral = 0;
    public const int LzIterate = 1;
    public const int LzAlternate = 2;
    public const int LzBlank = 3;
    public const int LzRepeat = 4;
    public const int LzFlip = 5;
    public const int LzReverse = 6;
    public const int LzLong = 7;

    public static byte[] Decompress(byte[] data) {
        return Decompress(new ReadStream(data));
    }

    public static byte[] Decompress(ReadStream compressed) {
        List<byte> decompressed = new List<byte>();
        while(true) {
            // If an ff-byte is encountered the decompression has completed.
            if(compressed.Peek() == LzEnd) {
                break;
            }

            // Bits 5-7 are occupied by control command.
            int command = (compressed.Peek() & 0xe0) >> 5;
            int length = 1;

            // The long command is used when 5 bits aren't enough.
            if(command == LzLong) {
                // Bits 2-4 contain the new control code.
                command = (compressed.Peek() & 0x1c) >> 2;
                // Bits 0-1 are appended to a new byte as bits 8-9, allowing a 10-bit operand.
                length += (compressed.u8() & 0x3) << 8;
                length += compressed.u8();
            } else {
                // If not a long command, bits 0-5 contain the command's operand.
                length += (compressed.u8() & 0x1f);
            }

            switch(command) {
                case LzLiteral: Literal(compressed, decompressed, length); break;
                case LzIterate: Iterate(compressed, decompressed, length); break;
                case LzAlternate: Alternate(compressed, decompressed, length); break;
                case LzBlank: Blank(compressed, decompressed, length); break;
                case LzRepeat: Repeat(compressed, decompressed, length, 1, false); break;
                case LzFlip: Repeat(compressed, decompressed, length, 1, true); break;
                case LzReverse: Repeat(compressed, decompressed, length, -1, false); break;
            }
        }

        return decompressed.ToArray();
    }

    public static void Literal(ReadStream compressed, List<byte> decompressed, int length) {
        // Copy 'length' bytes directly to the decompressed stream.
        decompressed.AddRange(compressed.Read(length));
    }

    public static void Iterate(ReadStream compressed, List<byte> decompressed, int length) {
        // Write the following byte 'length' times to the decompressed stream.
        byte b = compressed.u8();
        for(int i = 0; i < length; i++) {
            decompressed.Add(b);
        }
    }

    public static void Alternate(ReadStream compressed, List<byte> decompressed, int length) {
        // Alternate between the two following bytes 'length' times and write them to the decompressed stream.
        byte b1 = compressed.u8();
        byte b2 = compressed.u8();
        for(int i = 0; i < length; i++) {
            decompressed.Add((i & 1) == 0 ? b1 : b2);
        }
    }

    public static void Blank(ReadStream compressed, List<byte> decompressed, int length) {
        // Write 'length' number of zeros to the decompressed stream.
        for(int i = 0; i < length; i++) {
            decompressed.Add(0);
        }
    }

    public static void Repeat(ReadStream compressed, List<byte> decompressed, int length, int direction, bool flipped) {
        // Repeater commands repeat any data already present in the decompressed stream.
        // They take an additional singed operand to mark the relative starting point.
        int offset = 0;
        if(compressed.Peek() >= 0x80) {
            // If the operand is negative, it wraps around from the current position.
            offset = compressed.u8() & 0x7f;
            offset = decompressed.Count - offset - 1;
        } else {
            // For positive operands, a 16-bit offset is used.
            offset = compressed.u16be();
        }

        for(int i = 0; i < length; i++) {
            byte b = decompressed[offset + i * direction];
            // Reverse the bits if the command desires it.
            if(flipped) b = (byte) (((b * 0x0802LU & 0x22110LU) | (b * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
            decompressed.Add(b);
        }
    }
}