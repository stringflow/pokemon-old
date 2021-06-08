using System;
using System.Collections.Generic;

// Represents parsed ROM data. The data will only be parsed once and shared across multiple instances of the same game if the ROM's checksums match.
public class RbyData {

    public Charmap Charmap;
    public Dictionary<RbyType, Dictionary<RbyType, byte>> TypeEffectivenessTable = new Dictionary<RbyType, Dictionary<RbyType, byte>>();
    public DataList<RbyMove> Moves = new DataList<RbyMove>();
    public DataList<RbySpecies> Species = new DataList<RbySpecies>();
    public DataList<RbyItem> Items = new DataList<RbyItem>();
    public DataList<RbyTrainerClass> TrainerClasses = new DataList<RbyTrainerClass>();
    public List<RbyLedge> Ledges = new List<RbyLedge>();
    public DataList<RbyTileset> Tilesets = new DataList<RbyTileset>();
    public DataList<RbyMap> Maps = new DataList<RbyMap>();
    public Dictionary<Action, byte[]> DirectionalWarpTiles = new Dictionary<Action, byte[]>();

    public RbyData() {
        // See https://github.com/pret/pokered/blob/master/charmap.asm
        Charmap = new Charmap("A B C D E F G H I J K L M N O P " +
                              "Q R S T U V W X Y Z ( ) : ; [ ] " +
                              "a b c d e f g h i j k l m n o p " +
                              "q r s t u v w x y z E 'd 'l 's 't 'v " +
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

        Maps.NameCallback = obj => obj.Name;
        Maps.IndexCallback = obj => obj.Id;
    }
}

public partial class Rby : GameBoy {

    // Maps ROM checksums to their parsed data.
    private static Dictionary<int, RbyData> ParsedROMs = new Dictionary<int, RbyData>();

    public RbyData Data;
    public bool IsYellow;

    public Charmap Charmap {
        get { return Data.Charmap; }
    }

    public DataList<RbyMove> Moves {
        get { return Data.Moves; }
    }

