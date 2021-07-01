using System.Collections.Generic;

public class RbyTrainerClass : ROMObject {

    public List<List<RbyPokemon>> Teams;

    public RbyTrainerClass(Rby game, byte id, int length, ReadStream data, ReadStream name) {
        Id = id;
        Name = game.Charmap.Decode(name.Until(Charmap.Terminator));
        Teams = new List<List<RbyPokemon>>();

        long initial = data.Position;
        while(data.Position - initial < length) {
            List<RbyPokemon> team = new List<RbyPokemon>();
            byte format = data.u8();
            byte level = format;
            byte speciesIndex;

            while((speciesIndex = data.u8()) != 0x00) {
                if(format == 0xff) {
                    level = speciesIndex;
                    speciesIndex = data.u8();
                }
                team.Add(new RbyPokemon(game.Species[speciesIndex], level));
            }
            Teams.Add(team);
        }
    }
}

public class RbyTrainer : RbySprite {

    public RbyTrainerClass TrainerClass;
    public byte TeamIndex;
    public byte EventFlagBit;
    public byte SightRange;
    public ushort EventFlagAddress;

    public List<RbyPokemon> Team {
        get { return TrainerClass.Teams[TeamIndex]; }
    }

    public RbyTrainer(RbySprite baseSprite, ReadStream data) : base(baseSprite, data) {
        TrainerClass = Map.Game.TrainerClasses[data.u8()];
        TeamIndex = (byte) (data.u8() - 1);

        int textPointer = Map.Bank << 16 | Map.TextPointer + (TextId - 1) * 2;
        int scriptPointer = Map.Bank << 16 | Map.Game.ROM.u16le(textPointer);
        int headerPointer = Map.Bank << 16 | Map.Game.ROM.u16le(scriptPointer + 2);

        if(baseSprite.Map.Id == 166 || TrainerClass == null || TrainerClass.Name.Contains("RIVAL")) {
            return;
        }

        ReadStream header = Map.Game.ROM.From(headerPointer);
        EventFlagBit = header.u8();
        SightRange = (byte) (header.u8() >> 4);
        EventFlagAddress = header.u16le();

        if(EventFlagBit >= 8) {
            EventFlagBit -= 8;
            EventFlagAddress++;
        }
    }

    public bool IsDefeated(GameBoy gb) {
        return (gb.CpuRead(EventFlagAddress) & EventFlagBit) == 0;
    }
}