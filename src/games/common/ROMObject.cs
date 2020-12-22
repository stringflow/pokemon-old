// An object parsed from ROM. (species, items, moves, etc.)
public class ROMObject {

    public byte Id;
    public string Name;

    public override string ToString() {
        return Name;
    }

    public static implicit operator byte(ROMObject obj) {
        return obj.Id;
    }
}