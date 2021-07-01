using System;

public enum CollisionPermissions {

    Land = 0,
    Water = 1,
    Wall = 0xf,
    Talk = 0x10,
}

public class GscTileset {

    public Gsc Game;
    public byte Id;
    public int GFX;
    public int Meta;
    public int Coll;
    public ushort Anim;
    public ushort PalMap;
    public PermissionSet LandPermissions;
    public PermissionSet WaterPermissions;

    public GscTileset(Gsc game, byte id, ReadStream data) {
        Game = game;
        Id = id;
        GFX = data.u8() << 16 | data.u16le();
        Meta = data.u8() << 16 | data.u16le();
        Coll = data.u8() << 16 | data.u16le();
        Anim = data.u16le();
        data.Seek(2);
        PalMap = data.u16le();

        LandPermissions = new PermissionSet();
        WaterPermissions = new PermissionSet();
        ReadStream collisionData = game.ROM.From("TileCollisionTable");
        for(int i = 0; i < 256; i++) {
            CollisionPermissions perms = (CollisionPermissions) (collisionData.u8() & 0xf); // Ignore the upper nybble as it only indicates whether the tile can be interacted with.
            if(perms == CollisionPermissions.Land) LandPermissions.Add((byte) i);
            else if(perms == CollisionPermissions.Water) WaterPermissions.Add((byte) i);
        }
    }

    public byte[] GetTiles(byte[] blocks, int width) {
        int length = blocks.Length - blocks.Length % width;
        byte[] tiles = new byte[length * 4 * 4];
        for(int i = 0; i < length; i++) {
            byte[] tiles2 = Game.ROM.Subarray(Meta + blocks[i] * 16, 16);
            for(int j = 0; j < 4; j++) {
                Array.Copy(tiles2, j * 4, tiles, (i / width) * (width * 4) * 4 + j * (width * 4) + (i % width) * 4, 4);
            }
        }
        return tiles;
    }
}