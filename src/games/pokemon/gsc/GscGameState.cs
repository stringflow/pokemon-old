using System;
using System.Collections.Generic;

public struct GscObject {

    public byte Sprite;
    public byte MapObjectIndex;
    public byte SpriteTile;
    public GscSpriteMovement MovementType;
    public ushort Flags;
    public GscPalette Palette;
    public byte Walking;
    public byte Direction;
    public byte StepType;
    public byte StepDuration;
    public byte Action;
    public byte ObjectStepFrame;
    public byte Facing;
    public byte StandingTile;
    public byte LastTile;
    public byte StandingMapX;
    public byte StandingMapY;
    public byte LastMapX;
    public byte LastMapY;
    public byte ObjectInitX;
    public byte ObjectInitY;
    public byte Radius;
    public byte SpriteX;
    public byte SpriteY;
    public byte SpriteXOffset;
    public byte SpriteYOffset;
    public byte MovementByteIndex;
    public byte Field1c;
    public byte Field1d;
    public byte Field1e;
    public byte Field1f;
    public byte Range;

    public GscObject(RAMStream data) {
        Sprite = data.u8();
        MapObjectIndex = data.u8();
        SpriteTile = data.u8();
        MovementType = (GscSpriteMovement) data.u8();
        Flags = data.u16be();
        Palette = (GscPalette) data.u8();
        Walking = data.u8();
        Direction = data.u8();
        StepType = data.u8();
        StepDuration = data.u8();
        Action = data.u8();
        ObjectStepFrame = data.u8();
        Facing = data.u8();
        StandingTile = data.u8();
        LastTile = data.u8();
        StandingMapX = data.u8();
        StandingMapY = data.u8();
        LastMapX = data.u8();
        LastMapY = data.u8();
        ObjectInitX = data.u8();
        ObjectInitY = data.u8();
        Radius = data.u8();
        SpriteX = data.u8();
        SpriteY = data.u8();
        SpriteXOffset = data.u8();
        SpriteYOffset = data.u8();
        MovementByteIndex = data.u8();
        Field1c = data.u8();
        Field1d = data.u8();
        Field1e = data.u8();
        Field1f = data.u8();
        Range = data.u8();
    }

    public List<GscTile> Sight(GscMap map) {
        Action dir = ActionFunctions.FromSpriteDirection(Direction);
        List<GscTile> sight = new List<GscTile>();
        GscTile current = map[StandingMapX - 4, StandingMapY - 4];
        for(int j = 0; j < Range; j++) {
            current = current.GetNeighbor(dir);
            if(current == null) break;
            sight.Add(current);
        }

        return sight;
    }
}

public struct GscMapObject {

    public byte StructID;
    public byte Sprite;
    public byte YCoord;
    public byte XCOord;
    public byte Movement;
    public byte Raidus;
    public byte Hour;
    public byte TimeOfDay;
    public GscPalette Color;
    public byte Range;
    public ushort Script;
    public ushort EventFlag;

    public GscMapObject(RAMStream data) {
        StructID = data.u8();
        Sprite = data.u8();
        YCoord = data.u8();
        XCOord = data.u8();
        Movement = data.u8();
        Raidus = data.u8();
        Hour = data.u8();
        TimeOfDay = data.u8();
        Color = (GscPalette) data.u8();
        Range = data.u8();
        Script = data.u16le();
        EventFlag = data.u16le();
    }
}

public partial class Gsc {

    public GscPokemon BattleMon {
        get { return ReadBattleStruct(From("wBattleMon"), From("wPlayerStatLevels"), From("wPlayerSubStatus1"), SYM["wPlayerScreens"]); }
    }

    public GscPokemon EnemyMon {
        get { return ReadBattleStruct(From("wEnemyMon"), From("wEnemyStatLevels"), From("wEnemySubStatus1"), SYM["wEnemyScreens"]); }
    }

    public GscPokemon PartyMon1 {
        get { return ReadPartyStruct(From("wPartyMon1")); }
    }

    public GscPokemon PartyMon2 {
        get { return ReadPartyStruct(From("wPartyMon2")); }
    }

    public GscPokemon PartyMon3 {
        get { return ReadPartyStruct(From("wPartyMon3")); }
    }

    public GscPokemon PartyMon4 {
        get { return ReadPartyStruct(From("wPartyMon4")); }
    }

    public GscPokemon PartyMon5 {
        get { return ReadPartyStruct(From("wPartyMon5")); }
    }

    public GscPokemon PartyMon6 {
        get { return ReadPartyStruct(From("wPartyMon6")); }
    }

    public GscPokemon PartyMon(int index) {
        return ReadPartyStruct(From(SYM["wPartyMons"] + index * (SYM["wPartyMon2"] - SYM["wPartyMon1"])));
    }

    public int PartySize {
        get { return CpuRead("wPartyCount"); }
    }

    public GscPokemon[] Party {
        get {
            GscPokemon[] party = new GscPokemon[PartySize];
            for(int i = 0; i < party.Length; i++) {
                party[i] = PartyMon(i);
            }
            return party;
        }
    }

    public GscPokemon BoxMon(int index) {
        return ReadPartyStruct(From(SYM["wBoxMons"] + index * (SYM["wBoxMon2"] - SYM["wBoxMon1"])));
    }

