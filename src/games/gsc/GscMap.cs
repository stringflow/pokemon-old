using System;

public enum GscPalette {

    Auto,
    Day,
    Nite,
    Morn,
    Dark,
}

public enum GscFishGroup {

    None,
    Shore,
    Ocean,
    Lake,
    Pond,
    Dratini,
    QwilfishSwarm,
    RemoraidSwarm,
    Gyarados,
    Dratini2,
    WhirlIslands,
    Qwilfish,
    Remoraid,
    QwilfishNoSwarm,
}

public class GscConnection {

    public GscMap Map;
    public byte DestGroup;
    public byte DestNumber;
    public ushort Source;
    public ushort Destination;
    public byte Length;
    public byte Width;
    public byte YAlignment;
    public byte XAlignment;
    public ushort Window;

    public GscConnection(GscMap map, ByteStream data) {
        Map = map;
        DestGroup = data.u8();
        DestNumber = data.u8();
        Source = data.u16le();
        Destination = data.u16le();
        Length = data.u8();
        Width = data.u8();
        YAlignment = data.u8();
        XAlignment = data.u8();
        Window = data.u16le();
    }
}

public class GscTile : Tile<GscTile> {

    public GscMap Map;

    public override GscTile Right() {
        return Map[X + 1, Y];
    }

    public override GscTile Left() {
        return Map[X - 1, Y];
    }

    public override GscTile Up() {
        return Map[X, Y - 1];
    }

    public override GscTile Down() {
        return Map[X, Y + 1];
    }

    public override bool IsPassable(GscTile from, PermissionSet permissions) {
        GscWarp warp = Map.Warps[X, Y];
        if(warp != null && !warp.Allowed) return false;

        GscSprite sprite = Map.Sprites[X, Y];
        if(sprite != null && sprite.MovementFunction != GscSpriteMovement.Wander &&
                             sprite.MovementFunction != GscSpriteMovement.SwimWander &&
                             sprite.MovementFunction != GscSpriteMovement.WalkLeftRight &&
                             sprite.MovementFunction != GscSpriteMovement.WalkUpDown) return false;

        return permissions.IsAllowed(Collision);
    }

    public override bool IsLedgeHop(GscTile ledgeTile, Action action) {
        switch(action) {
            case Action.Right: return Collision == 0xa0;
            case Action.Left: return Collision == 0xa1;
            case Action.Up: return Collision == 0xa2;
            case Action.Down: return Collision == 0xa3;
            default: return false;
        }
    }

    public override GscTile WarpCheck() {
        GscWarp sourceWarp = Map.Warps[X, Y];
        if(sourceWarp != null && sourceWarp.Allowed) {
            GscMap destMap = Map.Game.Maps[sourceWarp.MapId];
            if(destMap != null) {
                GscWarp destWarp = destMap.Warps[sourceWarp.DestinationIndex];
                if(destWarp != null) {
                    GscTile destTile = destMap[destWarp.X, destWarp.Y];
                    if(destTile.Collision == 113) destTile = destTile.Neighbor(Action.Down); // Door tiles automatically move the player 1 tile down.
                    return destTile;
                }
            }
        }

        return this;
    }

    public override int LedgeCost() {
        return 34; // TODO: Fact check this
    }
}

public class GscMap : Map<GscTile> {

    // TODO: Environment, Location, and Music should all be enums.
    //       Because they are currently unused I left them as bytes to reduce noise in the code.
    public Gsc Game;
    public string Name;
    public byte Group;
    public byte Id;
    public int Attributes;
    public GscTileset Tileset;
    public byte Environment;
    public byte Location;
    public byte Music;
    public bool PhoneService;
    public GscPalette TimeOfDay;
    public GscFishGroup FishGroup;
    public byte BorderBlock;
    public int Blocks;
    public int Scripts;
    public int Events;
    public byte ConnectionFlags;
    public int EnvironmentColorPointer;
    public GscConnection[] Connections;
    public DataList<GscWarp> Warps;
    public DataList<GscCoordEvent> CoordEvents;
    public DataList<GscBGEvent> BGEvents;
    public DataList<GscSprite> Sprites;

