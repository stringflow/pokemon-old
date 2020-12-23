using System;
using System.Collections.Generic;

// Represents parsed ROM data. The data will only be parsed once and shared across multiple instances of the same game if the ROM's checksums match.
public class RbyData {

    public Charmap Charmap;
    public DataList<RbyMove> Moves = new DataList<RbyMove>();
    public DataList<RbySpecies> Species = new DataList<RbySpecies>();
    public DataList<RbyItem> Items = new DataList<RbyItem>();
    public DataList<RbyTrainerClass> TrainerClasses = new DataList<RbyTrainerClass>();
    public List<RbyLedge> Ledges = new List<RbyLedge>();
    public DataList<RbyTileset> Tilesets = new DataList<RbyTileset>();
    public DataList<RbyMap> Maps = new DataList<RbyMap>();

    public RbyData() {
        // See https://github.com/pret/pokered/blob/master/charmap.asm
        Charmap = new Charmap("A B C D E F G H I J K L M N O P " +
                              "Q R S T U V W X Y Z ( ) : ; [ ] " +
                              "a b c d e f g h i j k l m n o p " +
                              "q r s t u v w x y z é 'd 'l 's 't 'v " +
                              "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ " +
                              "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ " +
                              "' PK MN - 'r 'm ? ! . _ _ _ _ _ _ M " +
                              "$ * . / , F 0 1 2 3 4 5 6 7 8 9 ");
        Charmap.Map[0x4A] = "PkMn";
        Charmap.Map[0x54] = "POKE";
        Charmap.Map[0x52] = "<PLAYER>";
        Charmap.Map[0x53] = "<RIVAL";

        Moves.NameCallback = obj => obj.Name;
        Moves.IndexCallback = obj => obj.Id;

        Species.NameCallback = obj => obj.Name;
        Species.IndexCallback = obj => obj.Id;

        Items.NameCallback = obj => obj.Name;
        Items.IndexCallback = obj => obj.Id;

        TrainerClasses.NameCallback = obj => obj.Name;
        TrainerClasses.IndexCallback = obj => obj.Id;

        Tilesets.IndexCallback = obj => obj.Id;

        Maps.IndexCallback = obj => obj.Id;
    }
}

public class Rby : GameBoy {

    // Maps ROM checksums to their parsed data.
    private static Dictionary<int, RbyData> ParsedROMs = new Dictionary<int, RbyData>();

    public RbyData Data;

    public Charmap Charmap {
        get { return Data.Charmap; }
    }

    public DataList<RbyMove> Moves {
        get { return Data.Moves; }
    }

    public DataList<RbySpecies> Species {
        get { return Data.Species; }
    }

    public DataList<RbyItem> Items {
        get { return Data.Items; }
    }

    public DataList<RbyTrainerClass> TrainerClasses {
        get { return Data.TrainerClasses; }
    }

    public List<RbyLedge> Ledges {
        get { return Data.Ledges; }
    }

    public DataList<RbyTileset> Tilesets {
        get { return Data.Tilesets; }
    }

    public DataList<RbyMap> Maps {
        get { return Data.Maps; }
    }

    public Rby(string rom, SpeedupFlags speedupFlags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, speedupFlags) {
        // If a ROM with the same checksum has already been parsed, the data will be shared.
        if(ParsedROMs.ContainsKey(ROM.GlobalChecksum)) {
            Data = ParsedROMs[ROM.GlobalChecksum];
        } else {
            // Otherwise the new ROM will be parsed.
            Data = new RbyData();
            LoadMoves();
            LoadSpecies();
            LoadItems();
            LoadTrainerClasses();
            LoadLedges();
            LoadTilesets();
            LoadTilePairCollisions();
            LoadMaps();
            ParsedROMs[ROM.GlobalChecksum] = Data;
        }
    }

