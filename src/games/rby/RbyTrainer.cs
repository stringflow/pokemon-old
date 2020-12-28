using System.Collections.Generic;

public class RbyTrainerClass : ROMObject {

    public List<List<RbyPokemon>> Teams;

    public RbyTrainerClass(Rby game, byte id, int length, ByteStream data, ByteStream name) {
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

    public List<RbyTile> VisionTiles {
        get {
            List<RbyTile> tiles = new List<RbyTile>();
            RbyTile current = Map[X, Y];
            for(int i = 0; i < SightRange; i++) {
                RbyTile next = current.Neighbor(Direction);
                if(next == null) break;
                tiles.Add(next);
                current = next;
            }
            return tiles;
        }
    }

    public RbyTrainer(RbySprite baseSprite, ByteStream data) : base(baseSprite, data) {
        TrainerClass = Map.Game.TrainerClasses[data.u8()];
        TeamIndex = (byte) (data.u8() - 1);

        int textPointer = Map.Bank << 16 | Map.TextPointer + (TextId - 1) * 2;
        int scriptPointer = Map.Bank << 16 | Map.Game.ROM.u16le(textPointer);
        int headerPointer = Map.Bank << 16 | Map.Game.ROM.u16le(scriptPointer + 2);
        ByteStream header = Map.Game.ROM.From(headerPointer);
        EventFlagBit = header.u8();
        SightRange = (byte) (header.u8() >> 4);
        EventFlagAddress = header.u16le();
    }

    public bool IsDefeated(GameBoy gb) {
        return (gb.CpuRead(EventFlagAddress) & EventFlagBit) == 0;
    }
}