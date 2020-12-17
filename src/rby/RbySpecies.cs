public enum RbyType {

    Normal,
    Fighting,
    Flying,
    Poison,
    Ground,
    Rock,
    Bird,
    Bug,
    Ghost,
    Fire = 20,
    Water,
    Grass,
    Electric,
    Psyshic,
    Ice,
    Dragon
}

// Should generalize growth rate enum between gen 1/2?
public enum RbyGrowthRate {

    MediumFast,
    SlightlyFast,
    SlightlySlow,
    MediumSlow,
    Fast,
    Slow
}

public class RbySpecies : NamedObject {

    public Rby Game;
    public byte IndexNumber;
    public byte PokedexNumber;
    public byte BaseHP;
    public byte BaseAttack;
    public byte BaseDefense;
    public byte BaseSpeed;
    public byte BaseSpecial;
    public RbyType Type1;
    public RbyType Type2;
    public byte CatchRate;
    public byte BaseExp;
    public byte FrontSpriteWidth;
    public byte FrontSpriteHeight;
    public ushort FrontSpritePointer;
    public ushort BackSpritePointer;
    public RbyMove BaseMove1;
    public RbyMove BaseMove2;
    public RbyMove BaseMove3;
    public RbyMove BaseMove4;
    public RbyGrowthRate GrowthRate;
    public byte BaseTM;
    public byte BaseHM;

    public RbySpecies(Rby game, byte indexNumber, ByteStream data, ByteStream name) : base(name.Read(10), game.Charmap) {
        Game = game;
        IndexNumber = indexNumber;
        PokedexNumber = data.u8();
        BaseHP = data.u8();
        BaseAttack = data.u8();
        BaseDefense = data.u8();
        BaseSpeed = data.u8();
        BaseSpecial = data.u8();
        Type1 = (RbyType) data.u8();
        Type2 = (RbyType) data.u8();
        CatchRate = data.u8();
        BaseExp = data.u8();
        FrontSpriteWidth = data.Nybble();
        FrontSpriteHeight = data.Nybble();
        FrontSpritePointer = data.u16le();
        BackSpritePointer = data.u16le();
        BaseMove1 = Game.Moves[data.u8()];
        BaseMove2 = Game.Moves[data.u8()];
        BaseMove3 = Game.Moves[data.u8()];
        BaseMove4 = Game.Moves[data.u8()];
        GrowthRate = (RbyGrowthRate) data.u8();
        BaseTM = data.u8();
        BaseHM = data.u8();
    }

    // Missingno data
    public RbySpecies(Rby game, byte indexNumber, ByteStream name) : base(name.Read(10), game.Charmap) {
        Name = string.Format("{0}{1:X2}", Name, indexNumber);
        Game = game;
        IndexNumber = indexNumber;
    }
}
