public enum RbyEffect {

    NoAdditional,
    Unused01,
    PoisonSide1,
    DrainHp,
    BurnSide1,
    FreezeSide,
    ParalyzeSide1,
    Explode,
    DreamEater,
    MirrorMove,
    AttackUp1,
    DefenseUp1,
    SpeedUp1,
    SpecialUp1,
    AccuracyUp1,
    EvasionUp1,
    PayDay,
    Swift,
    AttackDown1,
    DefenseDown1,
    SpeedDown1,
    SpecialDown1,
    AccuracyDown1,
    EvasionDown1,
    Conversion,
    Haze,
    Bide,
    ThrashPetalDance,
    SwitchAndTeleport,
    TwoToFiveAttacks,
    Unused1e,
    FlinchSide1,
    Sleep,
    PoisonSide2,
    BurnSide2,
    Unused23,
    ParalyzeSide2,
    FlinchSide2,
    Ohko,
    Charge,
    SuperFang,
    SpecialDamage,
    Trapping,
    Fly,
    AttackTwice,
    JumpKick,
    Mist,
    FocusEnergy,
    Recoil,
    Confusion,
    AttackUp2,
    DefenseUp2,
    SpeedUp2,
    SpecialUp2,
    AccuracyUp2,
    EvasionUp2,
    Heal,
    Transform,
    AttackDown2,
    DefenseDown2,
    SpeedDown2,
    SpecialDown2,
    AccuracyDown2,
    EvasionDown2,
    LightScreen,
    Reflect,
    Poison,
    Paralyze,
    AttackDownSide,
    DefenseDownSide,
    SpeedDownSide,
    SpecialDownSide,
    Unused48,
    Unused49,
    Unused4a,
    Unused4b,
    ConfusionSide,
    Twineedle,
    Unused4e,
    Substitute,
    HyperBeam,
    Rage,
    Mimic,
    Metronome,
    LeechSeed,
    Splash,
    Disable,
}

public class RbyMove : ROMObject {

    public Rby Game;
    public byte Animation;
    public RbyEffect Effect;
    public byte Power;
    public RbyType Type;
    public byte Accuracy;
    public byte PP;

    public RbyMove(Rby game, ReadStream data, ReadStream name) {
        Game = game;
        Name = game.Charmap.Decode(name.Until(Charmap.Terminator));
        Id = data.u8();
        Effect = (RbyEffect) data.u8();
        Power = data.u8();
        Type = (RbyType) data.u8();
        Accuracy = data.u8();
        PP = data.u8();
    }
}
