using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class GscTurn {

    public string Move;
    public string Pokemon;
    public int Flags;

    public GscTurn(string move, int flags = 0) {
        Move = move;
        Flags = flags;

        if((Flags & 0x3f) == 0) {
            Flags |= 39;
        }
    }

    public GscTurn(string item, string pokemon) {
        Move = item;
        Pokemon = pokemon;
    }
}

public class GscForce : Gsc {

    // forceturn flags
    public const int Miss = 0x40;
    public const int Crit = 0x80;
    public const int SideEffect = 0x100;
    public const int ThreeTurn = 0x200;
    public const int Hitself = 0x400;

    public MenuType CurrentMenuType;

    private StateCacher StateCacher;

    public GscForce(string rom) : base(rom, (string) null) {
        StateCacher = new StateCacher(GetType().Name);
    }

    public void CacheState(string name, System.Action fn) {
        StateCacher.CacheState(this, name, fn);
    }

    public void ClearCache() {
        StateCacher.ClearCache();
    }

    public void ForceTurn(GscTurn playerTurn, GscTurn opponentTurn = null, bool speedTieWin = true) {
        int playerMoveIndex = Array.IndexOf(BattleMon.Moves, Moves[playerTurn.Move]);

        GscMove enemyMove = opponentTurn != null ? Moves[opponentTurn.Move] : EnemyMon.Moves.Where(m => m.Name != "QUICK ATTACK").First();
        int opponentMoveIndex = Array.IndexOf(EnemyMon.Moves, enemyMove);
        CpuWrite("wCurEnemyMoveNum", (byte) opponentMoveIndex);
        CpuWrite("wCurEnemyMove", enemyMove.Id);

        ChooseMenuItem(0);
        ChooseMenuItem(playerMoveIndex);

        int speedTie = RunUntil("DetermineMoveOrder.player_first", "DetermineMoveOrder.enemy_first", "DetermineMoveOrder.speed_tie");
        bool playerFirst = speedTie == SYM["DetermineMoveOrder.player_first"];

        if(speedTie == SYM["DetermineMoveOrder.speed_tie"]) {
            RunUntil(SYM["DetermineMoveOrder.speed_tie"] + 0x9);
            A = speedTieWin ? 0x00 : 0xff;
            playerFirst = speedTieWin;
        }

        if(playerFirst) {
            ForceTurnInternal(playerTurn);
            if(opponentTurn != null) ForceTurnInternal(opponentTurn);
        } else {
            ForceTurnInternal(opponentTurn);
            ForceTurnInternal(playerTurn);
        }

        ClearText();
    }

    public void ForceTurnInternal(GscTurn turn) {
        int playerTurnDone = SYM["PlayerTurn_EndOpponentProtectEndureDestinyBond"] + 0xc;
        int enemyTurnDone = SYM["EnemyTurn_EndOpponentProtectEndureDestinyBond"] + 0xc;
        int random = SYM["BattleRandom"] + 0x11;

        int ret;
        while((ret = ClearTextUntil(Joypad.None, random, playerTurnDone, enemyTurnDone)) == random) {
            int addr = 0xd << 16 | CpuReadLE<ushort>(SP);
            string label = SYM[addr];

            if(label.StartsWith("BattleCommand_Critical")) {
                A = (turn.Flags & Crit) > 0 ? 0x00 : 0xff;
            } else if(label.StartsWith("BattleCommand_DamageVariation")) {
                int roll = turn.Flags & 0x3f;
                if(roll < 1) roll = 1;
                if(roll > 39) roll = 39;
                roll += 216;
                A = (byte) ((roll << 1) | (roll >> 7)); // rotate left to counter a rrca instruction
            } else if(label.StartsWith("BattleCommand_CheckHit")) {
                A = (turn.Flags & Miss) > 0 ? 0xff : 0x00;
            } else if(label.StartsWith("BattleCommand_StatDown.ComputerMiss") || label.StartsWith("BattleCommand_Poison")) {
                A = (turn.Flags & Miss) > 0 ? 0x00 : 0xff;
            } else if(label.StartsWith("BattleCommand_EffectChance")) {
                A = (turn.Flags & SideEffect) > 0 ? 0x00 : 0xff;
            } else if(label.StartsWith("BattleCommand_Spite")) {
                int pp = turn.Flags & 0x3f;
                if(pp < 2) pp = 2;
                if(pp > 5) pp = 5;
                A = pp - 2;
            } else {
                Console.WriteLine("Unhandled BattleRandom call coming from " + label);
            }

            RunFor(1);
        }

        RunFor(1);
    }

