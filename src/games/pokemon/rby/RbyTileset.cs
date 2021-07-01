using System;
using System.Collections.Generic;

public class RbyTileset {

    public Rby Game;
    public byte Id;
    public byte Bank;
    public ushort BlockPointer;
    public ushort GfxPointer;
    public ushort CollisionPointer;
    public byte[] CounterTiles;
    public byte GrassTile;
    public List<int> TilePairCollisionsLand;
    public List<int> TilePairCollisionsWater;
    public PermissionSet LandPermissions;
    public PermissionSet WaterPermissions;
    public byte[] WarpTiles;
    public byte[] DoorTiles;

    public bool AllowBike {
        get { return Id == 0x0 || Id == 0x3 || Id == 0xb || Id == 0xe || Id == 0x11; }
    }

    public RbyTileset(Rby game, byte id, ReadStream data) {
        Game = game;
        Id = id;

        Bank = data.u8();
        BlockPointer = data.u16le();
        GfxPointer = data.u16le();
        CollisionPointer = data.u16le();
        CounterTiles = data.Read(3);
        GrassTile = data.u8();
        data.Seek(1);

        TilePairCollisionsLand = new List<int>();
        TilePairCollisionsWater = new List<int>();

        LandPermissions = new PermissionSet();
        LandPermissions.AddRange(game.ROM.From((game.IsYellow ? 0x01 : 0x00) << 16 | CollisionPointer).Until(0xff, false));
        WaterPermissions = new PermissionSet();
        WaterPermissions.Add(0x14);
        WaterPermissions.Add(0x32);
        if(id == 14) WaterPermissions.Add(0x48);

        WarpTiles = game.ROM.From(3 << 16 | game.ROM.u16le(game.SYM["WarpTileIDPointers"] + id * 2)).Until(0xff, false);

        ReadStream stream = game.ROM.From("DoorTileIDPointers");
        DoorTiles = new byte[0];
        for(; ; ) {
            byte tileset = stream.u8();
            ushort pointer = stream.u16le();
            if(tileset == 0xff) break;

            if(tileset == Id) {
                DoorTiles = game.ROM.From(6 << 16 | pointer).Until(0x00, false);
            }
        }
    }

    public byte[] GetTiles(byte[] blocks, int width) {
        int length = blocks.Length - blocks.Length % width;
        byte[] tiles = new byte[length * 4 * 4];
        for(int i = 0; i < length; i++) {
            byte[] tiles2 = Game.ROM.Subarray(Bank << 16 | BlockPointer + blocks[i] * 16, 16);
            for(int j = 0; j < 4; j++) {
                Array.Copy(tiles2, j * 4, tiles, (i / width) * (width * 4) * 4 + j * (width * 4) + (i % width) * 4, 4);
            }
        }
        return tiles;
    }
}
