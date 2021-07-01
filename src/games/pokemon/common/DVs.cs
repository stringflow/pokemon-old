public class DVs {

    private ushort Value;

    public byte HP {
        get { return (byte) (((Value >> 9) & 8) | ((Value >> 6) & 4) | ((Value >> 3) & 2) | (Value & 1)); }
    }

    public byte Attack {
        get { return (byte) ((Value >> 12) & 0xf); }
        set { Value = (byte) ((Value & 0x0fff) | (value << 12)); }
    }

    public byte Defense {
        get { return (byte) ((Value >> 8) & 0xf); }
        set { Value = (byte) ((Value & 0xf0ff) | (value << 8)); }
    }

    public byte Speed {
        get { return (byte) ((Value >> 4) & 0xf); }
        set { Value = (byte) ((Value & 0xff0f) | (value << 4)); }
    }

    public byte Special {
        get { return (byte) ((Value) & 0xf); }
        set { Value = (byte) ((Value & 0xfff0) | value); }
    }

    public byte Upper {
        get { return (byte) (Value >> 8); }
        set { Value = (byte) ((Value & 0x00ff) | value); }
    }

    public byte Lower {
        get { return (byte) (Value & 0xff); }
        set { Value = (byte) ((Value & 0xff00) | value); }
    }

    public override string ToString() {
        return string.Format("0x{0:x4}", Value);
    }

    public static implicit operator DVs(ushort value) { return new DVs { Value = value }; }
    public static implicit operator ushort(DVs dvs) { return dvs.Value; }
}