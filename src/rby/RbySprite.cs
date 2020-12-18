public enum RbySpriteMovement : byte {

    Turn = 0xfd,
    Walk = 0xfe,
    Stay = 0xff,
}

public class RbySprite {

    public RbyMap Map;
    public byte SpriteId;
    public byte PictureId;
    public byte Y;
    public byte X;
    public RbySpriteMovement Movement;
    public byte TextId;
    public bool IsTrainer;
    public bool IsItem;
    public byte Direction; // TODO: Enum
    public byte Range;

    public RbySprite(Rby game, RbyMap map, byte spriteId, ByteStream data) {
        Map = map;
        SpriteId = spriteId;
        PictureId = data.u8();
        Y = (byte) (data.u8() - 4);
        X = (byte) (data.u8() - 4);
        Movement = (RbySpriteMovement) data.u8();
        byte rangeOrDirection = data.u8();
        TextId = data.u8();
        IsTrainer = (TextId & 0x40) != 0;
        IsItem = (TextId & 0x80) != 0;

        if(Movement == RbySpriteMovement.Walk) {
            Range = rangeOrDirection;
        } else {
            Direction = rangeOrDirection;
            if(Direction == 0xff) {
                Movement = RbySpriteMovement.Turn;
            }
        }
    }
}