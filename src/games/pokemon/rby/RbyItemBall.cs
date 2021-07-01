public class RbyItemBall : RbySprite {

    public RbyItem Item;

    public RbyItemBall(RbySprite baseSprite, ReadStream data) : base(baseSprite, data) {
        Item = Map.Game.Items[data.u8()];
    }

    public override string ToString() {
        return base.ToString() + " - " + Item.Name;
    }
}