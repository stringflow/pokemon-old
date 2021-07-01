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

public static class RbyTypeFunctions {

    public static bool IsSpecial(this RbyType type) {
        return type >= RbyType.Fire;
    }
}

public class RbySpecies : ROMObject {

    public Rby Game;
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
    public RbyMove[] BaseMoves;
    public GrowthRate GrowthRate;

    public RbySpecies(Rby game, byte indexNumber, ReadStream data) : this(game, indexNumber) {
        Game = game;
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
        BaseMoves = new RbyMove[] { Game.Moves[data.u8()],
                                    Game.Moves[data.u8()],
                                    Game.Moves[data.u8()],
                                    Game.Moves[data.u8()] };
        GrowthRate = (GrowthRate) data.u8();
        data.Seek(8); // TODO: HMs/TMs
    }

    // Missingno data
    public RbySpecies(Rby game, byte indexNumber) {
        Game = game;
        Name = game.Charmap.Decode(game.ROM.Subarray(game.SYM["MonsterNames"] + (indexNumber - 1) * 10, 10));
        Id = indexNumber;
    }
}
