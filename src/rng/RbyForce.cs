using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public enum MenuType {

    None,
    Party,
    Bag,
    Options,
    Fight,
    StartMenu,
    Mart,
    ItemsPocket,
    BallsPocket,
    KeyItemsPocket,
    TMHMPocket,
    PC,
}

public class RbyTurn {

    public string Move;
    public string Pokemon;
    public int Flags;
    public string Target;

    public static int DefaultRoll = 39;

    public RbyTurn(string move, int flags = 0, string target = "") {
        Move = move;
        Flags = flags;
        Target = target;

        if((Flags & 0x3f) == 0) {
            Flags |= DefaultRoll;
        }
    }

    public RbyTurn(string item, string pokemon, string target = "") {
        Move = item;
        Pokemon = pokemon;
        Target = target;
    }
}

public class RbyForce : Rby {

    // forceturn flags
    public const int Miss = 0x40;
    public const int Crit = 0x80;
    public const int SideEffect = 0x100;
    public const int Hitself = 0x200;
    public const int AiItem = 0x400;
    public const int Switch = AiItem;
    public const int Turns = 0x800;
    public const int ThreeTurn = 3 * Turns;

    // wOptions flags
    public const int Fast = 0x1;
    public const int Medium = 0x3;
    public const int Slow = 0x5;
    public const int On = 0x0;
    public const int Off = 0x80;
    public const int Shift = 0x0;
    public const int Set = 0x40;

    public MenuType CurrentMenuType = MenuType.None;

    private StateCacher StateCacher;
    private int NumSpriteSlots;

    public RbyForce(string rom, SpeedupFlags speedupFlags) : base(rom, null, speedupFlags) {
        NumSpriteSlots = IsYellow ? 15 : 16;
        StateCacher = new StateCacher(GetType().Name);
    }

    public void CacheState(string name, System.Action fn) {
        StateCacher.CacheState(this, name, fn);
    }

    public void ClearCache() {
        StateCacher.ClearCache();
    }

    public void ForceGiftDVs(ushort dvs) {
        RunUntil("_AddPartyMon.next4");
        A = dvs >> 8;
        B = dvs & 0xff;
    }

    public void ForceEncounter(Action action, int slotIndex, ushort dvs) {
        byte[] slots = new byte[] {
            0, 51, 102, 141, 166, 191, 216, 229, 242, 253
        };

        Inject((Joypad) action);
        CpuWrite("wGrassRate", 0xff);
        CpuWrite("wWaterRate", 0xff);
        Hold((Joypad) action, SYM["TryDoWildEncounter.CanEncounter"] + 3);
        A = 0x00;

        RunUntil(SYM["TryDoWildEncounter.CanEncounter"] + 8);
        A = slots[slotIndex];

        RunUntil(SYM["LoadEnemyMonData.storeDVs"]);
        A = dvs >> 8;
        B = dvs & 0xff;
    }

    public void ForceYoloball(string ballname) {
        ClearText();
        BattleMenu(0, 1);
        ChooseListItem(FindItem(ballname));
        RunUntil(SYM["ItemUseBall.loop"] + 0x8);
        A = 1;
    }

    public void ForceSafariYoloball() {
        ClearText();
        MenuPress(Joypad.A);
        RunUntil(SYM["ItemUseBall.loop"] + 0x8);
        A = 1;
    }

