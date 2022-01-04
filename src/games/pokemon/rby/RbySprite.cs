public enum RbySpriteMovement : byte {
    MovingRight = 0x1,
    MovingLeft = 0x2,
    MovingDown = 0x4,
    MovingUp = 0x8,

    FacingDown = 0x0,
    FacingUp = 0x4,
    FacingLeft = 0x8,
    FacingRight = 0xc,

    AnyDir = 0x00,
    UpDown = 0x01,
    LeftRight = 0x02,
    Down = 0xd0,
    Up = 0xd1,
    Left = 0xd2,
    Right = 0xd3,
    None = 0xff,

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
    public Action Direction;
    public byte Range;

    public bool CanBeMissable;
    public int MissableAddress;
    public int MissableBit;

    public RbySprite() { }

    // Constructor to call from subclasses (RbyTrainer, RbyItemBall)
    public RbySprite(RbySprite baseSprite, ReadStream data) {
        Map = baseSprite.Map;
        SpriteId = baseSprite.SpriteId;
        Y = baseSprite.Y;
        X = baseSprite.X;
        Movement = baseSprite.Movement;
        TextId = baseSprite.TextId;
        IsTrainer = baseSprite.IsTrainer;
        IsItem = baseSprite.IsItem;
        Direction = baseSprite.Direction;
        Range = baseSprite.Range;
    }

    public RbySprite(Rby game, RbyMap map, byte spriteId, ReadStream data) {
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

        if(IsTrainer) TextId &= 0xbf;
        if(IsItem) TextId &= 0x7f;

        if(Movement == RbySpriteMovement.Walk) {
            Range = rangeOrDirection;
        } else {
            switch((RbySpriteMovement) rangeOrDirection) {
                case RbySpriteMovement.Down: Direction = Action.Down; break;
                case RbySpriteMovement.Up: Direction = Action.Up; break;
                case RbySpriteMovement.Left: Direction = Action.Left; break;
                case RbySpriteMovement.Right: Direction = Action.Right; break;
                case RbySpriteMovement.None:
                    Movement = RbySpriteMovement.Turn;
                    goto default;
                default:
                    Direction = Action.None;
                    break;
            }
        }
    }
}