    private void LoadSpecies() {
        const int maxIndexNumber = 190;

        int numBaseStats = (SYM["BaseStatsEnd"] - SYM["BaseStats"]) / (SYM["MonBaseStatsEnd"] - SYM["MonBaseStats"]);
        byte[] pokedex = ROM.Subarray(SYM["PokedexOrder"], maxIndexNumber);
        ByteStream data = ROM.From("BaseStats");

        for(int i = 0; i < numBaseStats; i++) {
            byte indexNumber = (byte) Array.IndexOf(pokedex, data.Peek());
            Species.Add(new RbySpecies(this, ++indexNumber, data));
        }

        // Add Mew data
        Species.Add(new RbySpecies(this, 21, ROM.From(SYM["MewBaseStats"])));

        // Add MISSINGNO data
        for(int i = 1; i <= maxIndexNumber; i++) {
            if(pokedex[i - 1] == 0) {
                RbySpecies species = new RbySpecies(this, (byte) i);
                Species.Add(new RbySpecies(this, (byte) i));
            }
        }
    }

    private void LoadMoves() {
        int movesStart = SYM["Moves"];
        int numMoves = (SYM["BaseStats"] - movesStart) / (SYM["MoveEnd"] - movesStart);

        ByteStream nameStream = ROM.From("MoveNames");
        ByteStream dataStream = ROM.From(movesStart);

        for(int i = 0; i < numMoves; i++) {
            Moves.Add(new RbyMove(this, dataStream, nameStream));
        }
    }

    private void LoadItems() {
        ByteStream nameStream = ROM.From("ItemNames");

        for(int i = 0; i < 256; i++) {
            string name;
            if(i > 0x0 && i <= 0x61) {
                name = Charmap.Decode(nameStream.Until(Charmap.Terminator));
            } else if(i >= 0xC4 && i <= 0xC8) {
                name = String.Format("HM{0}", (i + 1 - 0xc4).ToString("D2"));
            } else if(i >= 0xC9 && i <= 0xFF) {
                name = String.Format("TM{0}", (i + 1 - 0xC9).ToString("D2"));
            } else {
                name = String.Format("hex{0:X2}", i);
            }

            Items.Add(new RbyItem(this, (byte) i, name));
        }
    }

    private void LoadTrainerClasses() {
        const int numTrainerClasses = 47;

        ByteStream nameStream = ROM.From("TrainerNames");
        ByteStream trainerClassStream = ROM.From("TrainerDataPointers");

        int[] trainerDataOffsets = new int[numTrainerClasses];

        for(int i = 0; i < numTrainerClasses; i++) {
            trainerDataOffsets[i] = 0x0E << 16 | trainerClassStream.u16le();
        }

        for(int trainerClass = 0; trainerClass < numTrainerClasses; trainerClass++) {
            int currentOffset = trainerDataOffsets[trainerClass];
            int nextTrainerOffset = trainerClass == numTrainerClasses - 1 ? SYM["TrainerAI"] : trainerDataOffsets[trainerClass + 1];
            int length = nextTrainerOffset - currentOffset;

            if(length == 0) {
                nameStream.Until(Charmap.Terminator);
                continue;
            }

            ByteStream dataStream = ROM.From(trainerDataOffsets[trainerClass]);
            TrainerClasses.Add(new RbyTrainerClass(this, (byte) (trainerClass + 201), length, dataStream, nameStream));
        }
    }

    private void LoadTilesets() {
        int numTilesets = this is Yellow ? 25 : 24;
        ByteStream dataStream = ROM.From("Tilesets");
        for(byte i = 0; i < numTilesets; i++) {
            Tilesets.Add(new RbyTileset(this, i, dataStream));
        }
    }

    private void LoadTilePairCollisions() {
        byte[] data = ROM.From("TilePairCollisionsLand").Until(0xff);
        for(int i = 0; i < data.Length - 1; i += 3) {
            Tilesets[data[i]].TilePairCollisionsLand.Add(data[i + 1], data[i + 2]);
        }

        data = ROM.From("TilePairCollisionsWater").Until(0xff);
        for(int i = 0; i < data.Length - 1; i += 3) {
            Tilesets[data[i]].TilePairCollisionsWater.Add(data[i + 1], data[i + 2]);
        }
    }

