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

public class GscConnection : Connection<GscMap, GscTile> {

    public GscMap Map;
    public byte DestGroup;
    public byte DestNumber;

    public GscConnection(GscMap map, ReadStream data) {
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

    public override GscMap GetDestinationMap() {
        return Map.Game.Maps[DestGroup << 8 | DestNumber];
    }
}

public class GscTile : Tile<GscMap, GscTile> {

    public override int CalcStepCost(bool onBike, bool ledgeHop, bool warp, Action action) {
        int stepCost = onBike ? 9 : 17;

        if(ledgeHop) return stepCost * 2 + 1;
        if(warp) return 100;
        return stepCost;
    }

    public override bool LedgeCheck(GscTile ledgeTile, Action action) {
        return (Collision == 0xa0 && action == Action.Right) || (Collision == 0xa1 && action == Action.Left) ||
               (Collision == 0xa2 && action == Action.Up) || (Collision == 0xa3 && action == Action.Down);
    }

    public override (GscTile TileToWarpTo, Action ActionRequired) WarpCheck() {
        GscWarp warp = Map.Warps[X, Y];
        if(warp != null) {
            GscWarp destWarp = Map.Game.Maps[warp.MapId].Warps[warp.DestinationIndex];
            if(destWarp != null) {
                GscTile destTile = destWarp.Map[destWarp.X, destWarp.Y];
                if(destTile != null) {
                    Action actionRequired = Action.None;
                    switch(Collision) {
                        case 0x70: actionRequired = Action.Down; break;
                        case 0x76: actionRequired = Action.Left; break;
                        case 0x78: actionRequired = Action.Up; break;
                        case 0x7e: actionRequired = Action.Right; break;
                    }
                    return (destWarp.Map[destWarp.X, destWarp.Y], actionRequired);
                }
            }
        }
        return (null, Action.None);
    }

    public override bool IsDoorTile() {
        byte[] doors = {
            0x71, 0x73, 0x74, 0x75, 0x79, 0x7a, 0x7b, 0x7c, 0x7d,
        };
        return Array.IndexOf(doors, Collision) != -1;
    }

    public override bool CollisionCheckLand(GscTile dest, byte[] collisionMap, Action action, bool allowTrainerVision) {
        return CollisionCheck(dest, collisionMap, action, allowTrainerVision, dest.Map.Tileset.LandPermissions);
    }

    public override bool CollisionCheckWater(GscTile dest, byte[] collisionMap, Action action, bool allowTrainerVision) {
        return CollisionCheck(dest, collisionMap, action, allowTrainerVision, dest.Map.Tileset.WaterPermissions);
    }

    private bool CollisionCheck(GscTile dest, byte[] collisionMap, Action action, bool allowTrainerVision, PermissionSet permissions) {
        if(dest == null) return false;
        if(!IsTilePassable(collisionMap, dest, action, permissions)) return false;
        if(IsCollidingWithSprite(dest, collisionMap != null)) return false;
        if(!allowTrainerVision && IsMovingIntoTrainerVision(dest)) return false;
        return true;
    }

    private bool IsTilePassable(byte[] collisionMap, GscTile dest, Action action, PermissionSet permissions) {
        byte destCollision;
        if(collisionMap != null) {
            destCollision = collisionMap[dest.X + dest.Y * dest.Map.Width * 2];
        } else {
            destCollision = dest.Collision;
        }

        byte hiNybble = (byte) (destCollision & 0xf0);
        if(hiNybble == 0xb0 || hiNybble == 0xc0) {
            byte dirCollision = (byte) (destCollision & 0x7);
            if(action == Action.Down && (dirCollision == 2 || dirCollision == 6 || dirCollision == 4)) return false;
            else if(action == Action.Up && (dirCollision == 3 || dirCollision == 4 || dirCollision == 5)) return false;
            else if(action == Action.Right && (dirCollision == 1 || dirCollision == 5 || dirCollision == 7)) return false;
            else if(action == Action.Left && (dirCollision == 0 || dirCollision == 4 || dirCollision == 6)) return false;
        }

        if(!permissions.IsAllowed(destCollision)) return false;

        return true;
    }

    private bool IsCollidingWithSprite(GscTile dest, bool readFromRam) {
        if(readFromRam) {
            for(int spriteIndex = 1; spriteIndex < 16; spriteIndex++) {
                GscSprite sprite = dest.Map.Sprites[spriteIndex - 1];
                if(sprite != null && !IsSpriteHidden(sprite)) {
                    int spriteX = Map.Game.CpuRead(0xd701 | ((spriteIndex + 2) << 4)) - 4;
                    int spriteY = Map.Game.CpuRead(0xd700 | ((spriteIndex + 2) << 4)) - 4;
                    if(spriteX == dest.X && spriteY == dest.Y) return true;
                }
            }
        } else {
            foreach(GscSprite sprite in dest.Map.Sprites) {
                if(!IsSpriteHidden(sprite) && sprite.X == dest.X && sprite.Y == dest.Y) return true;
            }
        }

        return false;
    }

    private bool IsMovingIntoTrainerVision(GscTile dest) {
        foreach(GscSprite sprite in Map.Sprites) {
            if(sprite.Function != GscSpriteType.Trainer) continue;
            if(IsSpriteHidden(sprite)) continue;
            if(IsTrainerDefeated(sprite)) continue;
            if(sprite.IsSpinner) continue;

            GscTile current = sprite.Map[sprite.X, sprite.Y];
            for(int i = 0; i < sprite.SightRange && current != null; i++) {
                current = current.GetNeighbor(sprite.Direction);
                if(current == dest) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsSpriteHidden(GscSprite sprite) {
        return CheckEventFlag(sprite.EventFlag);
    }

    private bool IsTrainerDefeated(GscSprite sprite) {
        return CheckEventFlag(Map.Game.ROM.u16le((sprite.Map.Scripts & 0xff0000) | sprite.ScriptPointer));
    }

    private bool CheckEventFlag(int flag) {
        int offs = flag / 8;
        int bit = flag % 8;
        return (Map.Game.CpuRead(Map.Game.SYM["wEventFlags"] + offs) & (1 << bit)) > 0;
    }
}

public class GscMap : Map<GscMap, GscTile> {

    // TODO: Environment, Location, and Music should all be enums.
    //       Because they are currently unused I left them as bytes to reduce noise in the code.
    public Gsc Game;
    public string Name;
    public byte Group;
    public byte Number;
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
    public DataList<GscWarp> Warps;
    public DataList<GscCoordEvent> CoordEvents;
    public DataList<GscBGEvent> BGEvents;
    public DataList<GscSprite> Sprites;

    public GscMap(Gsc game, int group, int number, ReadStream data) {
        Game = game;
        Group = (byte) group;
        Number = (byte) number;
        Id = group << 8 | number;
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

        ReadStream attributesData = game.ROM.From(Attributes);
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

        ReadStream eventsData = game.ROM.From(Events + 2);

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
        Sprites.IndexCallback = obj => obj.Id;
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

    public override bool AllowsBiking() {
        return false;
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
        if(Game.IsCrystal) {
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

        ushort[] bgPalData = Game.ROM.From("TilesetBGPalette").ReadLE(168);
        ushort[] roofPalData = Game.ROM.From(Game.SYM["RoofPals"] + Group * 8).ReadLE(4);
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