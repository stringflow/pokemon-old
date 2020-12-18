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
    public Map<byte, byte> TilePairCollisionsLand;
    public Map<byte, byte> TilePairCollisionsWater;
    public PermissionSet LandPermissions;
    public PermissionSet WaterPermissions;

    public RbyTileset(Rby game, byte id, ByteStream data) {
        Game = game;
        Id = id;

        Bank = data.u8();
        BlockPointer = data.u16le();
        GfxPointer = data.u16le();
        CollisionPointer = data.u16le();
        CounterTiles = data.Read(3);
        GrassTile = data.u8();
        data.Seek(1);

        TilePairCollisionsLand = new Map<byte, byte>();
        TilePairCollisionsWater = new Map<byte, byte>();

        LandPermissions = new PermissionSet();
        LandPermissions.AddRange(game.ROM.From((game is Yellow ? 0x01 : 0x00) << 16 | CollisionPointer).Until(0xff));
        WaterPermissions = new PermissionSet();
        WaterPermissions.Add(0x14);
        WaterPermissions.Add(0x32);
        if(id == 14) WaterPermissions.Add(0x48);
    }
}