    public void ForceGiftDVs(int dvs) {
        Yes();
        ClearTextUntil(Joypad.None, SYM["GeneratePartyMonStats.initializeDVs"]);
        B = dvs >> 8;
        C = dvs & 0xff;
    }

    public void ForceEncounter(Action action, string pokemon, byte level, ushort dvs = 0x0000) {
        CpuWrite("wMornEncounterRate", 0xff);
        CpuWrite("wDayEncounterRate", 0xff);
        CpuWrite("wNiteEncounterRate", 0xff);
        CpuWrite("wWaterEncounterRate", 0xff);

        InjectOverworld((Joypad) action);
        Hold((Joypad) action, "ChooseWildEncounter.done");
        B = Species[pokemon].Id;
        CpuWrite("wCurPartyLevel", level);

        Hold((Joypad) action, "LoadEnemyMon.UpdateDVs");
        B = dvs >> 8;
        C = dvs & 0xff;
    }

    public void ForceYoloball(string ball) {
        UseItem(ball);
        ClearTextUntil(Joypad.None, SYM["PokeBallEffect.max_2"] + 0x7);
        A = 0;
    }

    public void ChooseMenuItem(int target) {
        Scroll(CalcMenuScroll(target), Joypad.A);
    }

    public void ChooseListItem(int target) {
        Scroll(CalcListScroll(target), Joypad.A);
    }

    public void Scroll((Joypad Direction, int Amount) scroll, Joypad endInput) {
        for(int i = 0; i < scroll.Amount; i++) MenuPress(scroll.Direction);
        MenuPress(Joypad.A);
    }

    public (Joypad Direction, int Amount) CalcMenuScroll(int target) {
        RunUntil("GetJoypad");
        ushort[] stack = new ushort[4];
        for(int i = 0; i < stack.Length; i++) {
            stack[i] = CpuReadLE<ushort>(SP + i * 2);
        }

        if(Array.IndexOf(stack, (ushort) ((SYM["Do2DMenuRTCJoypad.loopRTC"] & 0xffff) + 0x6)) != -1) {
            int current = CpuRead("wMenuCursorY") - 1;
            int max = CpuRead("w2DMenuNumRows") - 1;
            bool wrapping = (CpuRead("w2DMenuFlags1") & 0x20) > 0;
            return CalcScroll(current, target, max, wrapping);
        } else {
            Debug.Error("not 2d menu");
        }

        return (Joypad.None, 0);
    }

    public (Joypad Direction, int Amount) CalcListScroll(int target) {
        RunUntil("GetJoypad");

        int offset = CpuRead("wMenuScrollPosition");
        int max = CpuRead("wScrollingMenuListSize");
        if(CurrentMenuType == MenuType.ItemsPocket) {
            offset = CpuRead("wItemsPocketScrollPosition");
            max = CpuRead("wNumItems");
        } else if(CurrentMenuType == MenuType.BallsPocket) {
            offset = CpuRead("wBallsPocketScrollPosition");
            max = CpuRead("wNumBalls");
        } else if(CurrentMenuType == MenuType.KeyItemsPocket) {
            offset = CpuRead("wKeyItemsPocketScrollPosition");
            max = CpuRead("wNumKeyItems");
        } else if(CurrentMenuType == MenuType.TMHMPocket) {
            offset = CpuRead("wKeyItemsPocketScrollPosition");
            max = 0x39; // TODO: is this a problem?
        }

        return CalcScroll(CpuRead("wMenuCursorY") - 1 + offset, target, max, false);
    }

