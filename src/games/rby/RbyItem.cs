public class RbyItem : ROMObject {

    public Rby Game;

    public RbyItem(Rby game, byte id, string name) {
        Game = game;
        Name = name;
        Id = id;
    }
}

public class RbyItemStack {

    public RbyItem Item;
    public byte Quantity;

    public RbyItemStack(RbyItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
}
