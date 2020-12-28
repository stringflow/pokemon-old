public enum Joypad : byte {

    None = 0x0,
    A = 0x1,
    B = 0x2,
    Select = 0x4,
    Start = 0x8,
    Right = 0x10,
    Left = 0x20,
    Up = 0x40,
    Down = 0x80,
    All = 0xff,
}

public static class JoypadFunctions {

    public static Joypad Opposite(this Joypad joypad) {
        switch(joypad) {
            case Joypad.A: return Joypad.B;
            case Joypad.B: return Joypad.A;
            case Joypad.Right: return Joypad.Left;
            case Joypad.Left: return Joypad.Right;
            case Joypad.Up: return Joypad.Down;
            case Joypad.Down: return Joypad.Up;
            default: return joypad;
        }
    }
}