    public int FindPokemon(string name) {
        GscPokemon[] party = Party;
        int index = Array.IndexOf(party, party.Where(p => p.Species.Name == name).First());
        Debug.Assert(index != -1, "Unable to find the pokemon " + name);
        return index;
    }

    public int FindMove(GscPokemon pokemon, string name) {
        int index = Array.IndexOf(pokemon.Moves, pokemon.Moves.Where(m => m.Name == name).First());
        Debug.Assert(index != -1, "Unable to find the move " + name + " on pokemon " + pokemon.Species.Name);
        return index;
    }

    public GscItem ScrollToItem(string name) {
        GscItem item = Items[name];
        Debug.Assert(item != null, "Invalid item " + name);

        GscPocket pocket = item.Pocket;
        int index = -1;
        switch(pocket) {
            case GscPocket.Item: index = GetItemSlotItemBalls(item.Id, CpuRead("wNumItems"), From("wItems")); break;
            case GscPocket.Ball: index = GetItemSlotItemBalls(item.Id, CpuRead("wNumBalls"), From("wBalls")); break;
            case GscPocket.KeyItem: index = GetItemSlotKeyItem(item.Id); break;
            case GscPocket.TMHM: index = GetItemSlotTMHM(item.Id); break;
        }

        Debug.Assert(index != -1, "Could not find item " + name + " in pocket " + pocket);

        OpenPack();
        ScrollToPocket(item.Pocket);
        ChooseListItem(index);
        return item;
    }

    private int GetItemSlotItemBalls(byte item, byte numItems, RAMStream data) {
        for(int i = 0; i < numItems; i++) {
            if(data.u8() == item) {
                return i;
            }
            data.Seek(1);
        }

        return -1;
    }

    private int GetItemSlotKeyItem(byte item) {
        byte[] keyitems = From("wKeyItems").Read(CpuRead("wNumKeyItems"));
        return Array.IndexOf(keyitems, item);
    }

    private int GetItemSlotTMHM(byte item) {
        if(item > 0xc3) item--; // useless tm04
        if(item > 0xd2) item--; // useless tm28
        item -= Items["TM01"].Id;
        byte[] tmhm = From("wTMsHMs").Read(0x39);
        for(int i = 0, index = 0; i < tmhm.Length; i++) {
            if(tmhm[i] > 0) {
                if(i == item) return index;
                index++;
            }
        }

        return -1;
    }

    public void UseItem(string name, string pokemon) {
        UseItem(name, FindPokemon(pokemon), -1);
    }

    public void UseItem(string name, string pokemon, string move) {
        UseItem(name, FindPokemon(pokemon), move);
    }

    public void UseItem(string name, int target1, string move) {
        UseItem(name, target1, FindMove(Party[target1], move));
    }

    public void UseItem(string name, int target1 = -1, int target2 = -1) {
        GscItem item = ScrollToItem(name);
        Press(Joypad.A, Joypad.None, Joypad.A);

        if(item.Id >= Items["TM01"].Id) {
            ClearText();
            Yes();
            ChooseMenuItem(target1);
            ClearText();
            Yes();
            ClearText();
            ChooseMenuItem(target2);
            ClearText();
        } else {
            switch(item.ExecutionPointerLabel) {
                case "BicycleEffect":
                    Press(Joypad.None);
                    Press(Joypad.None);
                    ClearText();
                    CurrentMenuType = MenuType.None;
                    return;
            }
        }

        if(!InBattle) {
            Press(Joypad.None); // *PocketMenu
        }
    }

    public void RegisterItem(string name) {
        GscItem item = ScrollToItem(name);
        Press(Joypad.A, Joypad.Down, Joypad.A);
        ClearText();
    }

    public void OpenStartMenu() {
        if(InBattle) return;

        if(CurrentMenuType >= MenuType.ItemsPocket && CurrentMenuType <= MenuType.TMHMPocket) {
            MenuPress(Joypad.B);
            Press(Joypad.None);
            Press(Joypad.None);
        } else if(CurrentMenuType == MenuType.Party) {
            MenuPress(Joypad.B);
        } else if(CurrentMenuType != MenuType.StartMenu) {
            MenuPress(Joypad.Start);
            CurrentMenuType = MenuType.StartMenu;
        }
    }

