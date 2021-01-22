using System;

public class GscPokemon {

    public GscSpecies Species;

    public byte Status;
    public byte BattleStatus1;
    public byte BattleStatus2;
    public byte BattleStatus3;
    public byte BattleStatus4;
    public byte BattleStatus5;
    public byte Screens;

    public GscMove[] Moves;
    public byte[] PP;

    public DVs DVs;
    public byte Level;
    public int Experience;
    public byte Happiness;
    public bool Pokerus;

    public GscItem HeldItem;

    public ushort MaxHP;
    public ushort HP;
    public ushort Attack;
    public ushort Defense;
    public ushort SpecialAttack;
    public ushort SpecialDefense;
    public ushort Speed;
    public ushort HPExp;
    public ushort AttackExp;
    public ushort DefenseExp;
    public ushort SpecialExp;
    public ushort SpeedExp;
    public ushort UnmodifiedMaxHP;
    public ushort UnmodifiedAttack;
    public ushort UnmodifiedDefense;
    public ushort UnmodifiedSpecialAttack;
    public ushort UnmodifiedSpecialDefense;
    public ushort UnmodifiedSpeed;
    public byte AttackModifider;
    public byte DefenseModifider;
    public byte SpecialAttackModifider;
    public byte SpecialDefenseModifider;
    public byte SpeedModifider;
    public byte AccuracyModifider;
    public byte EvasionModifider;

    public int SleepCounter { get { return Status & 7; } }
    public bool Asleep { get { return SleepCounter > 0; } }
    public bool Poisoned { get { return (Status & 0x08) > 0; } }
    public bool Burned { get { return (Status & 0x10) > 0; } }
    public bool Frozen { get { return (Status & 0x20) > 0; } }
    public bool Paralyzed { get { return (Status & 0x40) > 0; } }

    public bool HasNightmare { get { return (BattleStatus1 & 0x01) > 0; } }
    public bool Curesed { get { return (BattleStatus1 & 0x02) > 0; } }
    public bool Protected { get { return (BattleStatus1 & 0x04) > 0; } }
    public bool Indentified { get { return (BattleStatus1 & 0x08) > 0; } }
    public bool PerishSongActive { get { return (BattleStatus1 & 0x10) > 0; } }
    public bool Enduring { get { return (BattleStatus1 & 0x20) > 0; } }
    public bool RollingOut { get { return (BattleStatus1 & 0x40) > 0; } }
    public bool InLove { get { return (BattleStatus1 & 0x80) > 0; } }

    public bool Curled { get { return (BattleStatus2 & 0x01) > 0; } }

    public bool StoringEnergy { get { return (BattleStatus3 & 0x01) > 0; } }
    public bool ThrashingAbout { get { return (BattleStatus3 & 0x02) > 0; } }
    public bool InLoop { get { return (BattleStatus3 & 0x04) > 0; } }
    public bool Flinched { get { return (BattleStatus3 & 0x08) > 0; } }
    public bool Charged { get { return (BattleStatus3 & 0x10) > 0; } }
    public bool Underground { get { return (BattleStatus3 & 0x20) > 0; } }
    public bool Flying { get { return (BattleStatus3 & 0x40) > 0; } }
    public bool Confused { get { return (BattleStatus3 & 0x80) > 0; } }

    public bool XAccuracyEffect { get { return (BattleStatus4 & 0x01) > 0; } }
    public bool ProtectedByMist { get { return (BattleStatus4 & 0x02) > 0; } }
    public bool FocusEnergyEffect { get { return (BattleStatus4 & 0x04) > 0; } }
    public bool SubstituteActive { get { return (BattleStatus4 & 0x10) > 0; } }
    public bool Recharging { get { return (BattleStatus4 & 0x20) > 0; } }
    public bool Raging { get { return (BattleStatus4 & 0x40) > 0; } }
    public bool Seeded { get { return (BattleStatus4 & 0x80) > 0; } }