    public void ForceTurn(RbyTurn playerTurn, RbyTurn enemyTurn = null, bool speedTieWin = true) {
        bool useItem = Items[playerTurn.Move] != null;
        if(useItem) {
            if(playerTurn.Pokemon != null) UseItem(playerTurn.Move, playerTurn.Pokemon, playerTurn.Target);
            else UseItem(playerTurn.Move, playerTurn.Flags);
        } else if((playerTurn.Flags & Switch) != 0) {
            BattleMenu(1,0);
            ChooseMenuItem(FindPokemon(playerTurn.Move));
            ChooseMenuItem(0);
        } else if(EnemyMon.UsingTrappingMove) {
            if(CpuRead("wTopMenuItemX") != 0x9 || CpuRead("wCurrentMenuItem") != 0)
                MenuPress(Joypad.Left | Joypad.Up);
        } else if(!BattleMon.ThrashingAbout && !BattleMon.Invulnerable) {
            if(CurrentMenuType != MenuType.Fight) BattleMenu(0, 0);

            int moveIndex = FindBattleMove(playerTurn.Move);

            // Reusing 'ChooseMenuItem' code, because the final AdvanceFrame advances past 'SelectEnemyMove.done', 
            // and I don't have a good solution for this problem right now.
            var scroll = CalcMenuScroll(moveIndex);
            for(int i = 0; i < scroll.Amount; i++) {
                MenuPress(scroll.Direction);
            }

            if((CpuRead("hJoyLast") & (byte)Joypad.A) != 0) Press(Joypad.None);
            Inject(Joypad.A);
        }

        if(!(EnemyMon.StoringEnergy | EnemyMon.ChargingUp | EnemyMon.UsingRage | EnemyMon.Frozen | EnemyMon.UsingTrappingMove)) {
            Hold(Joypad.A, SYM["SelectEnemyMove.done"]);
            A = enemyTurn != null ? Moves[enemyTurn.Move].Id : 0;
        }

        bool playerFirst;
        int speedtie = Hold(Joypad.A, SYM["MainInBattleLoop.speedEqual"] + 9, SYM["MainInBattleLoop.enemyMovesFirst"], SYM["MainInBattleLoop.playerMovesFirst"]);
        if(speedtie == SYM["MainInBattleLoop.enemyMovesFirst"]) playerFirst = false;
        else if(speedtie == SYM["MainInBattleLoop.playerMovesFirst"]) playerFirst = true;
        else {
            A = speedTieWin ? 0x00 : 0xff;
            playerFirst = speedTieWin;
        }

        if(playerFirst) {
            if(!useItem) ForceTurnInternal(playerTurn);
            else RunUntil(SYM["MainInBattleLoop.playerMovesFirst"] + 6);
            if(enemyTurn != null) ForceTurnInternal(enemyTurn);
        } else {
            Debug.Assert(enemyTurn != null, "No enemy turn was specified even though the opponent moved first!");
            ForceTurnInternal(enemyTurn);
            if(!useItem) ForceTurnInternal(playerTurn);
        }

        CurrentMenuType = MenuType.None;

        // Semi-terrible code to get around thrash. TODO: fix
        if(BattleMon.ThrashingAbout) {
            if(EnemyMon.HP == 0) {
                if(EnemyParty.Where(mon => mon.HP > 0).Count() > 1) ClearTextUntil(Joypad.None, SYM["PlayCry"]);
                else ClearText();
            }
        } else {
            ClearText();
        }
    }