    public GscMap(Gsc game, int group, int id, ByteStream data) {
        Game = game;
        Group = (byte) group;
        Id = (byte) id;
        byte bank = data.u8();
        Tileset = game.Tilesets[data.u8()];
        Environment = data.u8();
        Attributes = bank << 16 | data.u16le();
        Location = data.u8();
        Music = data.u8();
        PhoneService = data.Nybble() == 0;
        TimeOfDay = (GscPalette) data.Nybble();
        FishGroup = (GscFishGroup) data.u8();

        Name = game.SYM[Attributes];
        Name = Name.Substring(0, Name.IndexOf("_MapAttributes"));

        EnvironmentColorPointer = game.SYM["EnvironmentColorsPointers"] & 0xff0000 | game.ROM.u16le(game.SYM["EnvironmentColorsPointers"] + Environment * 2);

        ByteStream attributesData = game.ROM.From(Attributes);
        BorderBlock = attributesData.u8();
        Height = attributesData.u8();
        Width = attributesData.u8();
        Blocks = attributesData.u8() << 16 | attributesData.u16le();
        Scripts = attributesData.u8() << 16 | attributesData.u16le();
        Events = (Scripts & 0xff0000) | attributesData.u16le();
        ConnectionFlags = attributesData.u8();

        Connections = new GscConnection[4];
        for(int i = 3; i >= 0; i--) {
            if(((ConnectionFlags >> i) & 1) == 1) {
                Connections[i] = new GscConnection(this, attributesData);
            }
        }

        ByteStream eventsData = game.ROM.From(Events + 2);

        Warps = new DataList<GscWarp>();
        Warps.IndexCallback = obj => obj.Index;
        Warps.PositionCallback = obj => (obj.X, obj.Y);
        byte numWarps = eventsData.u8();
        for(byte i = 0; i < numWarps; i++) {
            Warps.Add(new GscWarp(game, this, i, eventsData));
        }

        CoordEvents = new DataList<GscCoordEvent>();
        CoordEvents.PositionCallback = obj => (obj.X, obj.Y);
        byte numCoordEvents = eventsData.u8();
        for(byte i = 0; i < numCoordEvents; i++) {
            CoordEvents.Add(new GscCoordEvent(game, this, eventsData));
        }

        BGEvents = new DataList<GscBGEvent>();
        BGEvents.PositionCallback = obj => (obj.X, obj.Y);
        byte numBGEvents = eventsData.u8();
        for(byte i = 0; i < numBGEvents; i++) {
            BGEvents.Add(new GscBGEvent(game, this, eventsData));
        }

        Sprites = new DataList<GscSprite>();
        Sprites.PositionCallback = obj => (obj.X, obj.Y);
        byte numSprites = eventsData.u8();
        for(byte i = 0; i < numSprites; i++) {
            Sprites.Add(new GscSprite(game, this, i, eventsData));
        }

        byte[] blocks = game.ROM.Subarray(Blocks, Width * Height);
        Tiles = new GscTile[Width * 2, Height * 2];
        for(int i = 0; i < blocks.Length; i++) {
            byte block = blocks[i];
            for(int j = 0; j < 4; j++) {
                byte collision = game.ROM[Tileset.Coll + block * 4 + j];
                int tileSpaceIndex = i * 2 + (j & 1) + (j >> 1) * (Width * 2) + (i / Width * 2 * Width);
                byte xt = (byte) (tileSpaceIndex % (Width * 2));
                byte yt = (byte) (tileSpaceIndex / (Width * 2));
                Tiles[xt, yt] = new GscTile {
                    Map = this,
                    X = xt,
                    Y = yt,
                    Collision = collision,
                };
            }
        }
    }

    public override Bitmap Render() {
        return Render(GscPalette.Auto);
    }

    public Bitmap Render(GscPalette timePalette) {
        byte[] tiles = Tileset.GetTiles(Game.ROM.Subarray(Blocks, Width * Height), Width);
        byte[] decompressed = LZ.Decompress(Game.ROM.From(Tileset.GFX));

        byte[] gfx = new byte[0xe00];
        Array.Copy(decompressed, 0, gfx, 0, 0x600); // vram bank 1
        if(decompressed.Length > 0x600) {
            Array.Copy(decompressed, 0x600, gfx, 0x800, 0x600); // optional vram bank 2
        }

        if(Tileset.Id == 1 || Tileset.Id == 2 || Tileset.Id == 4) {
            // Load map group specific roof tiles.
            byte roofIndex = Game.ROM[Game.SYM["MapGroupRoofs"] + Group];
            if(roofIndex != 0xff) {
                Array.Copy(Game.ROM.Data, Game.SYM["Roofs"] + roofIndex * 0x90, gfx, 0xa0, 0x90);
            }
        }

        int timeOffset = 0;
        switch(timePalette) {
            case GscPalette.Morn: timeOffset = 0; break;
            case GscPalette.Auto: // TODO: Don't always default to day time?
            case GscPalette.Day: timeOffset = 1; break;
            case GscPalette.Nite: timeOffset = 2; break;
            case GscPalette.Dark: timeOffset = 3; break;
        }

        byte palMapBank;
        if(Game is Crystal) {
            palMapBank = 0x13;
        } else {
            palMapBank = 0x02;
        }

        byte[] pixels = new byte[tiles.Length * 16];
        byte[] palMap = new byte[tiles.Length];

        for(int i = 0; i < tiles.Length; i++) {
            byte tile = tiles[i];
            Array.Copy(gfx, tile * 16, pixels, i * 16, 16);

            int bankOffset = tile > 0x60 ? 0x20 : 0;
            byte palType = Game.ROM[(palMapBank << 16 | Tileset.PalMap) + (tile + bankOffset) / 2];
            if((tile & 1) == 0) palType &= 0xf;
            else palType >>= 4;

            palMap[i] = Game.ROM[EnvironmentColorPointer + timeOffset * 8 + palType];
        }

        ushort[] bgPalData = Game.ROM.From("TilesetBGPalette").ReadU16le(168);
        ushort[] roofPalData = Game.ROM.From(Game.SYM["RoofPals"] + Group * 8).ReadU16le(4);
        Array.Copy(roofPalData, 0, bgPalData, 6 * 4 + 1, 2);
        Array.Copy(roofPalData, 0, bgPalData, 14 * 4 + 1, 2);
        Array.Copy(roofPalData, 2, bgPalData, 22 * 4 + 1, 2);

        byte[][][] bgPal = new byte[42][][];
        for(int i = 0; i < bgPal.Length; i++) {
            bgPal[i] = new byte[4][];
            for(int j = 0; j < 4; j++) {
                bgPal[i][j] = new byte[3];

                ushort val = bgPalData[i * 4 + j];
                bgPal[i][j][0] = (byte) (((val) & 0x1f) << 3);
                bgPal[i][j][1] = (byte) (((val >> 5) & 0x1f) << 3);
                bgPal[i][j][2] = (byte) (((val >> 10) & 0x1f) << 3);
            }
        }

        Bitmap bitmap = new Bitmap(Width * 2 * 2 * 8, Height * 2 * 2 * 8);
        bitmap.Unpack2BPP(pixels, bgPal, palMap);
        return bitmap;
    }

    public override string ToString() {
        return Name;
    }
}