public enum GscPocket {

    Item = 1,
    KeyItem = 2,
    Ball = 3,
    TMHM = 4,
}

public class GscItem : ROMObject {

    public Gsc Game;
    public ushort Price;
    public byte HeldEffect;
    public byte Parameter;
    public byte Property;
    public GscPocket Pocket;
    public byte FieldMenu;
    public byte BattleMenu;
    public int ExecutionPointer;
    public string ExecutionPointerLabel;

    public GscItem(Gsc game, byte id, ReadStream name, ReadStream attributes) {
        Game = game;
        Name = game.Charmap.Decode(name.Until(Charmap.Terminator));
        Id = id;
        Price = attributes.u16le();
        HeldEffect = attributes.u8();
        Parameter = attributes.u8();
        Property = attributes.u8();
        Pocket = (GscPocket) attributes.u8();
        FieldMenu = attributes.Nybble();
        BattleMenu = attributes.Nybble();

        if(id <= 0xb3) {
            ExecutionPointer = 0x3 << 16 | game.ROM.u16le(game.SYM["ItemEffects"] + (byte) (id - 1) * 2);
            if(game.SYM.Contains(ExecutionPointer)) ExecutionPointerLabel = game.SYM[ExecutionPointer];
        }
    }
}

public class GscItemStack {

    public GscItem Item;
    public byte Quantity;

    public GscItemStack(GscItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
}