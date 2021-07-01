public enum GscType {

    Normal,
    Fighting,
    Flying,
    Poison,
    Ground,
    Rock,
    Bird,
    Bug,
    Ghost,
    Steel,
    CurseType = 19,
    Fire,
    Water,
    Grass,
    Electric,
    Psychic,
    Ice,
    Dragon,
    Dark
}

public enum GscEggGroup {

    Monster = 1,
    Water1,
    Bug,
    Flying,
    Ground,
    Fairy,
    Plant,
    Humanshape,
    Water3,
    Mineral,
    Indeterminate,
    Water2,
    Ditto,
    Dragon,
    None,
}

public class GscSpecies : ROMObject {

    public Gsc Game;
    public byte BaseHP;
    public byte BaseAttack;
    public byte BaseDefense;
    public byte BaseSpeed;
    public byte BaseSpecialAttack;
    public byte BaseSpecialDefense;
    public GscType Type1;
    public GscType Type2;
    public byte CatchRate;
    public byte BaseExp;
    public byte Item1;
    public byte Item2;
    public byte GenderRatio;
    public byte Unknown1;
    public byte HatchCycles;
    public byte Unknown2;
    public byte FrontSpriteWidth;
    public byte FrontSpriteHeight;
    public GrowthRate GrowthRate;
    public GscEggGroup EggGroup1;
    public GscEggGroup EggGroup2;

    public GscSpecies(Gsc game, ReadStream data, ReadStream name) { // Names are padded to 10 length using terminator characters.
        Game = game;
        Name = game.Charmap.Decode(name.Read(10));
        Id = data.u8();
        BaseHP = data.u8();
        BaseAttack = data.u8();
        BaseDefense = data.u8();
        BaseSpeed = data.u8();
        BaseSpecialAttack = data.u8();
        BaseSpecialDefense = data.u8();
        Type1 = (GscType) data.u8();
        Type2 = (GscType) data.u8();
        CatchRate = data.u8();
        BaseExp = data.u8();
        Item1 = data.u8();
        Item2 = data.u8();
        GenderRatio = data.u8();
        Unknown1 = data.u8();
        HatchCycles = data.u8();
        Unknown2 = data.u8();
        FrontSpriteWidth = data.Nybble();
        FrontSpriteHeight = data.Nybble();
        data.Seek(4); // 4 unused bytes
        GrowthRate = (GrowthRate) data.u8();
        EggGroup1 = (GscEggGroup) data.Nybble();
        EggGroup2 = (GscEggGroup) data.Nybble();
        data.Seek(8); // TODO: HMs/TMs
    }
}