    public void OpenPack() {
        if(CurrentMenuType >= MenuType.ItemsPocket && CurrentMenuType <= MenuType.TMHMPocket) return;
        if(InBattle) {
            Press(Joypad.Down, Joypad.A);
        } else if(CurrentMenuType != MenuType.Bag) {
            OpenStartMenu();
            ChooseMenuItem(2); // TODO: Pokedex obtained flag
        }

        Press(Joypad.None); // InitGFX
        Press(Joypad.None); // Init*Pocket
        Press(Joypad.None); // *PocketMenu
    }

    public void OpenParty() {
        if(CurrentMenuType == MenuType.Party) return;
        OpenStartMenu();
        ChooseMenuItem(1); // TODO: Pokedex obtained flag
    }

    public void ScrollToPocket(GscPocket pocket) {
        GscPocket[] pockets = { GscPocket.Item, GscPocket.Ball, GscPocket.KeyItem, GscPocket.TMHM };
        int targetPocket = Array.IndexOf(pockets, pocket);
        var scroll = CalcScroll(CpuRead("wCurPocket"), targetPocket, 3, true);
        scroll.Direction = (Joypad) ((int) (scroll.Direction ^ (Joypad.Up | Joypad.Down)) >> 2);

        for(int i = 0; i < scroll.Amount; i++) {
            Press(scroll.Direction);
            Press(Joypad.None); // Init*Pocket
            Press(Joypad.None); // *PocketMenu
        }

        CurrentMenuType = (MenuType) ((int) MenuType.ItemsPocket + (int) pocket - 1);
    }

    public void SetClock(int hours, int minutes) {
        UpDownScroll(10, hours, 23);
        ClearText();
        Press(Joypad.A);
        ClearText();
        UpDownScroll(0, minutes, 59);
        ClearText();
        Press(Joypad.A);
    }

    private void UpDownScroll(int current, int target, int max, bool flip = true) {
        var scroll = CalcScroll(current, target, max, true);
        if(flip) scroll.Direction ^= (Joypad.Up | Joypad.Down);

        CpuWrite("hJoyDown", (byte) scroll.Direction);

        for(int i = 0; i < scroll.Amount; i++) {
            if(PC != SYM["GetJoypad"] + 0x1) RunUntil("GetJoypad");
            Inject(scroll.Direction | (Joypad) (i % 2 + 1 << 4));
            RunFor(30);
        }

        Press(Joypad.A);
    }

    public int MoveTo(int map, int x, int y, Action preferredDirection = Action.None) {
        return MoveTo(Maps[map][x, y], preferredDirection);
    }

    public int MoveTo(string map, int x, int y, Action preferredDirection = Action.None) {
        return MoveTo(Maps[map][x, y], preferredDirection);
    }

    public override int MoveTo(int targetX, int targetY, Action preferredDirection = Action.None) {
        return MoveTo(Map[targetX, targetY], preferredDirection);
    }

    public int MoveTo(GscTile target, Action preferredDirection = Action.None, params GscTile[] additionallyBlockedTiles) {
        CloseMenu();
        RunUntil("OWPlayerInput");
        List<Action> path = Pathfinding.FindPath<GscMap, GscTile>(this, Tile, target, preferredDirection, additionallyBlockedTiles);
        return Execute(path.ToArray());
    }

    public void TalkTo(int map, int x, int y) {
        TalkTo(Maps[map][x, y], Action.None);
    }

    public void TalkTo(string map, int x, int y) {
        TalkTo(Maps[map][x, y], Action.None);
    }

    public void TalkTo(int targetX, int targetY) {
        TalkTo(Map[targetX, targetY], Action.None);
    }

    public void TalkTo(int map, int x, int y, Joypad holdButton = Joypad.None) {
        TalkTo(Maps[map][x, y], Action.None, holdButton);
    }

    public void TalkTo(string map, int x, int y, Joypad holdButton = Joypad.None) {
        TalkTo(Maps[map][x, y], Action.None, holdButton);
    }

