using System;
using System.Collections.Generic;

public class RbyData {

    public Charmap Charmap;
    public DataList<RbySpecies> Species = new DataList<RbySpecies>();
    public DataList<RbyMove> Moves = new DataList<RbyMove>();

    public RbyData() {
        Charmap = new Charmap("A B C D E F G H I J K L M N O P " +
                              "Q R S T U V W X Y Z ( ) : ; [ ] " +
                              "a b c d e f g h i j k l m n o p " +
                              "q r s t u v w x y z E 'd 'l 's 't 'v " +
                              "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ " +
                              "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ " +
                              "' _ _ - _ _ ? ! . _ _ _ _ _ _ M " +
                              "_ * . / , F 0 1 2 3 4 5 6 7 8 9 ");
        Charmap.Map[0x4A] = "PkMn";
        Charmap.Map[0x54] = "POKE";
        Charmap.Map[0x52] = "<PLAYER>";
        Charmap.Map[0x53] = "<RIVAL";

        Species.NameCallback = obj => obj.Name;
        Species.IndexCallback = obj => obj.IndexNumber;

        Moves.NameCallback = obj => obj.Name;
        Moves.IndexCallback = obj => obj.IndexNumber;
    }
}

public class Rby : GameBoy {

    private static Dictionary<int, RbyData> ParsedROMs = new Dictionary<int, RbyData>();
    public RbyData Data;
    public Charmap Charmap {
        get { return Data.Charmap; }
    }

    public DataList<RbySpecies> Species {
        get { return Data.Species; }
    }

    public DataList<RbyMove> Moves {
        get { return Data.Moves; }
    }

    public Rby(string rom, SpeedupFlags speedupFlags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, speedupFlags) {
        if(ParsedROMs.ContainsKey(ROM.GlobalChecksum)) {
            Data = ParsedROMs[ROM.GlobalChecksum];
        } else {
            Data = new RbyData();
            LoadSpecies();
            LoadMoves();
        }
    }

    private void LoadSpecies() {
        int maxIndexNumber = 190;

        int namesStart = SYM["MonsterNames"];
        int baseStatsStart = SYM["MonBaseStats"];

        int baseStatsSize = SYM["MonBaseStatsEnd"] - baseStatsStart;
        int numBaseStats = (SYM["CryData"] - baseStatsStart) / baseStatsSize;
        byte[] pokedex = ROM.Subarray(SYM["PokedexOrder"], 190);

        for(int index = 0; index < numBaseStats; index++) {
            int dataOffset = baseStatsStart + index * baseStatsSize;
            byte indexNumber = (byte) Array.IndexOf(pokedex, ROM[dataOffset]);
            int nameOffset = namesStart + indexNumber * 10;

            ByteStream dataStream = ROM.From(dataOffset);
            ByteStream nameStream = ROM.From(nameOffset);

            Species.Add(new RbySpecies(this, ++indexNumber, dataStream, nameStream));
        }

        // Add Mew data
        Species.Add(new RbySpecies(this, 21, ROM.From(SYM["MewBaseStats"]), ROM.From(namesStart + 20 * 10)));

        // Add MISSINGNO data
        for (int i = 1; i <= maxIndexNumber; i++) {
            if(pokedex[i - 1] == 0) {
                Species.Add(new RbySpecies(this, (byte) i, ROM.From(namesStart + (i-1) * 10)));
            }
        }
    }

    private void LoadMoves() {
        int movesStart = SYM["Moves"];
        int numMoves = (SYM["BaseStats"] - movesStart) / (SYM["MoveEnd"] - movesStart);

        ByteStream nameStream = ROM.From("MoveNames");
        ByteStream dataStream = ROM.From(movesStart);

        for (int i = 0; i < numMoves; i++) {
            Moves.Add(new RbyMove(this, dataStream, nameStream));
        }
    }
}
