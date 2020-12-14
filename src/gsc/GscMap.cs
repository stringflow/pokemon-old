using System;
using System.IO;

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

    public override bool IsPassable(PermissionSet permissions) {
        return Map.Sprites[X, Y] == null && permissions.IsAllowed(Collision);
    }
}

public class GscMap : Map<GscTile> {

    // TODO: Environment, Location, and Music should all be enums.
    //       Because they are currently unused I left them as bytes to reduce noise in the code.
    public Gsc Game;
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
        Warps.PositionCallback = obj => (obj.X, obj.Y);
        byte numWarps = eventsData.u8();
        for(byte index = 0; index < numWarps; index++) {
            Warps.Add(new GscWarp(game, this, index, eventsData));
        }

        CoordEvents = new DataList<GscCoordEvent>();
        CoordEvents.PositionCallback = obj => (obj.X, obj.Y);
        byte numCoordEvents = eventsData.u8();
        for(byte index = 0; index < numCoordEvents; index++) {
            CoordEvents.Add(new GscCoordEvent(game, this, eventsData));
        }

        BGEvents = new DataList<GscBGEvent>();
        BGEvents.PositionCallback = obj => (obj.X, obj.Y);
        byte numBGEvents = eventsData.u8();
        for(byte index = 0; index < numBGEvents; index++) {
            BGEvents.Add(new GscBGEvent(game, this, eventsData));
        }

        Sprites = new DataList<GscSprite>();
        Sprites.PositionCallback = obj => (obj.X, obj.Y);
        byte numSprites = eventsData.u8();
        for(byte index = 0; index < numSprites; index++) {
            Sprites.Add(new GscSprite(game, this, index, eventsData));
        }

        byte[] blocks = game.ROM.Subarray(Blocks, Width * Height);
        Tiles = new GscTile[blocks.Length * 2 * 2];
        for(int i = 0; i < blocks.Length; i++) {
            byte block = blocks[i];
            for(int j = 0; j < 4; j++) {
                byte collision = game.ROM[Tileset.Coll + block * 4 + j];
                int tileSpaceIndex = i * 2 + (j & 1) + (j >> 1) * (Width * 2) + (i / Width * 2 * Width);
                byte xt = (byte) (tileSpaceIndex % (Width * 2));
                byte yt = (byte) (tileSpaceIndex / (Width * 2));
                Tiles[tileSpaceIndex] = new GscTile {
                    Map = this,
                    X = xt,
                    Y = yt,
                    Collision = collision,
                };
            }
        }
    }

    public override Bitmap Render() {
        byte[] tiles = Tileset.GetTiles(Game.ROM.Subarray(Blocks, Width * Height), Width);
        byte[] gfx = LZ.Decompress(Game.ROM.From(Tileset.GFX));
        byte[][] pal = new byte[][] {
                    new byte[] { 232, 232, 232 },
                    new byte[] { 160, 160, 160 },
                    new byte[] { 88, 88, 88 },
                    new byte[] { 16, 16, 16 }};

        Bitmap bitmap = new Bitmap(Width * 2 * 2 * 8, Height * 2 * 2 * 8);
        int w = Width * 4;
        for(int i = 0; i < tiles.Length; i++) {
            byte[] pixels2 = gfx.Subarray(tiles[i] * 16, 16);
            for(int j = 0; j < 8; j++) {
                byte top = pixels2[j * 2 + 0];
                byte bot = pixels2[j * 2 + 1];
                for(int k = 0; k < 8; k++) {
                    int idx = ((i / w) * (w * 8) * 8 + j * (w * 8) + (i % w) * 8 + k) * 4;
                    byte[] col = pal[(byte) (((top >> (7 - k)) & 1) + ((bot >> (7 - k)) & 1) * 2)];
                    bitmap[idx + 0] = col[0];
                    bitmap[idx + 1] = col[1];
                    bitmap[idx + 2] = col[2];
                    bitmap[idx + 3] = 0xff;
                }
            }
        }

        return bitmap;
    }
}