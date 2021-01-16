using System;

public class RbyPokemon {

    public RbySpecies Species;

    public byte Status;
    public byte BattleStatus1;
    public byte BattleStatus2;
    public byte BattleStatus3;

    public RbyMove[] Moves;
    public byte[] PP;

    public DVs DVs;
    public byte Level;
    public int Experience;

    public ushort MaxHP;
    public ushort HP;
    public ushort Attack;
    public ushort Defense;
    public ushort Speed;
    public ushort Special;
    public ushort HPExp;
    public ushort AttackExp;
    public ushort DefenseExp;
    public ushort SpeedExp;
    public ushort SpecialExp;
    public ushort UnmodifiedMaxHP;
    public ushort UnmodifiedAttack;
    public ushort UnmodifiedDefense;
    public ushort UnmodifiedSpeed;
    public ushort UnmodifiedSpecial;
    public byte AttackModifider;
    public byte DefenseModifider;
    public byte SpeedModifider;
    public byte SpecialModifider;
    public byte AccuracyModifider;
    public byte EvasionModifider;

    public int SleepCounter { get { return Status & 7; } }
    public bool Asleep { get { return SleepCounter > 0; } }
    public bool Poisoned { get { return (Status & 0x08) > 0; } }
    public bool Burned { get { return (Status & 0x10) > 0; } }
    public bool Frozen { get { return (Status & 0x20) > 0; } }
    public bool Paralyzed { get { return (Status & 0x40) > 0; } }

    public bool StoringEnergy { get { return (BattleStatus1 & 0x01) > 0; } }          // e.g. Bide
    public bool ThrashingAbout { get { return (BattleStatus1 & 0x02) > 0; } }         // e.g. Thrash, Pedal Dance
    public bool AttackingMultipleTimes { get { return (BattleStatus1 & 0x04) > 0; } } // e.g. Double Kick, Fury Attack
    public bool Flinched { get { return (BattleStatus1 & 0x08) > 0; } }
    public bool ChargingUp { get { return (BattleStatus1 & 0x10) > 0; } }             // e.g. Solar Beam, fly
    public bool UsingTrappingMove { get { return (BattleStatus1 & 0x20) > 0; } }      // e.g. Wrap, Fire Spin
    public bool Invulnerable { get { return (BattleStatus1 & 0x40) > 0; } }           // e.g. Fly/Dig
    public bool Confused { get { return (BattleStatus1 & 0x80) > 0; } }

    public bool XAccuracyEffect { get { return (BattleStatus2 & 0x01) > 0; } }
    public bool ProtectedByMist { get { return (BattleStatus2 & 0x02) > 0; } }
    public bool FocusEnergyEffect { get { return (BattleStatus2 & 0x04) > 0; } }
    public bool SubstituteActive { get { return (BattleStatus2 & 0x10) > 0; } }
    public bool Recharging { get { return (BattleStatus2 & 0x20) > 0; } }
    public bool UsingRage { get { return (BattleStatus2 & 0x40) > 0; } }
    public bool Seeded { get { return (BattleStatus2 & 0x80) > 0; } }

    public bool BadlyPoisoned { get { return (BattleStatus3 & 0x01) > 0; } }
    public bool LightScreenActive { get { return (BattleStatus3 & 0x02) > 0; } }
    public bool ReflectActive { get { return (BattleStatus3 & 0x04) > 0; } }
    public bool Transformed { get { return (BattleStatus3 & 0x10) > 0; } }

    public bool Redbar {
        get {
            int n = HP * 48;
            int m = MaxHP;

            if(m > 0xff) {
                m /= 4;
                n = (n & 0xff0000) | ((n & 0x00ffff) / 4);
            }

            return (((n / m) & 0xff) < 10);
        }
    }

    public RbyPokemon() { }
    public RbyPokemon(RbySpecies species, byte level) : this(species, level, 0x9888) { }
    public RbyPokemon(RbySpecies species, byte level, ushort dvs) {
        (Species, Level, DVs) = (species, level, dvs);
        CalculateUnmodifiedStats();
        MaxHP = UnmodifiedMaxHP;
        Attack = UnmodifiedAttack;
        Defense = UnmodifiedDefense;
        Speed = UnmodifiedSpeed;
        Special = UnmodifiedSpecial;
        AttackModifider = 7;
        DefenseModifider = 7;
        SpeedModifider = 7;
        SpecialModifider = 7;
        AccuracyModifider = 7;
        EvasionModifider = 7;
    }

    public void CalculateUnmodifiedStats() {
        UnmodifiedMaxHP = CalculateStat(DVs.HP, Species.BaseHP, HPExp, Level + 10);
        UnmodifiedAttack = CalculateStat(DVs.Attack, Species.BaseAttack, AttackExp, 5);
        UnmodifiedDefense = CalculateStat(DVs.Defense, Species.BaseDefense, DefenseExp, 5);
        UnmodifiedSpeed = CalculateStat(DVs.Speed, Species.BaseSpeed, SpeedExp, 5);
        UnmodifiedSpecial = CalculateStat(DVs.Special, Species.BaseSpecial, SpecialExp, 5);
        if(MaxHP == 0) MaxHP = UnmodifiedMaxHP;
        if(HP == 0) HP = UnmodifiedMaxHP;
        if(Attack == 0) Attack = UnmodifiedAttack;
        if(Defense == 0) Defense = UnmodifiedDefense;
        if(Speed == 0) Speed = UnmodifiedSpeed;
        if(Special == 0) Special = UnmodifiedSpecial;
    }

    public int ExpNeededForLevelUp() {
        if(Level == 100) return -1;
        return Species.GrowthRate.CalcExpNeeded(Level + 1);
    }

    private ushort CalculateStat(byte dv, byte baseStat, ushort exp, int constant) {
        int n = 2 * (dv + baseStat);
        int expBonus = Math.Min((int) Math.Ceiling(Math.Sqrt(exp)), 255) / 4;
        int stat = (n + expBonus) * Level / 100 + constant;
        return (ushort) Math.Min(stat, 65535);
    }

    public override string ToString() {
        return string.Format("L{0} {1} DVs {2:X4}", Level, Species.Name, DVs);
    }

    public static implicit operator RbySpecies(RbyPokemon pokemon) { return pokemon.Species; }
}
