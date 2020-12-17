public class RbyItem {

    public Rby Game;
    public byte Id;
    public string Name;

    public RbyItem(Rby game, byte id, string name) {
        Game = game;
        Id = id;
        Name = name;
    }
}

public class RbyItemStack {

    public RbyItem Item;
    public byte Quantity;

    public RbyItemStack(RbyItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
}
