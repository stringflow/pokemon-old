using System;
using System.Collections.Generic;

public class RbyData {

    public Charmap Charmap;
    public DataList<RbySpecies> Species = new DataList<RbySpecies>();
    public DataList<RbyMove> Moves = new DataList<RbyMove>();
    public DataList<RbyItem> Items = new DataList<RbyItem>();
    public DataList<RbyTileset> Tilesets = new DataList<RbyTileset>();

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

        Items.NameCallback = obj => obj.Name;
        Items.IndexCallback = obj => obj.Id;

        Tilesets.IndexCallback = obj => obj.Id;
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

    public DataList<RbyItem> Items {
        get { return Data.Items; }
    }

    public DataList<RbyTileset> Tilesets {
        get { return Data.Tilesets; }
    }

    public Rby(string rom, SpeedupFlags speedupFlags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, speedupFlags) {
        if(ParsedROMs.ContainsKey(ROM.GlobalChecksum)) {
            Data = ParsedROMs[ROM.GlobalChecksum];
        } else {
            Data = new RbyData();
            LoadSpecies();
            LoadMoves();
            LoadItems();
            LoadTilesets();
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

    private void LoadItems() {
        const int numItems = 97;

        ByteStream nameStream = ROM.From("ItemNames");
        string name;

        for (byte i = 0; i < numItems; i++) {
            name = Charmap.Decode(nameStream.Until(Charmap.Terminator));
            Items.Add(new RbyItem(this, i, name));
        }

        for (int i = 0; i < 256; i++) {
            if (i >= 0xC4 && i <= 0xC8) {
                name = String.Format("HM{0}", (i + 1 - 0xc4).ToString("D2"));
            } else if (i >= 0xC9 && i <= 0xFF) {
                name = String.Format("TM{0}", (i + 1 - 0xC9).ToString("D2"));
            } else if (Items[i] == null) {
                name = String.Format("hex{0:X2}", i);
            } else {
                continue;
            }

            Items.Add(new RbyItem(this, (byte) i, name));
        }
    }

    private void LoadTilesets() {
        Dictionary<byte, List<(byte, byte)>> tilePairCollisionsLand = new Dictionary<byte, List<(byte, byte)>>();
        ByteStream collisionData = ROM.From("TilePairCollisionsLand");

        byte tileset;
        while((tileset = collisionData.u8()) != 0xFF) {
            if (!tilePairCollisionsLand.ContainsKey(tileset)) {
                tilePairCollisionsLand[tileset] = new List<(byte, byte)>();
            }
            tilePairCollisionsLand[tileset].Add((collisionData.u8(), collisionData.u8()));
        }

        ByteStream dataStream = ROM.From("Tilesets");
        int numTilesets = GetType() == typeof(Yellow) ? 25 : 24;
        for (byte index = 0; index < numTilesets; index++) {
            List<(byte, byte)> collisions = tilePairCollisionsLand.GetValueOrDefault(index, new List<(byte, byte)>());
            Tilesets.Add(new RbyTileset(this, index, collisions, dataStream));
        }
    }
}
