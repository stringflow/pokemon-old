using System;
using System.Collections.Generic;
using System.Linq;

public class RbyLedge {

    public byte Source;
    public byte Ledge;
    public Action ActionRequired;
}

public class RbyConnection : Connection<RbyMap, RbyTile> {

    public RbyMap Map;
    public byte DestMapId;

    public RbyConnection(RbyMap map, ReadStream data) {
        Map = map;
        DestMapId = data.u8();
        Source = data.u16le();
        Destination = data.u16le();
        Length = data.u8();
        Width = data.u8();
        YAlignment = data.u8();
        XAlignment = data.u8();
        Window = data.u16le();
    }

    public override RbyMap GetDestinationMap() {
        return Map.Game.Maps[DestMapId];
    }
}

public class RbyTile : Tile<RbyMap, RbyTile> {

    public string PokeworldLink {
        get { return "https://gunnermaniac.com/pokeworld?local=" + Map.Id + "#" + X + "/" + Y; }
    }

    public override int CalcStepCost(bool onBike, bool ledgeHop, bool warp, Action action) {
        if(ledgeHop) return Rby.LedgeHopCost;
        if(warp) return Rby.WarpCost;

        bool cyclingRoad = Map.Id == 28 || (Map.Id == 27 && Y >= 10 && X >= 23);

        if(cyclingRoad) {
            return action == Action.Down ? Rby.BikeCost : Rby.WalkCost;
        } else {
            return onBike ? Rby.BikeCost : Rby.WalkCost;
        }
    }

    public override bool LedgeCheck(RbyTile ledgeTile, Action action) {
        return Map.Game.Ledges.Any(ledge => ledge.Source == Collision && ledge.Ledge == ledgeTile.Collision && ledge.ActionRequired == action);
    }

    public override (RbyTile TileToWarpTo, Action ActionRequired) WarpCheck() {
        RbyWarp srcWarp = Map.Warps[X, Y];
        if(srcWarp != null) {
            RbyMap destMap = Map.Game.Maps[srcWarp.DestinationMap];
            if(destMap != null) {
                RbyWarp destWarp = destMap.Warps[srcWarp.DestinationIndex];
                if(destWarp != null) {
                    RbyTile destTile = destMap[destWarp.X, destWarp.Y];
                    if(Array.IndexOf(Map.Tileset.WarpTiles, Collision) != -1) return (destTile, Action.None);
                    else {
                        Action action = ExtraWarpCheck();
                        if(action != Action.None) {
                            return (destTile, action);
                        }
                    }
                }
            }
        }

        return (null, Action.None);
    }

    private Action ExtraWarpCheck() {
        int map = Map.Id;
        byte tileset = Map.Tileset.Id;

        // https://github.com/pret/pokered/blob/master/home/overworld.asm#L719-L747
        if(map == 0x61) return EdgeOfMapWarpCheck();
        else if(map == 0xc7) return DirectionalWarpCheck();
        else if(map == 0xc8) return DirectionalWarpCheck();
        else if(map == 0xca) return DirectionalWarpCheck();
        else if(map == 0x52) return DirectionalWarpCheck();
        else if(tileset == 0) return DirectionalWarpCheck();
        else if(tileset == 0xd) return DirectionalWarpCheck();
        else if(tileset == 0xe) return DirectionalWarpCheck();
        else if(tileset == 0x17) return DirectionalWarpCheck();
        else return EdgeOfMapWarpCheck();
    }

    public Action EdgeOfMapWarpCheck() {
        if(X == 0) return Action.Left;
        else if(X == Map.Width * 2 - 1) return Action.Right;
        else if(Y == 0) return Action.Up;
        else if(Y == Map.Height * 2 - 1) return Action.Down;

        return Action.None;
    }

    public Action DirectionalWarpCheck() {
        for(int i = 0; i < 4; i++) {
            Action action = (Action) (0x10 << i);
            RbyTile neighbor = GetNeighbor(action);
            byte collision;
            if(neighbor == null) {
                int offs;
                if(action == Action.Up) offs = 2 + X % 2;
                else if(action == Action.Down) offs = X % 2;
                else if(action == Action.Right) offs = (Y % 2) * 2;
                else offs = 1 + (Y % 2) * 2;

                collision = Map.Game.ROM[(Map.Tileset.Bank << 16 | Map.Tileset.BlockPointer) + Map.BorderBlock * 16 + offs * 4 + 3];
            } else {
                collision = neighbor.Collision;
            }
            if(Array.IndexOf(Map.Game.DirectionalWarpTiles[action], collision) != -1) return action;
        }

        return Action.None;
    }

    public override bool IsDoorTile() {
        return Array.IndexOf(Map.Tileset.DoorTiles, Collision) != -1;
    }

    public override bool CollisionCheckLand(RbyTile dest, byte[] collisionMap, Action action, bool allowTrainerVision) {
        return CollisionCheck(dest, collisionMap, allowTrainerVision, Map.Tileset.LandPermissions, Map.Tileset.TilePairCollisionsLand);
    }

