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
    public PermissionSet Permission;
    public byte[] CollisionData;
    public List<(byte, byte)> TilePairCollisions;

    public RbyTileset(Rby game, byte id, List<(byte, byte)> collisions, ByteStream data) {
        Game = game;
        Id = id;
        TilePairCollisions = collisions;

        Bank = data.u8();
        BlockPointer = data.u16le();
        GfxPointer = data.u16le();
        CollisionPointer = data.u16le();
        CounterTiles = data.Read(3);
        GrassTile = data.u8();
        Permission = new PermissionSet();
        Permission.Add(data.u8());

        byte collisionBank = (byte) (GetType() == typeof(Yellow) ? 0x01 : 0x00);
        ByteStream collisionDataStream = game.ROM.From((collisionBank << 16) | CollisionPointer);
        List<byte> collData = new List<byte>();
        byte tile;
        while((tile = collisionDataStream.u8()) != 0xFF) {
            collData.Add(tile);
        }
        CollisionData = collData.ToArray();
    }
}
