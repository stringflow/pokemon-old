public class GscItem : NamedObject {

    public Gsc Game;
    public byte Id;

    public GscItem(Gsc game, byte id, ByteStream name) : base(name.Until(Charmap.Terminator), game.Charmap) {
        Game = game;
        Id = id;
    }
}

public class GscItemStack {

    public GscItem Item;
    public byte Quantity;

    public GscItemStack(GscItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
}