    private void ForceTurnInternal(RbyTurn turn) {
        int random = SYM["Random"] + 0x10;
        int playerTurnDone1 = SYM["MainInBattleLoop.playerMovesFirst"] + 0x3;
        int playerTurnDone2 = SYM["MainInBattleLoop.AIActionUsedEnemyFirst"] + 0xc;
        int playerTurnDone3 = SYM["HandlePlayerMonFainted"];
        int enemyTurnDone1 = SYM["MainInBattleLoop.enemyMovesFirst"] + 0x11;
        int enemyTurnDone2 = SYM["MainInBattleLoop.playerMovesFirst"] + 0x27;
        int enemyTurnDone3 = SYM["HandleEnemyMonFainted"];
        int enemyTurnDone4 = SYM["MainInBattleLoop.AIActionUsedPlayerFirst"];

        int ret;
        Joypad holdButton = Joypad.None;
        if((CpuRead("wOptions") & 0x7) != 1) holdButton = Joypad.A;

        string[] sideEffects = {
            "FreezeBurnParalyzeEffect",
            "StatModifierDownEffect",
            "PoisonEffect",
            "ConfusionSideEffect",
            "FlinchSideEffect"
        };

        while((ret = ClearTextUntil(holdButton, random, playerTurnDone1, playerTurnDone2, playerTurnDone3, enemyTurnDone1, enemyTurnDone2, enemyTurnDone3, enemyTurnDone4)) == random) {
            int addr = CpuReadLE<ushort>(SP);
            if(addr > 0x4000) addr |= CpuRead("hLoadedROMBank") << 16;
            string address = SYM[addr];
            RunUntil(addr);
            if(!address.StartsWith("VBlank")) {
                if(address.StartsWith("MoveHitTest")) { // hit/miss
                    A = (turn.Flags & Miss) > 0 ? 0xff : 0x00;
                } else if(address.StartsWith("CriticalHitTest")) { // crit
                    A = (turn.Flags & Crit) > 0 ? 0x00 : 0xff;
                } else if(address.StartsWith("RandomizeDamage")) { // damage roll
                    int roll = turn.Flags & 0x3f;
                    if(roll < 1) roll = 1;
                    if(roll > 39) roll = 39;
                    roll += 216;
                    A = (byte) ((roll << 1) | (roll >> 7)); // rotate left to counter a rrca instruction
                } else if(address == "StatModifierDownEffect+0021") { // AI's 25% chance to miss
                    A = (turn.Flags & Miss) > 0 ? 0x00 : 0xff;
                } else if(sideEffects.Any(effect => address.StartsWith(effect))) { // various side effects
                    A = (turn.Flags & SideEffect) > 0 ? 0x00 : 0xff;
                } else if(address.StartsWith("TrainerAI")) {  // trainer ai
                    A = (turn.Flags & AiItem) > 0 ? 0x00 : 0xff;
                } else if(address.StartsWith("ThrashPetalDanceEffect")) {  // thresh/petal dance length
                    A = (turn.Flags & ThreeTurn) > 0 ? 0 : 1;
                } else if(address.StartsWith("CheckPlayerStatusConditions.IsConfused") || address.StartsWith("CheckEnemyStatusConditions.IsConfused")) {  // confusion hit through
                    A = (turn.Flags & Hitself) > 0 ? 0xff : 0x00;
                } else if(address.StartsWith("CheckPlayerStatusConditions.ThrashingAboutCheck")) { // confused for 2-5 turns after thrash/petal dance ends
                    A = 0;
                } else if(address.StartsWith("ApplyAttackToPlayerPokemon.loop")) { // psywave damage
                    A = turn.Flags & 0x3f;
                } else if(address.StartsWith("DisableEffect.pickMoveToDisable")) { // disable move
                    A = FindBattleMove(turn.Target) & 0x3;
                } else if(address.StartsWith("DisableEffect.playerTurnNotLinkBattle")) { // disable number of turns (1-8)
                    int turns=turn.Flags/Turns;
                    A = ((turns >= 1 ? turns : 8) - 1) & 0x7;
                } else if(address.StartsWith("BideEffect.bideEffect")) { // bide number of turns (2-3)
                    int turns=turn.Flags/Turns;
                    A = ((turns >= 2 ? turns : 3) - 2) & 0x7;
                } else if(address.StartsWith("TrappingEffect.trappingEffect+000b") || address.StartsWith("TwoToFiveAttacksEffect.setNumberOfHits+000e")) { // not needed
                    A = 0x3;
                } else if(address.StartsWith("TrappingEffect.trappingEffect+0014") || address.StartsWith("TwoToFiveAttacksEffect.setNumberOfHits+0017")) { // multi-attack number of hits (2-5)
                    int turns=turn.Flags/Turns;
                    A = ((turns >= 2 ? turns : 5) - 2) & 0x3;
                } else {
                    Console.WriteLine("Unhandled Random call coming from " + address);
                }
            }
        }
        RunFor(1);
    }

    public void BattleSwitch(string pokemon, RbyTurn enemyTurn) {
        ForceTurn(new RbyTurn(pokemon, Switch), enemyTurn);
    }

