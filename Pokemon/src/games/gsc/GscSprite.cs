namespace Pokemon
{
    public enum GscSpriteMovement : byte
    {

        SpriteMovement00,
        Still,
        Wander,
        SpinrandomSlow,
        WalkUpDown,
        WalkLeftRight,
        StandingDown,
        StandingUp,
        StandingLeft,
        StandingRight,
        SpinrandomFast,
        Player,
        SpriteMovement0C,
        SpriteMovement0D,
        SpriteMovement0E,
        SpriteMovement0F,
        SpriteMovement10,
        SpriteMovement11,
        SpriteMovement12,
        Following,
        Scripted,
        Bigdollsym,
        Pokemon,
        Sudowoodo,
        SmashableRock,
        StrengthBoulder,
        Follownotexact,
        Shadow,
        Emote,
        Screenshake,
        Spincounterclockwise,
        Spinclockwise,
        Bigdollasym,
        Bigdoll,
        Boulderdust,
        Grass,
        SwimWander,
    }

    public enum GscSpriteType : byte
    {

        Script,
        Itemball,
        Trainer,
        SpriteType3,
        SpriteType4,
        SpriteType5,
        SpriteType6,
    }

    public class GscSprite
    {

        public GscMap Map;
        public byte Id;
        public byte X;
        public byte Y;
        public byte PictureId;
        public GscSpriteMovement MovementFunction;
        public byte MovementRadiusY;
        public byte MovementRadiusX;
        public byte H1;
        public byte H2;
        public byte Color;
        public GscSpriteType Function;
        public byte SightRange;
        public ushort ScriptPointer;
        public ushort EventFlag;

        public GscSprite(Gsc game, GscMap map, byte id, ByteStream data)
        {
            Map = map;
            Id = id;
            PictureId = data.u8();
            Y = (byte)(data.u8() - 4);
            X = (byte)(data.u8() - 4);
            MovementFunction = (GscSpriteMovement)data.u8();
            MovementRadiusY = data.Nybble();
            MovementRadiusX = data.Nybble();
            H1 = data.u8();
            H2 = data.u8();
            Color = data.Nybble();
            Function = (GscSpriteType)data.Nybble();
            SightRange = data.u8();
            ScriptPointer = data.u16le();
            EventFlag = data.u16le();
        }
    } 
}