    public Dictionary<RbyType, Dictionary<RbyType, byte>> TypeEffectivenessTable {
        get { return Data.TypeEffectivenessTable; }
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

    public Dictionary<Action, byte[]> DirectionalWarpTiles {
        get { return Data.DirectionalWarpTiles; }
    }

    public Rby(string rom, SpeedupFlags speedupFlags = SpeedupFlags.None) : base("roms/gbc_bios.bin", rom, speedupFlags) {
        IsYellow = ROM.Title == "POKEMON YELLOW";

        // If a ROM with the same checksum has already been parsed, the data will be shared.
        if(ParsedROMs.ContainsKey(ROM.GlobalChecksum)) {
            Data = ParsedROMs[ROM.GlobalChecksum];
        } else {
            // Otherwise the new ROM will be parsed.
            Data = new RbyData();
            LoadTypeEffectivenessTable();
            LoadMoves();
            LoadSpecies();
            LoadItems();
            LoadTrainerClasses();
            LoadLedges();
            LoadTilesets();
            LoadTilePairCollisions();
            LoadMaps();
            LoadMissableSprites();
            LoadDirectionalWarpTiles();
            ParsedROMs[ROM.GlobalChecksum] = Data;
        }

        OverworldLoopAddress = SYM["JoypadOverworld"];
    }

    private void LoadTypeEffectivenessTable() {
        byte[] data = ROM.From("TypeEffects").Until(0xff, false);
        for(int i = 0; i < data.Length; i += 3) {
            RbyType type1 = (RbyType) data[i + 0];
            RbyType type2 = (RbyType) data[i + 1];
            byte effectiveness = data[i + 2];

            if(!TypeEffectivenessTable.ContainsKey(type1)) TypeEffectivenessTable[type1] = new Dictionary<RbyType, byte>();
            TypeEffectivenessTable[type1][type2] = effectiveness;
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

    private void LoadSpecies() {
        const int maxIndexNumber = 190;

        int numBaseStats = IsYellow ? 151 : 150;
        byte[] pokedex = ROM.Subarray(SYM["PokedexOrder"], maxIndexNumber);
        ByteStream data = ROM.From("BaseStats");

        for(int i = 0; i < numBaseStats; i++) {
            byte indexNumber = (byte) Array.IndexOf(pokedex, data.Peek());
            Species.Add(new RbySpecies(this, ++indexNumber, data));
        }

        if(this is RedBlue) {
            Species.Add(new RbySpecies(this, 21, ROM.From(SYM["MewBaseStats"])));
        }

        // Add MISSINGNO data
        for(int i = 1; i <= maxIndexNumber; i++) {
            if(pokedex[i - 1] == 0) {
                RbySpecies species = new RbySpecies(this, (byte) i);
                Species.Add(new RbySpecies(this, (byte) i));
            }
        }
    }

    private void LoadItems() {
        ByteStream nameStream = ROM.From("ItemNames");

        for(int i = 0; i < 256; i++) {
            string name;
            if(i > 0x0 && i <= 0x61) {
                name = Charmap.Decode(nameStream.Until(Charmap.Terminator));
            } else if(i >= 0xc4 && i <= 0xc8) {
                name = String.Format("HM{0}", (i + 1 - 0xc4).ToString("D2"));
            } else if(i >= 0xc9 && i <= 0xff) {
                name = String.Format("TM{0}", (i + 1 - 0xc9).ToString("D2"));
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
            trainerDataOffsets[i] = 0x0e << 16 | trainerClassStream.u16le();
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
        int numTilesets = IsYellow ? 25 : 24;
        ByteStream dataStream = ROM.From("Tilesets");
        for(byte i = 0; i < numTilesets; i++) {
            Tilesets.Add(new RbyTileset(this, i, dataStream));
        }
    }

    private void LoadTilePairCollisions() {
        byte[] data = ROM.From("TilePairCollisionsLand").Until(0xff, false);
        for(int i = 0; i < data.Length; i += 3) {
            Tilesets[data[i]].TilePairCollisionsLand.Add(data[i + 1] << 8 | data[i + 2]);
            Tilesets[data[i]].TilePairCollisionsLand.Add(data[i + 2] << 8 | data[i + 1]);
        }

        data = ROM.From("TilePairCollisionsWater").Until(0xff, false);
        for(int i = 0; i < data.Length; i += 3) {
            Tilesets[data[i]].TilePairCollisionsWater.Add(data[i + 1] << 8 | data[i + 2]);
            Tilesets[data[i]].TilePairCollisionsWater.Add(data[i + 2] << 8 | data[i + 1]);
        }
    }

    private void LoadLedges() {
        byte[] data = ROM.From("LedgeTiles").Until(0xff, false);
        for(int i = 0; i < data.Length; i += 4) {
            Ledges.Add(new RbyLedge() {
                Source = data[i + 1],
                Ledge = data[i + 2],
                ActionRequired = (Action) data[i + 3],
            });
        }
    }

    private void LoadMaps() {
        int numMaps = IsYellow ? 249 : 248;
        ByteStream bankStream = ROM.From("MapHeaderBanks");
        ByteStream addressStream = ROM.From("MapHeaderPointers");
        for(byte i = 0; i < numMaps; i++) {
            int headerAddress = bankStream.u8() << 16 | addressStream.u16le();
            if(SYM.Contains(headerAddress)) {
                string addressLabel = SYM[headerAddress];
                if(addressLabel.EndsWith("_h")) Maps.Add(new RbyMap(this, addressLabel.Substring(0, addressLabel.IndexOf("_h")), i, ROM.From(headerAddress)));
            }
        }
    }

    private void LoadMissableSprites() {
        ByteStream data = ROM.From("MissableObjects");
        for(int id = 0; ; id++) {
            byte mapId = data.u8();
            byte spriteId = data.u8();
            byte state = data.u8();

            if(mapId == 0xff) break;

            RbyMap map = Maps[mapId];
            if(map == null) continue;

            RbySprite sprite = map.Sprites[spriteId - 1];
            if(sprite == null) continue;

            sprite.CanBeMissable = true;
            sprite.MissableAddress = SYM["wMissableObjectFlags"] + id / 8;
            sprite.MissableBit = id % 8;
        }
    }

    private void LoadDirectionalWarpTiles() {
        DirectionalWarpTiles[Action.Down] = ROM.From("WarpTileListPointers.FacingDownWarpTiles").Until(0xff, false);
        DirectionalWarpTiles[Action.Up] = ROM.From("WarpTileListPointers.FacingUpWarpTiles").Until(0xff, false);
        DirectionalWarpTiles[Action.Left] = ROM.From("WarpTileListPointers.FacingLeftWarpTiles").Until(0xff, false);
        DirectionalWarpTiles[Action.Right] = ROM.From("WarpTileListPointers.FacingRightWarpTiles").Until(0xff, false);
    }

    public override Font ReadFont() {
        const int numCols = 16;
        byte[] gfx = ROM.Subarray("FontGraphics", SYM["FontGraphicsEnd"] - SYM["FontGraphics"]);
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

    public (Joypad Input, int Amount) CalcScroll(int target, int current, int max, bool wrapping) {
        if((CpuRead(0xfff6 + (IsYellow ? 4 : 0)) & 0x02) > 0 && CpuReadBE<ushort>("wEnemyMonHP") > 0 && CpuRead("wIsInBattle") > 0) {
            // The move selection is its own thing for some reason, so the input values are wrong have to be adjusted.
            current--;
            max = CpuRead("wNumMovesMinusOne");
            wrapping = true;
        }

        // The input is either Up or Down depending on whether the 'target' slot is above or below the 'current' slot.
        Joypad scrollInput = target < current ? Joypad.Up : Joypad.Down;
        // The number of inputs needed is the distance between the 'current' slot and the 'target' slot.
        int amount = Math.Abs(current - target);

        // If the menu wraps around, the number of inputs should never exceed half of the menus size.
        if(wrapping && amount > max / 2) {
            // If it does exceed, going the other way is fewer inputs.
            amount = max - amount + 1;
            scrollInput ^= (Joypad) 0xc0; // Switch to the other button. This is achieved by XORing the value by 0xc0.
                                          // (Joypad.Down) 01000000 xor 11000000 = 10000000 (Joypad.Up)
                                          // (Joypad.Up)   10000000 xor 11000000 = 01000000 (Joypad.Down)
        }

        if(amount == 0) {
            scrollInput = Joypad.None;
        }

        return (scrollInput, amount);
    }

    public virtual byte[][] BGPalette() {
        return new byte[][] {
                    new byte[] { 232, 232, 232 },
                    new byte[] { 160, 160, 160 },
                    new byte[] { 88, 88, 88 },
                    new byte[] { 16, 16, 16 }};
    }

    public virtual byte[][] ObjPalette() {
        return new byte[][] {
                    new byte[] { 232, 232, 232 },
                    new byte[] { 160, 160, 160 },
                    new byte[] { 88, 88, 88 },
                    new byte[] { 16, 16, 16 }};
    }

    public int GetTypeEffectiveness(RbyType type1, RbyType type2) {
        if(!TypeEffectivenessTable[type1].ContainsKey(type2)) return 10;
        return TypeEffectivenessTable[type1][type2];
    }
}
