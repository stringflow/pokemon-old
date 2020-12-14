// An object parsed from ROM that has a name. (species, items, moves, etc.)
public class NamedObject {

    public byte[] RawName;
    public string Name;

    public NamedObject(byte[] raw, Charmap charmap) {
        RawName = raw;
        Name = charmap.Decode(raw);
    }

    public override string ToString() {
        return Name;
    }
}