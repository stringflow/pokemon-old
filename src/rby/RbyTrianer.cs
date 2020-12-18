using System.Collections.Generic;

public class RbyPokemon {

    public RbySpecies Species;
    public ushort HP;
    public byte Status;
    public byte Status2;
    public RbyMove[] Moves;
    // Convert to common gen 1/2 DV enum
    public ushort DVs;
    public byte[] PP;
    public byte Level;
    public ushort MaxHP;
    public ushort Attack;
    public ushort Defense;
    public ushort Speed;
    public ushort Special;

    public bool Asleep {
        get { return SleepCounter != 0; }
    }

    public byte SleepCounter {
        get { return (byte) (Status & 0b111); }
    }

    public bool Poisoned {
        get { return (Status & (1 << 3)) != 0; }
    }

    public bool Burned {
        get { return (Status & (1 << 4)) != 0; }
    }

    public bool Frozen {
        get { return (Status & (1 << 5)) != 0; }
    }

    public bool Paralyzed {
        get { return (Status & (1 << 6)) != 0; }
    }

    public bool XAccSetup {
        get { return (Status2 & (1)) != 0; }
    }

    public override string ToString() {
        return string.Format("L{0} {1} DVs {2:X4}", Level, Species.Name, DVs);
    }

    public RbyPokemon(RbySpecies species, byte level) : this(species, level, 0x9888) { }

    public RbyPokemon(RbySpecies species, byte level, ushort dvs) => (Species, Level, DVs) = (species, level, dvs);

    public static implicit operator RbySpecies(RbyPokemon pokemon) { return pokemon.Species; }
}

public class RbyTrainerClass : ROMObject {

    public List<List<RbyPokemon>> Teams;
    public RbyTrainerClass(Rby game, byte id, int length, ByteStream data, ByteStream name) {
        Id = id;
        Name = game.Charmap.Decode(name.Until(Charmap.Terminator));
        List<List<RbyPokemon>> Teams = new List<List<RbyPokemon>>();

        long initial = data.Position;
        while(data.Position - initial < length) {
            List<RbyPokemon> team = new List<RbyPokemon>();
            byte format = data.u8();
            byte level = format;
            byte speciesIndex;

            while((speciesIndex = data.u8()) != 0x00) {
                if (format == 0xFF) {
                    level = speciesIndex;
                    speciesIndex = data.u8();
                }
                team.Add(new RbyPokemon(game.Species[speciesIndex], level));
            }
            Teams.Add(team);
        }
    }
}