    private void LoadLedges() {
        byte[] data = ROM.From("LedgeTiles").Until(0xff);
        for(int i = 0; i < data.Length - 1; i += 4) {
            Ledges.Add(new RbyLedge() {
                Source = data[i + 1],
                Ledge = data[i + 2],
                ActionRequired = (Action) data[i + 3],
            });
        }
    }

    private void LoadMaps() {
        int numMaps = this is Yellow ? 249 : 248;
        ByteStream bankStream = ROM.From("MapHeaderBanks");
        ByteStream addressStream = ROM.From("MapHeaderPointers");
        for(byte i = 0; i < numMaps; i++) {
            Maps.Add(new RbyMap(this, i, ROM.From(bankStream.u8() << 16 | addressStream.u16le())));
        }
    }

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyInput", (byte) joypad);
    }

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            do {
                RunFor(1);
                RunUntil("Joypad");
            } while ((CpuRead(SYM["wd30"]) & (1 << 5)) != 0);
            Inject(joypad);
            AdvanceFrame();
        }
    }

    public override int Execute(params Action[] actions){
        int ret = 0;

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    Joypad joypad = (Joypad) ((byte) action << 4);
                    do {
                        RunUntil("JoypadOverworld");
                        Inject(joypad);
                        ret = Hold(joypad, SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["TryDoWildEncounter.CanEncounter"] + 6, SYM["OverworldLoopLessDelay.newBattle"] + 3);
                        if(ret == SYM["TryDoWildEncounter.CanEncounter"] + 6) {
                            return RunUntil("CalcStats");
                        } else if(ret == SYM["CollisionCheckOnWater.collision"]) {
                            return ret;
                        }

                        ret = SYM["JoypadOverworld"];
                        RunUntil(SYM["JoypadOverworld"], SYM["EnterMap"] + 0x10);
                    } while((CpuRead("wd736") & 0x40) != 0);
                    break;
                case Action.A:
                    Inject(Joypad.A);
                    AdvanceFrame(Joypad.A);
                    ret = Hold(Joypad.A, "JoypadOverworld", "PrintLetterDelay");
                    if(ret == SYM["PrintLetterDelay"]) {
                        return ret;
                    }
                    break;
                case Action.StartB:
                    Press(Joypad.Start, Joypad.B);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.PokedexFlash:
                    Press(Joypad.Start, Joypad.A, Joypad.B, Joypad.Start);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.Delay:
                    Inject(Joypad.None);
                    RunUntil("OverworldLoop");
                    ret = RunUntil("JoypadOverworld");
                    break;
                default:
                    Debug.Assert(false, "Unknown Action: {0}", action);
                    break;
            }
        }

        return ret;
    }

    public override Font ReadFont() {
        const int numCols = 16;
        byte[] gfx = ROM.Subarray("FontGraphics", SYM["FontGraphicsEnd"] - SYM["FontGraphics"]);
        Bitmap bitmap = new Bitmap(numCols * 8, gfx.Length / numCols);
        for(int i = 0; i < gfx.Length; i++) {
            int xTile = (i / 8 * 8) % bitmap.Width;
            int yTile = i / bitmap.Width * 8;
            for(int j = 0; j < 8; j++) {
                byte col = (byte) ((gfx[i] >> (7 - j) & 1) * 0xff);
                bitmap.SetPixel(xTile + j, yTile + i & 7, col, col, col, col);
            }
        }

        return new Font {
            Bitmap = bitmap,
            CharacterSize = 8,
            NumCharsPerRow = numCols,
            Charmap = Data.Charmap,
            CharmapOffset = 0x80,
        };
    }
}