    public GscMap Map {
        get { return Maps[CpuReadBE<ushort>("wMapGroup")]; }
    }

    public GscTile Tile {
        get { return Map[XCoord, YCoord]; }
    }

    public byte XCoord {
        get { return CpuRead("wXCoord"); }
    }

    public byte YCoord {
        get { return CpuRead("wYCoord"); }
    }

    public GscObject Object(int index) {
        return new GscObject(From(SYM["wObject" + index + "Struct"]));
    }

    public GscMapObject MapObject(int index) {
        return new GscMapObject(From(SYM["wMap" + index + "Object"]));
    }

    public GscObject[] Objects {
        get {
            GscObject[] objects = new GscObject[13];
            for(int i = 1; i < objects.Length; i++) {
                objects[i - 1] = Object(i);
            }
            return objects;
        }
    }

    public GscMapObject[] MapObjects {
        get {
            GscMapObject[] objects = new GscMapObject[16];
            for(int i = 1; i < objects.Length; i++) {
                objects[i - 1] = MapObject(i);
            }
            return objects;
        }
    }

    public bool InBattle {
        get { return CpuRead("wBattleMode") > 0; }
    }

    private GscPokemon ReadBattleStruct(RAMStream data, RAMStream modifiers, RAMStream battleStatus, int screensAddr) {
        GscPokemon mon = new GscPokemon();
        mon.Species = Species[data.u8()];
        mon.HeldItem = Items[data.u8()];
        mon.Moves = Array.ConvertAll(data.Read(4), m => Moves[m]);
        mon.DVs = data.u16be();
        mon.PP = data.Read(4);
        mon.Happiness = data.u8();
        mon.Level = data.u8();
        mon.Status = data.u8();
        data.Seek(1); // unused
        mon.HP = data.u16be();
        mon.MaxHP = data.u16be();
        mon.Attack = data.u16be();
        mon.Defense = data.u16be();
        mon.Speed = data.u16be();
        mon.SpecialAttack = data.u16be();
        mon.SpecialDefense = data.u16be();
        mon.AttackModifider = modifiers.u8();
        mon.DefenseModifider = modifiers.u8();
        mon.SpeedModifider = modifiers.u8();
        mon.SpecialAttackModifider = modifiers.u8();
        mon.SpecialDefenseModifider = modifiers.u8();
        mon.AccuracyModifider = modifiers.u8();
        mon.EvasionModifider = modifiers.u8();
        mon.BattleStatus1 = battleStatus.u8();
        mon.BattleStatus2 = battleStatus.u8();
        mon.BattleStatus3 = battleStatus.u8();
        mon.BattleStatus4 = battleStatus.u8();
        mon.BattleStatus5 = battleStatus.u8();
        mon.Screens = CpuRead(screensAddr);
        mon.CalculateUnmodifiedStats();
        return mon;
    }

    private GscPokemon ReadBoxStruct(RAMStream data) {
        GscPokemon mon = new GscPokemon();
        mon.Species = Species[data.u8()];
        mon.HeldItem = Items[data.u8()];
        mon.Moves = Array.ConvertAll(data.Read(4), m => Moves[m]);
        data.Seek(2); // ID
        mon.Experience = data.u24be();
        mon.HPExp = data.u16be();
        mon.AttackExp = data.u16be();
        mon.DefenseExp = data.u16be();
        mon.SpeedExp = data.u16be();
        mon.SpecialExp = data.u16be();
        mon.DVs = data.u16be();
        mon.PP = data.Read(4);
        mon.Happiness = data.u8();
        mon.Pokerus = data.u8() > 0;
        data.Seek(2); // unused
        mon.Level = data.u8();
        mon.CalculateUnmodifiedStats();
        return mon;
    }

    private GscPokemon ReadPartyStruct(RAMStream data) {
        GscPokemon mon = ReadBoxStruct(data);
        mon.Status = data.u8();
        data.Seek(1); // unused
        mon.HP = data.u16be();
        mon.MaxHP = data.u16be();
        mon.Attack = data.u16be();
        mon.Defense = data.u16be();
        mon.Speed = data.u16be();
        mon.SpecialAttack = data.u16be();
        mon.SpecialDefense = data.u16be();
        return mon;
    }

    public override byte[] ReadCollisionMap() {
        GscMap map = Map;
        int width = map.Width;
        int height = map.Height;

        byte[] overworldMap = CpuRead("wOverworldMapBlocks", 1300);
        byte[] blocks = new byte[width * height];

        for(int i = 0; i < height; i++) {
            Array.Copy(overworldMap, (i + 3) * (width + 6) + 3, blocks, i * width, width);
        }

        byte[] collision = new byte[width * 2 * height * 2];
        for(int i = 0; i < blocks.Length; i++) {
            byte block = blocks[i];
            for(int j = 0; j < 4; j++) {
                byte coll = ROM[map.Tileset.Coll + block * 4 + j];
                int tileSpaceIndex = i * 2 + (j & 1) + (j >> 1) * (width * 2) + (i / width * 2 * width);
                collision[tileSpaceIndex] = coll;
            }
        }

        return collision;
    }

    public override bool Surfing() {
        return false;
    }

    public override bool Biking() {
        return false;
    }
}