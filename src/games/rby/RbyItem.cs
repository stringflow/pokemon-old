public class RbyItem : ROMObject {

    public Rby Game;
    public int ExecutionPointer;
    public string ExecutionPointerLabel;

    public RbyItem(Rby game, byte id, string name) {
        Game = game;
        Name = name;
        Id = id;
        ExecutionPointer = 0x3 << 16 | game.ROM.u16le(game.SYM["ItemUsePtrTable"] + (byte) (id - 1) * 2);
        if(id >= 0xC4) {
            ExecutionPointer = game.SYM["ItemUseTMHM"];
        }

        if(game.SYM.Contains(ExecutionPointer)) ExecutionPointerLabel = game.SYM[ExecutionPointer];
    }
}

public class RbyItemStack {

    public RbyItem Item;
    public byte Quantity;

    public RbyItemStack(RbyItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
}