    public void TalkTo(int targetX, int targetY, Joypad holdButton = Joypad.None) {
        TalkTo(Map[targetX, targetY], Action.None, holdButton);
    }

    public void TalkTo(int map, int x, int y, Action preferredDirection = Action.None, Joypad holdButton = Joypad.None) {
        TalkTo(Maps[map][x, y], preferredDirection);
    }

    public void TalkTo(string map, int x, int y, Action preferredDirection = Action.None, Joypad holdButton = Joypad.None) {
        TalkTo(Maps[map][x, y], preferredDirection);
    }

    public void TalkTo(int targetX, int targetY, Action preferredDirection = Action.None, Joypad holdButton = Joypad.None) {
        TalkTo(Map[targetX, targetY], preferredDirection);
    }

    public void TalkTo(GscTile target, Action preferredDirection = Action.None, Joypad holdButton = Joypad.None) {
        MoveTo(target, preferredDirection);
        Press(Joypad.A);
        ClearText(holdButton);
    }

    public override int Execute(params Action[] actions) {
        CloseMenu();

        int ret = 0;
        foreach(Action action in actions) {
            switch(action & ~Action.A) {
                case Action.Right:
                case Action.Left:
                case Action.Up:
                case Action.Down:
                    CpuWrite("wMornEncounterRate", 0);
                    CpuWrite("wDayEncounterRate", 0);
                    CpuWrite("wNiteEncounterRate", 0);
                    CpuWrite("wWaterEncounterRate", 0);

                    Joypad input = (Joypad) action;
                    RunUntil("OWPlayerInput");

                    GscTile dest = Tile.GetNeighbor(action);
                    GscObject[] objects = Objects;
                    for(int i = 0; i < objects.Length; i++) {
                        GscObject obj = objects[i];
                        if(obj.MovementType == GscSpriteMovement.SpinrandomFast) {
                            string prefix = "wObject" + (i + 1);
                            List<GscTile> sight = obj.Sight(Map);
                            if(sight.Contains(dest)) {
                                CpuWrite(prefix + "Direction", (byte) (obj.Direction ^ 4));
                                break;
                            }
                            CpuWrite(prefix + "StepDuration", 0xff);
                        }
                    }

                    InjectOverworld(input);
                    while((ret = Hold(input, "EnterMap", "CountStep", "RandomEncounter.ok", "PrintLetterDelay.checkjoypad", "DoPlayerMovement.BumpSound", "_RandomWalkContinue")) == SYM["_RandomWalkContinue"]) {
                        PC = 0x4b0f;
                        RunFor(1);
                    }

                    if(ret == SYM["EnterMap"]) {
                        Hold(input, "DisableEvents");
                        if(Tile.IsDoorTile()) {
                            while((ret = Hold(input, "CountStep", "_RandomWalkContinue")) == SYM["_RandomWalkContinue"]) {
                                PC = 0x4b0f;
                                RunFor(1);
                            } // step off of door tile
                        }
                    }

                    if(ret == SYM["DoPlayerMovement.BumpSound"]) {
                        byte turningDirection = CpuRead("wPlayerTurningDirection");
                        byte walkingDirection = CpuRead("wWalkingDirection");
                        byte playerDirection = CpuRead("wPlayerDirection");
                        if(turningDirection != 0 && walkingDirection != 0xff && playerDirection >> 2 != walkingDirection) {
                            ret = Hold(input, "PlayerMovement.turn");
                            ret = Hold(input, "OWPlayerInput");
                        }
                    } else {
                        while((ret = Hold(input, "OWPlayerInput", "RandomEncounter.ok", "PrintLetterDelay.checkjoypad", "_RandomWalkContinue")) == SYM["_RandomWalkContinue"]) {
                            PC = 0x4b0f;
                            RunFor(1);
                        }
                    }

                    if(ret != SYM["OWPlayerInput"]) {
                        return ret;
                    }

                    InjectOverworld(Joypad.None);
                    break;
            }
        }

        return ret;
    }

