public class GscWarp {

    public GscMap Map;
    public byte Y;
    public byte X;
    public byte MapGroup;
    public byte MapNumber;
    public byte Index;
    public byte DestinationIndex;

    public bool Allowed;

    public ushort MapId {
        get { return (ushort) ((MapGroup << 8) | MapNumber); }
    }

    public GscWarp(Gsc game, GscMap map, byte index, ReadStream data) {
        Map = map;
        Index = index;
        Y = data.u8();
        X = data.u8();
        DestinationIndex = (byte) (data.u8() - 1);
        MapGroup = data.u8();
        MapNumber = data.u8();
        Allowed = false;
    }
}

public class GscCoordEvent {

    public GscMap Map;
    public byte Y;
    public byte X;
    public byte SceneId;
    public ushort ScriptPointer;

    public GscCoordEvent(Gsc game, GscMap map, ReadStream data) {
        Map = map;
        SceneId = data.u8();
        Y = data.u8();
        X = data.u8();
        data.Seek(1);
        ScriptPointer = data.u16le();
        data.Seek(2);
    }
}

public enum GscBGEventType : byte {

    Read,
    Up,
    Down,
    Right,
    Left,
    IfSet,
    IfNotSet,
    Item,
    Copy,
}

public class GscBGEvent {

    public GscMap Map;
    public byte Y;
    public byte X;
    public GscBGEventType Function;
    public ushort ScriptPointer;

    public GscBGEvent(Gsc game, GscMap map, ReadStream data) {
        Y = data.u8();
        X = data.u8();
        Function = (GscBGEventType) data.u8();
        ScriptPointer = data.u16le();
    }
}