    public override bool CollisionCheckWater(RbyTile dest, byte[] collisionMap, Action action, bool allowTrainerVision) {
        return CollisionCheck(dest, collisionMap, allowTrainerVision, Map.Tileset.WaterPermissions, Map.Tileset.TilePairCollisionsWater);
    }

    public bool CollisionCheck(RbyTile dest, byte[] collisionMap, bool allowTrainerVision, PermissionSet permissions, List<int> tilePairCollisions) {
        if(dest == null) return false;
        if(!IsTilePassable(collisionMap, dest, permissions, tilePairCollisions)) return false;
        if(IsCollidingWithSprite(dest, collisionMap != null)) return false;
        if(!allowTrainerVision && IsMovingIntoTrainerVision(dest)) return false; // allow moving into trainer vision on the end tile
        if(BlockSpinningTiles(dest)) return false;
        return true;
    }

    private bool IsTilePassable(byte[] collisionMap, RbyTile dest, PermissionSet permissions, List<int> tilePairCollisions) {
        byte destCollision;
        if(collisionMap != null) {
            destCollision = collisionMap[dest.X + dest.Y * dest.Map.Width * 2];
        } else {
            destCollision = dest.Collision;
        }

        if(!permissions.IsAllowed(destCollision)) return false;
        if(tilePairCollisions.Contains(Collision << 8 | destCollision)) return false;

        return true;
    }

    private bool IsCollidingWithSprite(RbyTile dest, bool readFromRam) {
        if(readFromRam) {
            for(int spriteIndex = 1; spriteIndex < (Map.Game.IsYellow ? 15 : 16); spriteIndex++) {
                RbySprite sprite = dest.Map.Sprites[spriteIndex - 1];
                if(!IsSpriteHidden(sprite)) {
                    int spriteX = Map.Game.CpuRead(0xc205 | (spriteIndex << 4)) - 4;
                    int spriteY = Map.Game.CpuRead(0xc204 | (spriteIndex << 4)) - 4;
                    if(spriteX == dest.X && spriteY == dest.Y) return true;
                }
            }
        } else {
            foreach(RbySprite sprite in dest.Map.Sprites) {
                if(!IsSpriteHidden(sprite) && sprite.X == dest.X && sprite.Y == dest.Y) return true;
            }
        }

        return false;
    }

    private bool IsSpriteHidden(RbySprite sprite) {
        if(sprite == null) return false;

        return sprite.CanBeMissable && (Map.Game.CpuRead(sprite.MissableAddress) & (1 << sprite.MissableBit)) > 0;
    }

    private bool BlockSpinningTiles(RbyTile dest) {
        // TODO: Handle them properly ecks dee
        return dest.Map.Tileset.Id == 7 && (dest.Collision == 0x3c || dest.Collision == 0x3d || dest.Collision == 0x4c || dest.Collision == 0x4d);
    }

    private bool IsMovingIntoTrainerVision(RbyTile tile) {
        foreach(RbyTrainer trainer in tile.Map.Trainers) {
            if((Map.Game.CpuRead(trainer.EventFlagAddress) & (1 << trainer.EventFlagBit)) != 0) continue;

            int range = trainer.SightRange;
            if(trainer.Direction == Action.Down && tile.Y - trainer.Y == 4) range--;

            RbyTile current = trainer.Map[trainer.X, trainer.Y];
            for(int i = 0; i < range && current != null; i++) {
                current = current.GetNeighbor(trainer.Direction);
                if(current == tile) {
                    return true;
                }
            }
        }

        return false;
    }
}

public class RbyWarp {

    public RbyMap Map;
    public byte Y;
    public byte X;
    public byte Index;
    public byte DestinationIndex;
    public byte DestinationMap;

    public bool Allowed;

    public RbyWarp(Rby game, RbyMap map, byte index, ReadStream data) {
        Map = map;
        Index = index;
        Y = data.u8();
        X = data.u8();
        DestinationIndex = data.u8();
        DestinationMap = data.u8();
        Allowed = false;

        if(DestinationMap == 0xff) {
            if(!game.IsYellow) DestinationMap = RedBlue.wLastMapDestinations[(map.Name, index)];
            else DestinationMap = Yellow.wLastMapDestinations[(map.Name, index)];
        }
    }
}

public class RbyMap : Map<RbyMap, RbyTile> {

    public Rby Game;
    public string Name;
    public byte Bank;
    public RbyTileset Tileset;
    public ushort DataPointer;
    public ushort TextPointer;
    public ushort ScriptPointer;
    public byte ConnectionFlags;
    public ushort ObjectPointer;
    public byte BorderBlock;
    public DataList<RbyWarp> Warps;
    public DataList<RbySprite> Sprites;
    public DataList<RbyTrainer> Trainers;
    public DataList<RbyItemBall> ItemBalls;