    public void CloseMenu() {
        if(CurrentMenuType == MenuType.None) return;

        if(CurrentMenuType == MenuType.Mart) {
            MenuPress(Joypad.B);
            ClearText();
        } else if(CurrentMenuType >= MenuType.ItemsPocket && CurrentMenuType <= MenuType.TMHMPocket) {
            MenuPress(Joypad.B);
            MenuPress(Joypad.Start);
        } else if(CurrentMenuType == MenuType.PC) {
            MenuPress(Joypad.B);
            MenuPress(Joypad.B);
        }

        CurrentMenuType = MenuType.None;
    }

    public void Yes() {
        MenuPress(Joypad.A);
    }

    public void No() {
        MenuPress(Joypad.B);
    }

    public void Nickname() {
        MenuPress(Joypad.A);
        Press(Joypad.Start, Joypad.A);
        ClearText();
    }

    public void Buy(params object[] itemsToBuy) {
        ChooseMenuItem(0);
        RunUntil("GetJoypad");

        byte[] mart = From(SYM["wCurMart"] + 1).Until(0xff, false);
        for(int i = 0; i < itemsToBuy.Length; i += 2) {
            byte item = Items[itemsToBuy[i].ToString()].Id;
            int quantity = (int) itemsToBuy[i + 1];

            int itemSlot = Array.IndexOf(mart, item);
            Debug.Assert(itemSlot != -1, "Unable to find item " + itemsToBuy[i].ToString() + " in the mart");

            ChooseListItem(itemSlot);
            ClearText();
            // TODO: Right/Left inputs for quantities >6
            UpDownScroll(1, quantity, 98);
            ClearText();
            Yes();
            ClearText();
        }

        Press(Joypad.B);
        ClearText();
        CurrentMenuType = MenuType.Mart;
    }

    public void Evolve() {
        RunUntil("PrintText");
        ClearText();
    }

    public void Cut() {
        UseOverworldMove("CUT");
    }

    public void UseOverworldMove(string name) {
        string[] overworldMoves = {
            "CUT",
            "FLY",
            "SURF",
            "STRENGTH",
            "FLASH",
            "WHIRLPOOL",
            "DIG",
            "TELEPORT",
            "HEADBUTT",
            "WATERFALL",
            "ROCKSMASH",
            "SWEETSCENT",
        };

        int partyIndex = -1;
        int moveIndex = -1;
        for(int i = 0; i < PartySize && partyIndex == -1; i++) {
            GscPokemon partyMon = PartyMon(i);
            moveIndex = 0;
            for(int j = 0; j < 4; j++) {
                GscMove move = partyMon.Moves[j];
                if(move == null) continue;
                if(overworldMoves.Contains(move.Name)) {
                    if(move.Name == name) {
                        partyIndex = i;
                        break;
                    }
                    moveIndex++;
                }
            }
        }

        Debug.Assert(partyIndex != -1 && moveIndex != -1, "Unable to find pokemon with the move " + name);

        OpenParty();
        ChooseMenuItem(partyIndex);
        ChooseMenuItem(moveIndex);
        ClearText();
        CurrentMenuType = MenuType.None;
    }

    public void UseRegisteredItem() {
        RunUntil("OWPlayerInput");
        InjectOverworld(Joypad.Select);
        AdvanceFrame(Joypad.Select);
        Hold(Joypad.Select, "OWPlayerInput");
    }

    public void Deposit(params string[] pokemon) {
        ChooseMenuItem(0);
        ClearText();
        ChooseMenuItem(1);
        Press(Joypad.None); // init
        foreach(string mon in pokemon) {
            int index = FindPokemon(mon);
            UpDownScroll(CpuRead("wBillsPC_CursorPosition"), index, CpuRead("wBillsPC_NumMonsInBox"), false);
            Press(Joypad.None); // whatsup
            Press(Joypad.None); // submenu
            Press(Joypad.A);
            Press(Joypad.None); // init
        }
        Press(Joypad.B);
        Press(Joypad.None);
        Press(Joypad.None);

        CurrentMenuType = MenuType.PC;
    }
}