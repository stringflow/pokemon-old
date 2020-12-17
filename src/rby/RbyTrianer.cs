using System.Collections.Generic;

public class RbyTrainerClass : NamedObject {
    public byte Id;
    public List<List<RbyPokemon>> Teams;

    public RbyTrainerClass(Rby game, byte id, int length, ByteStream data, ByteStream name) : base(name.Until(Charmap.Terminator), game.Charmap) {
        Id = id;
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