    public void ForceCan() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["GymTrashScript.ok"] + 0xb, SYM["GymTrashScript.trySecondLock"] + 0x7);
        B = A;
        ClearText();
    }

    public override void ChooseMenuItem(int target, Joypad direction = Joypad.None) {
        Scroll(CalcMenuScroll(target), Joypad.A | direction);
    }

    public override void SelectMenuItem(int target, Joypad direction = Joypad.None) {
        Scroll(CalcMenuScroll(target), Joypad.Select | direction);
    }

    public override void ChooseListItem(int target, Joypad direction = Joypad.None) {
        Scroll(CalcListScroll(target), Joypad.A | direction);
    }

    public override void SelectListItem(int target, Joypad direction = Joypad.None) {
        Scroll(CalcListScroll(target), Joypad.Select | direction);
    }

    public (Joypad Direction, int Amount) CalcMenuScroll(int target) {
        int ret = RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        bool inStartMenu = CpuReadLE<ushort>(Registers.SP + 6) == (IsYellow ? SYM["RedisplayStartMenu_DoNotDrawStartMenu.loop"] : SYM["RedisplayStartMenu.loop"]) + 0x3;
        int current = CpuRead("wCurrentMenuItem");
        int max = CpuRead("wMaxMenuItem");
        bool wrap = CpuRead("wMenuWrappingEnabled") > 0;

        if(inStartMenu) {
            max--;
            wrap = true;
        }

        return CalcScroll(target, current, max, wrap);
    }

    public (Joypad Direction, int Amount) CalcListScroll(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        return CalcScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);
    }

    public void Scroll((Joypad Direction, int Amount) scroll, Joypad finalInput) {
        for(int i = 0; i < scroll.Amount; i++) MenuPress(scroll.Direction);
        MenuPress(finalInput);
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

    public int MoveTo(RbyTile target, Action preferredDirection = Action.None, params RbyTile[] additionallyBlockedTiles) {
        List<Action> path = Pathfinding.FindPath<RbyMap, RbyTile>(this, Tile, target, preferredDirection, additionallyBlockedTiles);
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

    public void TalkTo(RbyTile target, Action preferredDirection = Action.None, Joypad holdButton = Joypad.None) {
        MoveTo(target, preferredDirection);
        Press(Joypad.A);
        ClearText(holdButton);
    }

    public void PickupItemAt(int map, int x, int y, Action preferredDirection = Action.None) {
        PickupItemAt(Maps[map][x, y], preferredDirection);
    }

    public void PickupItemAt(string map, int x, int y, Action preferredDirection = Action.None) {
        PickupItemAt(Maps[map][x, y], preferredDirection);
    }

    public void PickupItemAt(int targetX, int targetY, Action preferredDirection = Action.None) {
        PickupItemAt(Map[targetX, targetY], preferredDirection);
    }

    public void PickupItemAt(RbyTile target, Action preferredDirection = Action.None) {
        MoveTo(target, preferredDirection, target);
        PickupItem();
    }

    public void CutAt(int map, int x, int y, Action preferredDirection = Action.None) {
        CutAt(Maps[map][x, y], preferredDirection);
    }

    public void CutAt(string map, int x, int y, Action preferredDirection = Action.None) {
        CutAt(Maps[map][x, y], preferredDirection);
    }

    public void CutAt(int targetX, int targetY, Action preferredDirection = Action.None) {
        CutAt(Map[targetX, targetY], preferredDirection);
    }

    public void CutAt(RbyTile target, Action preferredDirection = Action.None) {
        MoveTo(target, preferredDirection);
        Cut();
    }

    public override int Execute(Action[] actions, params (RbyTile Tile, System.Action Function)[] tileCallbacks) {
        CloseMenu();

        int ret = 0;

        bool directionalWarp = false;
        Action previous = Action.None;

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    CpuWrite("wGrassRate", 0);
                    CpuWrite("wWaterRate", 0);

                    Joypad joypad = (Joypad) action;
                    if(IsYellow) joypad |= Joypad.B; // i think this is fine?

                    if(directionalWarp && previous == action) {
                        Inject(joypad);
                    } else {
                        if(CpuReadLE<ushort>(SP) != SYM["JoypadOverworld"] + 0xd) RunUntil(SYM["JoypadOverworld"] + 0xa);

                        Inject(joypad);
                        bool turnframe = CpuRead("wCheckFor180DegreeTurn") == 1;
                        while((ret = Hold(turnframe ? joypad : Joypad.None, SYM["OverworldLoopLessDelay.newBattle"] + 3, SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["DisplayRepelWoreOffText"], SYM["TryWalking"])) == SYM["TryWalking"]) {
                            D = 0x00;
                            E = 0x00;
                            RunFor(1);
                        }
                        if(ret==SYM["DisplayRepelWoreOffText"]) {
                            ClearText();
                        }

                        if(turnframe && (ret == SYM["CollisionCheckOnLand.collision"] || ret == SYM["CollisionCheckOnWater.collision"])) {
                            RunUntil(SYM["JoypadOverworld"]);
                        }
                    }

                    do {
                        RunFor(1);
                        ret = RunUntil(SYM["JoypadOverworld"], SYM["CheckWarpsNoCollisionLoop"] + 0x2d, SYM["PrintLetterDelay"], SYM["TryWalking"]);
                        if(ret == SYM["CheckWarpsNoCollisionLoop"] + 0x2d) {
                            directionalWarp = true;
                            Inject(Joypad.None);
                            break;
                        } else if(ret == SYM["PrintLetterDelay"]) {
                            break;
                        } else if(ret == SYM["TryWalking"]) {
                            D = 0x00;
                            E = 0x00;
                        } else {
                            directionalWarp = false;
                        }
                    } while((CpuRead("wd730") & 0xa0) > 0);
                    break;
                default:
                    base.Execute(new[]{action}, tileCallbacks);
                    break;
            }
            previous = action;
        }

        return ret;
    }

    public new void PickupItem() {
        CloseMenu();
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["PlaySound"]);
        RunUntil("JoypadOverworld");
    }

    public void SetOptions(int newOptions) {
        OpenOptions();

        int newTextSpeed = (newOptions & 0xf);
        int newBattleStyle = newOptions & 0x40;
        int newAnimations = newOptions & 0x80;

        byte curOptions = CpuRead("wOptions");
        int curTextSpeed = (curOptions & 0xf);
        int curBattleStyle = curOptions & 0x40;
        int curAnimations = curOptions & 0x80;

        if(newTextSpeed != curTextSpeed) MenuPress(newTextSpeed > curTextSpeed ? Joypad.Right : Joypad.Left);

        if(newAnimations != curAnimations || newBattleStyle != curBattleStyle) {
            MenuPress(Joypad.Down);
            if(newAnimations != curAnimations) MenuPress(newAnimations > curAnimations ? Joypad.Right : Joypad.Left);

            if(newBattleStyle != curBattleStyle) {
                MenuPress(Joypad.Down);
                MenuPress(newBattleStyle > curBattleStyle ? Joypad.Right : Joypad.Left);
            }
        }

        Debug.Assert(CpuRead("wOptions") == newOptions, "Options were not set correctly!");
    }

    public void Buy(params object[] itemsToBuy) {
        ChooseMenuItem(0);
        ClearText();

        RAMStream stream = From("wItemList");
        byte[] mart = stream.Read(stream.u8());

        for(int i = 0; i < itemsToBuy.Length; i += 2) {
            byte item = Items[itemsToBuy[i].ToString()].Id;
            int quantity = (int) itemsToBuy[i + 1];

            int itemSlot = Array.IndexOf(mart, item);
            Debug.Assert(itemSlot != -1, "Unable to find item " + itemsToBuy[i].ToString() + " in the mart");
            ChooseListItem(itemSlot);
            for(int j = 1; j < quantity; j++) MenuPress(Joypad.Up);
            MenuPress(Joypad.A);
            ClearText();
            MenuPress(Joypad.A);
            ClearText();
        }

        MenuPress(Joypad.B);
        ClearText();

        CurrentMenuType = MenuType.Mart;
    }

    public void Sell(params object[] itemsToSell) {
        ChooseMenuItem(1);
        ClearText();
        Press(Joypad.None); // BAD CONSIDER NOT USING ANY MENUING STRATS FOR COMPARISONS??? SHIT'S ANNOYING

        for(int i = 0; i < itemsToSell.Length; i += 2) {
            string item = itemsToSell[i].ToString();
            int quantity = (int) itemsToSell[i + 1];

            ChooseListItem(FindItem(item));
            if(quantity == 0) MenuPress(Joypad.Down);
            else {
                for(int j = 1; j < quantity; j++) MenuPress(Joypad.Up);
            }
            MenuPress(Joypad.A);
            ClearText();
            MenuPress(Joypad.A);
            ClearText();
        }

        MenuPress(Joypad.B);
        ClearText();

        CurrentMenuType = MenuType.Mart;
    }

    public void OpenStartMenu() {
        if(InBattle || CurrentMenuType == MenuType.StartMenu) {
            return;
        }

        if(CurrentMenuType != MenuType.None) {
            MenuPress(Joypad.B);
        } else if(CurrentMenuType != MenuType.StartMenu) {
            MenuPress(Joypad.Start);
            CurrentMenuType = MenuType.StartMenu;
        }
    }

    public void CloseMenu(Joypad direction = Joypad.None) {
        if(CurrentMenuType != MenuType.None) {
            if(CurrentMenuType == MenuType.Mart) {
                MenuPress(Joypad.B);
                ClearText();
            } else {
                if(CurrentMenuType != MenuType.StartMenu) MenuPress(Joypad.B | direction);
                MenuPress(Joypad.Start);
            }
        }
        CurrentMenuType = MenuType.None;
    }

    public void EndMenu() {
        if(CurrentMenuType != MenuType.None) MenuPress(Joypad.B);
        MenuPress(Joypad.Start);
        CurrentMenuType = MenuType.None;
    }

    private void OpenParty() {
        if(CurrentMenuType == MenuType.Party) return;
        OpenStartMenu();

        if(InBattle) BattleMenu(1, 0);
        else ChooseMenuItem(0 + StartMenuOffset());

        CurrentMenuType = MenuType.Party;
    }

    private void OpenBag() {
        if(CurrentMenuType == MenuType.Bag) return;
        OpenStartMenu();

        if(InBattle) BattleMenu(0, 1);
        else ChooseMenuItem(1 + StartMenuOffset());

        CurrentMenuType = MenuType.Bag;
    }

    public void Save() {
        OpenStartMenu();
        ChooseMenuItem(3 + StartMenuOffset());
        ClearText();
        Yes();

        CurrentMenuType = MenuType.None;
    }

    private void OpenOptions() {
        if(CurrentMenuType == MenuType.Options) return;
        OpenStartMenu();
        ChooseMenuItem(4 + StartMenuOffset());
        CurrentMenuType = MenuType.Options;
    }

    public int StartMenuOffset() {
        return (CpuRead(SYM["wEventFlags"] + 4) >> 5) & 1; // check pokedex obtained flag, TODO: proper event checking
    }

    public int FindPokemon(string mon) {
        RbyPokemon[] party = Party;
        int index = Array.IndexOf(party, party.Where(p => p.Species.Name == mon).First());
        Debug.Assert(index != -1, "Unable to find the pokemon " + mon);
        return index;
    }

    public int FindBattleMove(string move) {
        int index = Array.IndexOf(BattleMon.Moves, Moves[move]);
        Debug.Assert(index != -1, "Unable to find the move " + move);
        return index;
    }

    public int FindItem(string item) {
        int index = Bag.IndexOf(item);
        Debug.Assert(index != -1, "Unable to find the item " + item);
        return index;
    }

    public void PartySwap(int mon1, int mon2) {
        OpenParty();
        ChooseMenuItem(mon1);
        ChooseMenuItem(1);
        ChooseMenuItem(mon2);
    }

    public void PartySwap(string mon1, string mon2) {
        PartySwap(FindPokemon(mon1), FindPokemon(mon2));
    }

    public void ItemSwap(string item1, string item2) {
        ItemSwap(FindItem(item1), FindItem(item2));
    }

    public void ItemSwap(int item1, int item2) {
        OpenBag();
        SelectListItem(item1);
        SelectListItem(item2);
    }

    public void UseItem(string name, int target1 = -1, int target2 = -1) {
        UseItem(Items[name], target1, target2);
    }

    public void UseItem(string name, string target1, string target2 = "") {
        int partyIndex = FindPokemon(target1);
        int slotIndex = -1;
        if(target2 != "") {
            RbyPokemon mon = Party[partyIndex];
            slotIndex = Array.IndexOf(mon.Moves, mon.Moves.Where(m => m != null && m.Name == target2).First());
            Debug.Assert(slotIndex != -1, "Unable to find the move " + target2);
        }
        UseItem(Items[name], partyIndex, slotIndex);
    }

    public void TossItem(string name, int quantity = 1) {
        OpenBag();
        ChooseListItem(FindItem(name));
        ChooseMenuItem(1);
        while(quantity < 1) { // 0 to toss all, -1 to toss all minus one, etc.
            MenuPress(Joypad.Down);
            quantity++;
        }
        while(quantity > 1) {
            MenuPress(Joypad.Up);
            quantity--;
        }
        MenuPress(Joypad.A);
        ClearText();
        Yes();
        ClearText();
    }

    public void UseItem(RbyItem item, int target1 = -1, int target2 = -1) {
        OpenBag();

        ChooseListItem(FindItem(item.Name));

        switch(item.ExecutionPointerLabel) {
            case "ItemUseEvoStone": // Can only be used outside of battle
                ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                RunUntil("Evolution_PartyMonLoop.done");
                break;
            case "ItemUseTMHM": // Can only be used outside of battle
                int numMoves = PartyMon(target1).NumMoves;
                ChooseMenuItem(0); // USE
                ClearText();
                MenuPress(Joypad.A); // Do you want to teach?
                ChooseMenuItem(target1);
                ClearText();
                if(numMoves == 4) {
                    MenuPress(Joypad.A); // Do you want to overwrite?
                    ClearText();
                    ChooseMenuItem(target2); // Which move to overwrite?
                    ClearText();
                }
                break;
            case "ItemUseEscapeRope": // Can only be used outside of battle
                ChooseMenuItem(0); // USE
                RunUntil("DisableLCD");
                RunUntil("JoypadOverworld");
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUseBicycle": // Can only be used outside of battle
                ClearText();
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUsePokedoll": // Can only be used in battle
                ClearText();
                CurrentMenuType = MenuType.None;
                break;
            case "ItemUsePokeflute":
                if(!InBattle) ChooseMenuItem(0); // USE
                ClearText();
                if(!InBattle) ClearText();
                break;
            case "ItemUseVitamin": // Can only be used outside of battle
                if(item.Name == "RARE CANDY") {
                    ChooseMenuItem(0); // USE
                    ChooseMenuItem(target1);
                    ClearText();
                } else {
                    // TODO: Implement
                }
                break;
            case "ItemUsePPRestore":
                if(!InBattle) ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                if(item.Name.Contains("ETHER")) {
                    ClearText();
                    ChooseMenuItem(target2 + 1);
                }
                RunUntil("ManualTextScroll");
                Inject(Joypad.B);
                if(!InBattle) {
                    AdvanceFrame(Joypad.B);
                    RunUntil("Joypad");
                }
                break;
            case "ItemUsePPUp":
                ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                ClearText();
                ChooseMenuItem(target2 + 1);
                ClearText();
                break;
            case "ItemUseSuperRepel":
            case "ItemUseRepel":
                if(!InBattle) ChooseMenuItem(0); // USE
                ClearText();
                break;
            case "ItemUseMedicine":
                if(!InBattle) ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                RunUntil("DoneText");
                Press(Joypad.B);
                break;
            case "ItemUseXAccuracy":
                RunUntil("DoneText");
                Inject(Joypad.B);
                break;
            case "ItemUseXStat": // Can only be used in battle
                RunUntil("DoneText");
                Inject(Joypad.B);
                RunUntil("ManualTextScroll");
                Inject(Joypad.B);
                break;
        }

        if(InBattle) CurrentMenuType = MenuType.None;
    }

    public void MoveSwap(int move1, int move2) {
        if(CurrentMenuType != MenuType.Fight) BattleMenu(0, 0);
        SelectMenuItem(move1);
        SelectMenuItem(move2);
        CurrentMenuType = MenuType.Fight;
    }

    public void MoveSwap(string move1, string move2) {
        MoveSwap(FindBattleMove(move1), FindBattleMove(move2));
    }

    public void TeachLevelUpMove(string moveToOverwrite) {
        TeachLevelUpMove(FindBattleMove(moveToOverwrite));
    }

    public void TeachLevelUpMove(int slot) {
        Yes();
        ClearText();
        ChooseMenuItem(slot);
        ClearText();
    }

    public void SkipLevelUpMove() {
        No();
        ClearText();
        Yes();
        ClearText();
    }

    public void SendOut(string name, Joypad holdButton = Joypad.None) {
        if(CpuRead("wIsInBattle") == 1) Yes(); // Wild encounters ask if you want to send out or run away
        ChooseMenuItem(FindPokemon(name));
        ClearText(holdButton);
    }

    public void Cut() {
        UseOverworldMove("CUT");
        ClearText();
    }

    public void Surf() {
        UseOverworldMove("SURF");
        ClearText();
    }

    public void Strength() {
        UseOverworldMove("STRENGTH");
        ClearText();
    }

    public void Fly(string townName) {
        ushort townFlags = CpuReadLE<ushort>("wTownVisitedFlag");
        List<string> visitedTowns = new List<string>();
        for(int i = 0; i <= 10; i++) {
            if((townFlags & (1 << i)) > 0) visitedTowns.Add(Maps[i].Name);
        }

        Debug.Assert(visitedTowns.Contains(townName), "You do not have the fly location for " + townName + " unlocked yet");

        var scroll = CalcScroll(visitedTowns.IndexOf(townName), 0, visitedTowns.Count() - 1, true);
        // the input has to be inverted as menuing code is reused which assumes pressing down = going down in the list
        Fly(scroll.Input ^ (Joypad.Up | Joypad.Down), scroll.Amount);
    }

    public void Fly(Joypad direction, int amount) {
        UseOverworldMove("FLY");
        for(int i = 0; i < amount; i++) MenuPress(direction);
        MenuPress(Joypad.A);
        RunUntil("DisableLCD");
        RunFor(1);
        RunUntil("DisableLCD");
        while(RunUntil("TryWalking", "Joypad") == SYM["TryWalking"]) {
            D = 0x00;
            E = 0x00;
            RunFor(1);
        }
    }

    public void Dig() {
        UseOverworldMove("DIG");
        RunUntil("DisableLCD");
        RunFor(1);
        RunUntil("DisableLCD");
    }

    public void UseOverworldMove(string name) {
        string[] overworldMoves = {
            "CUT",
            "SURF",
            "STRENGTH",
            "FLY",
            "TELEPORT",
            "DIG",
            "FLASH"
        };

        int partyIndex = -1;
        int moveIndex = -1;
        for(int i = 0; i < PartySize && partyIndex == -1; i++) {
            RbyPokemon partyMon = PartyMon(i);
            moveIndex = 0;
            for(int j = 0; j < 4; j++) {
                RbyMove move = partyMon.Moves[j];
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
        CurrentMenuType = MenuType.None;
    }

    public void Evolve() {
        RunUntil("Evolution_PartyMonLoop.done");
        RunUntil(SYM["JoypadOverworld"] + 0xa);
    }

    public void RunAway() {
        BattleMenu(1, 1);
        ClearText();
    }

    public void FallDown() {
        RunUntil("DisableLCD");
        RunUntil("JoypadOverworld");
    }

    public void ActivateMansionSwitch() {
        MenuPress(Joypad.A);
        ClearText();
    }

    public void BlaineQuiz(Joypad joypad) {
        MenuPress(joypad);
        ClearText();
    }

    public void PushBoulder(Joypad joypad, int pushes = 1) {
        for(int i = 0; i < pushes; ++i) {
            int encounterCheck = SYM["TryDoWildEncounter.CanEncounter"] + 3;
            int boulderPush = SYM["UpdateNPCSprite"] + (IsYellow ? 0x77 : 0x70);
            Inject(joypad);
            while(Hold(joypad, boulderPush, encounterCheck) == encounterCheck) {
                A = 0xff;
                RunFor(1);
            }
            RunFor(1);
        }
    }

    public void Yes() {
        MenuPress(Joypad.A);
    }

    public void No() {
        MenuPress(Joypad.B);
    }
}