    public RbyMap(Rby game, string name, byte id, ReadStream data) {
        Game = game;
        Name = name;
        Id = id;
        Bank = (byte) (data.Position >> 16);
        Tileset = game.Tilesets[data.u8()];
        Height = data.u8();
        Width = data.u8();
        DataPointer = data.u16le();
        TextPointer = data.u16le();
        ScriptPointer = data.u16le();
        ConnectionFlags = data.u8();
        Connections = new RbyConnection[4];
        for(int i = 3; i >= 0; i--) {
            if(((ConnectionFlags >> i) & 1) == 1) {
                Connections[i] = new RbyConnection(this, data);
            }
        }
        ObjectPointer = data.u16le();

        ReadStream objectData = game.ROM.From(Bank << 16 | ObjectPointer);

        BorderBlock = objectData.u8();

        Warps = new DataList<RbyWarp>();
        Warps.IndexCallback = obj => obj.Index;
        Warps.PositionCallback = obj => (obj.X, obj.Y);
        byte numWarps = objectData.u8();
        for(byte i = 0; i < numWarps; i++) {
            Warps.Add(new RbyWarp(game, this, i, objectData));
        }

        byte numSigns = objectData.u8();
        objectData.Seek(numSigns * 3); // TODO: Parse signs

        Sprites = new DataList<RbySprite>();
        Sprites.IndexCallback = obj => obj.SpriteId;
        Sprites.PositionCallback = obj => (obj.X, obj.Y);
        Trainers = new DataList<RbyTrainer>();
        Trainers.IndexCallback = obj => obj.SpriteId;
        Trainers.PositionCallback = obj => (obj.X, obj.Y);
        ItemBalls = new DataList<RbyItemBall>();
        ItemBalls.IndexCallback = obj => obj.SpriteId;
        ItemBalls.PositionCallback = obj => (obj.X, obj.Y);
        byte numSprites = objectData.u8();
        for(byte i = 0; i < numSprites; i++) {
            RbySprite sprite = new RbySprite(game, this, i, objectData);
            Sprites.Add(sprite);
            if(sprite.IsTrainer) {
                Trainers.Add(new RbyTrainer(sprite, objectData));
            } else if(sprite.IsItem) {
                ItemBalls.Add(new RbyItemBall(sprite, objectData));
            }
        }

        Tiles = new RbyTile[Width * 2, Height * 2];
        if(Tileset != null) {
            for(int i = 0; i < Width * Height; i++) {
                byte block = game.ROM[(Bank << 16 | DataPointer) + i];
                byte[] tiles = Game.ROM.Subarray(Tileset.Bank << 16 | Tileset.BlockPointer + block * 16, 16);
                for(int j = 0; j < 4; j++) {
                    int tileSpaceIndex = i * 2 + (j & 1) + (j >> 1) * (Width * 2) + (i / Width * 2 * Width);
                    byte xt = (byte) (tileSpaceIndex % (Width * 2));
                    byte yt = (byte) (tileSpaceIndex / (Width * 2));
                    Tiles[xt, yt] = new RbyTile {
                        Map = this,
                        X = xt,
                        Y = yt,
                        Collision = tiles[(j >> 1) * 8 + (j & 1) * 2 + 4],
                    };
                }
            }
        }
    }

    public override bool AllowsBiking() {
        return Tileset.AllowBike;
    }


    public override Bitmap Render() {
        byte[] tiles = Tileset.GetTiles(Game.ROM.Subarray(Bank << 16 | DataPointer, Width * Height), Width);

        byte[] pixels = new byte[tiles.Length * 16];
        for(int i = 0; i < tiles.Length; i++) {
            Array.Copy(Game.ROM.Data, Tileset.Bank << 16 | Tileset.GfxPointer + tiles[i] * 16, pixels, i * 16, 16);
        }

        Bitmap bitmap = new Bitmap(Width * 2 * 2 * 8, Height * 2 * 2 * 8);
        bitmap.Unpack2BPP(pixels, Game.BGPalette());

        foreach(RbySprite sprite in Sprites) {
            int headerPointer = Game.SYM["SpriteSheetPointerTable"] + (sprite.PictureId - 1) * 4;
            int spritePointer = Game.ROM[headerPointer + 3] << 16 | Game.ROM.u16le(headerPointer);
            bool flip = false;
            int spriteIndex;
            switch(sprite.Direction) {
                case Action.Down: spriteIndex = 0; break;
                case Action.Up: spriteIndex = 1; break;
                case Action.Left: spriteIndex = 2; break;
                case Action.Right: spriteIndex = 2; flip = true; break;
                default: spriteIndex = 0; break;
            }
            Bitmap spriteBitmap = new Bitmap(16, 16);
            spriteBitmap.Unpack2BPP(Game.ROM.Subarray(spritePointer + spriteIndex * 16 * 4, 16 * 4), Game.ObjPalette(), true);
            if(flip) spriteBitmap.Flip(true, false);
            bitmap.DrawBitmap(spriteBitmap, sprite.X * 16, sprite.Y * 16 - 4);
        }

        return bitmap;
    }

    public override string ToString() {
        return Name;
    }
}