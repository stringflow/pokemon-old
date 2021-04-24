namespace Pokemon
{
    public class GscItem : ROMObject
    {

        public Gsc Game;

        public GscItem(Gsc game, byte id, ByteStream name)
        {
            Game = game;
            Name = game.Charmap.Decode(name.Until(Charmap.Terminator));
            Id = id;
        }
    }

    public class GscItemStack
    {

        public GscItem Item;
        public byte Quantity;

        public GscItemStack(GscItem item, byte quantity = 1) => (Item, Quantity) = (item, quantity);
    } 
}