public class RedBlue : Rby {

    public RedBlue(string rom, bool speedup = false) : base(rom, speedup ? SpeedupFlags.All : SpeedupFlags.None) { }
}

public class Red : RedBlue {

    public Red(bool speedup = false, string rom = "roms/pokered.gbc") : base(rom, speedup) { }

    public override byte[][] BGPalette() {
        return new byte[][] {
                    new byte[] { 248, 248, 248 },
                    new byte[] { 225, 128, 150 },
                    new byte[] { 127, 56, 72 },
                    new byte[] { 0, 0, 0 }};
    }

    public override byte[][] ObjPalette() {
        return new byte[][] {
                    new byte[] { 16, 96, 16 },
                    new byte[] { 248, 248, 248 },
                    new byte[] { 131, 198, 86 },
                    new byte[] { 0, 0, 0 }};
    }
}

public class Blue : RedBlue {

    public Blue(bool speedup = false, string rom = "roms/pokeblue.gbc") : base(rom, speedup) { }

    public override byte[][] BGPalette() {
        return new byte[][] {
                    new byte[] { 248, 248, 248 },
                    new byte[] { 113, 182, 208 },
                    new byte[] { 15, 62, 170 },
                    new byte[] { 0, 0, 0 }};
    }

    public override byte[][] ObjPalette() {
        return new byte[][] {
                    new byte[] { 127, 56, 72 },
                    new byte[] { 248, 248, 248 },
                    new byte[] { 225, 128, 150 },
                    new byte[] { 0, 0, 0 }};
    }
}