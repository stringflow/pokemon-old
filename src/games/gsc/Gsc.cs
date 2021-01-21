using System.Collections.Generic;

// Represents parsed ROM data. The data will only be parsed once and shared across multiple instances of the same game if the ROM's checksums match.
public class GscData {

    public Charmap Charmap;
    public DataList<GscSpecies> Species = new DataList<GscSpecies>();
    public DataList<GscMove> Moves = new DataList<GscMove>();
    public DataList<GscItem> Items = new DataList<GscItem>();
    public DataList<GscTileset> Tilesets = new DataList<GscTileset>();
    public DataList<GscMap> Maps = new DataList<GscMap>();

    public GscData() {
        // See https://github.com/pret/pokegold/blob/master/charmap.asm
        Charmap = new Charmap("A B C D E F G H I J K L M N O P " +
                              "Q R S T U V W X Y Z ( ) : ; [ ] " +
                              "a b c d e f g h i j k l m n o p " +
                              "q r s t u v w x y z _ _ _ _ _ _ " +
                              "Ä Ö Ü ä ö ü _ _ _ _ _ _ _ _ _ _ " +
                              "'d 'l 'm 'r 's 't 'v _ _ _ _ _ _ _ _ _ " +
                              "' PM MN - _ _ ? ! . & é _ _ _ _ _M " +
                              "$ * . / , _F 0 1 2 3 4 5 6 7 8 9");
        Charmap.Map[0x54] = "POKE";

        Species.NameCallback = obj => obj.Name;
        Species.IndexCallback = obj => obj.Id;

        Moves.NameCallback = obj => obj.Name;
        Moves.IndexCallback = obj => obj.Id;

        Items.NameCallback = obj => obj.Name;
        Items.IndexCallback = obj => obj.Id;

        Tilesets.IndexCallback = obj => obj.Id;

        Maps.NameCallback = obj => obj.Name;
        Maps.IndexCallback = obj => obj.Group << 8 | obj.Id;
    }
}

public partial class Gsc : GameBoy {

    // Maps ROM checksums to their parsed data.
    private static Dictionary<int, GscData> ParsedROMs = new Dictionary<int, GscData>();

    public GscData Data;

    public Charmap Charmap {
        get { return Data.Charmap; }
    }

    public DataList<GscSpecies> Species {
        get { return Data.Species; }
    }

    public DataList<GscMove> Moves {
        get { return Data.Moves; }
    }

    public DataList<GscItem> Items {
        get { return Data.Items; }
    }

    public DataList<GscTileset> Tilesets {
        get { return Data.Tilesets; }
    }

    public DataList<GscMap> Maps {
        get { return Data.Maps; }
    }

    public Gsc(string rom, SpeedupFlags flags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, flags) {
        // If a ROM with the same checksum has already been parsed, the data will be shared.
        if(ParsedROMs.ContainsKey(ROM.GlobalChecksum)) {
            Data = ParsedROMs[ROM.GlobalChecksum];
        } else {
            // Otherwise the new ROM will be parsed.
            Data = new GscData();
            LoadSpecies();
            LoadMoves();
            LoadItems();
            LoadTilesets();
            LoadMaps();
            ParsedROMs[ROM.GlobalChecksum] = Data;
        }

        OverworldLoopAddress = SYM["OWPlayerInput"];
    }

    private void LoadSpecies() {
        ByteStream dataStream = ROM.From("BaseData");
        ByteStream nameStream = ROM.From("PokemonNames");
        for(int index = 0; index <= 0xff; index++) {
            Species.Add(new GscSpecies(this, dataStream, nameStream));
        }
    }

    private void LoadMoves() {
        ByteStream dataStream = ROM.From("Moves");
        ByteStream nameStream = ROM.From("MoveNames");
        for(int index = 0; index <= 0xfa; index++) {
            Moves.Add(new GscMove(this, dataStream, nameStream));
        }
    }

    private void LoadItems() {
        ByteStream nameStream = ROM.From("ItemNames");
        for(int index = 0; index <= 0xff; index++) {
            Items.Add(new GscItem(this, (byte) index, nameStream));
        }
    }

    private void LoadTilesets() {
        int numTilesets = this is Crystal ? 37 : 29;
        ByteStream dataStream = ROM.From("Tilesets");
        for(int index = 0; index < numTilesets; index++) {
            Tilesets.Add(new GscTileset(this, (byte) index, dataStream));
        }
    }

    private void LoadMaps() {
        const int numMapGroups = 26;

        byte bank = (byte) (SYM["MapGroupPointers"] >> 16);
        int[] mapGroupOffsets = new int[numMapGroups];
        ByteStream mapGroupsStream = ROM.From("MapGroupPointers");
        for(int i = 0; i < numMapGroups; i++) {
            mapGroupOffsets[i] = bank << 16 | mapGroupsStream.u16le();
        }

        for(int mapGroup = 0; mapGroup < numMapGroups; mapGroup++) {
            int currentOffset = mapGroupOffsets[mapGroup];
            int nextGroupOffset = mapGroup == numMapGroups - 1 ? SYM["NewBarkTown_MapAttributes"] : mapGroupOffsets[mapGroup + 1];
            int numMaps = (nextGroupOffset - currentOffset) / 9;
            ByteStream dataStream = ROM.From(mapGroupOffsets[mapGroup]);
            for(int map = 0; map < numMaps; map++) {
                Maps.Add(new GscMap(this, mapGroup + 1, map + 1, dataStream));
            }
        }
    }

    public override Font ReadFont() {
        const int numCols = 16;
        byte[] gfx = ROM.Subarray("Font", 16 * 8 * 8);
        Bitmap bitmap = new Bitmap(numCols * 8, gfx.Length / numCols);
        bitmap.Unpack1BPP(gfx, new byte[][] {
                                    new byte[] { 0x00, 0x00, 0x00, 0x00 },
                                    new byte[] { 0xff, 0xff, 0xff, 0xff }});

        return new Font {
            Bitmap = bitmap,
            CharacterSize = 8,
            NumCharsPerRow = numCols,
            Charmap = Data.Charmap,
            CharmapOffset = 0x80,
        };
    }
}