    public bool BadlyPoisoned { get { return (BattleStatus5 & 0x01) > 0; } }
    public bool Transformed { get { return (BattleStatus5 & 0x08) > 0; } }
    public bool Encored { get { return (BattleStatus5 & 0x10) > 0; } }
    public bool LockedOn { get { return (BattleStatus5 & 0x20) > 0; } }
    public bool DestinyBonded { get { return (BattleStatus5 & 0x40) > 0; } }
    public bool CantRun { get { return (BattleStatus5 & 0x80) > 0; } }

    public bool Spiked { get { return (Screens & 0x01) > 0; } }
    public bool Safeguarded { get { return (Screens & 0x04) > 0; } }
    public bool LightScreenActive { get { return (Screens & 0x08) > 0; } }
    public bool ReflectActive { get { return (Screens & 0x10) > 0; } }

    public Gender Gender {
        get {
            switch(Species.GenderRatio) {
                case 0xff: return Gender.Genderless;
                case 0xfe: return Gender.Female;
                case 0x00: return Gender.Male;
                default: return Species.GenderRatio < (DVs.Attack << 4 | DVs.Speed) ? Gender.Male : Gender.Female;
            }
        }
    }

    public GscType HPType {
        get {
            GscType type = (GscType) ((DVs.Defense & 3) + ((DVs.Attack & 3) << 2));
            if(type == GscType.Normal) type++;
            if(type == GscType.Bird) type++;
            if(type > GscType.Steel) type += 0xc;
            return type;
        }
    }

    public byte HPPower {
        get { return (byte) ((((DVs.Attack & 8) | ((DVs.Defense & 8) >> 1) | ((DVs.Speed & 8) >> 2) | ((DVs.Special & 8) >> 3)) * 5 + (DVs.Special & 3)) / 2 + 31); }
    }

    public GscPokemon() { }
    public GscPokemon(GscSpecies species, byte level) : this(species, level, 0x9888) { }
    public GscPokemon(GscSpecies species, byte level, ushort dvs) {
        (Species, Level, DVs) = (species, level, dvs);
        CalculateUnmodifiedStats();
        MaxHP = UnmodifiedMaxHP;
        Attack = UnmodifiedAttack;
        Defense = UnmodifiedDefense;
        Speed = UnmodifiedSpeed;
        SpecialAttack = UnmodifiedSpecialAttack;
        SpecialDefense = UnmodifiedSpecialDefense;
        AttackModifider = 7;
        DefenseModifider = 7;
        SpeedModifider = 7;
        SpecialAttackModifider = 7;
        SpecialDefenseModifider = 7;
        AccuracyModifider = 7;
        EvasionModifider = 7;
    }

    public void CalculateUnmodifiedStats() {
        UnmodifiedMaxHP = CalculateStat(DVs.HP, Species.BaseHP, HPExp, Level + 10);
        UnmodifiedAttack = CalculateStat(DVs.Attack, Species.BaseAttack, AttackExp, 5);
        UnmodifiedDefense = CalculateStat(DVs.Defense, Species.BaseDefense, DefenseExp, 5);
        UnmodifiedSpecialAttack = CalculateStat(DVs.Special, Species.BaseSpecialAttack, SpecialExp, 5);
        UnmodifiedSpecialAttack = CalculateStat(DVs.Special, Species.BaseSpecialDefense, SpecialExp, 5);
        UnmodifiedSpeed = CalculateStat(DVs.Speed, Species.BaseSpeed, SpeedExp, 5);
        if(MaxHP == 0) MaxHP = UnmodifiedMaxHP;
        if(HP == 0) HP = UnmodifiedMaxHP;
        if(Attack == 0) Attack = UnmodifiedAttack;
        if(Defense == 0) Defense = UnmodifiedDefense;
        if(SpecialAttack == 0) SpecialAttack = UnmodifiedSpecialAttack;
        if(SpecialDefense == 0) SpecialAttack = UnmodifiedSpecialDefense;
        if(Speed == 0) Speed = UnmodifiedSpeed;
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

    public static implicit operator GscSpecies(GscPokemon pokemon) { return pokemon.Species; }
}