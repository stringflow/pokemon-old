using System;
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
}

public class RedBlueForce : RedBlue {

    public static int Miss = 0x40;
    public static int Crit = 0x80;
    public static int Effect = 0x100;
    public static int ThreeTurn = 0x200;
    public static int Hitself = 0x400;

    public MenuType CurrentMenuType = MenuType.None;

    public RedBlueForce(string rom, bool speedup = false) : base(rom, speedup) {
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
        Hold((Joypad) action, SYM["TryDoWildEncounter.CanEncounter"] + 3);
        A = 0x00;

        RunUntil(SYM["TryDoWildEncounter.CanEncounter"] + 8);
        A = slots[slotIndex];

        RunUntil(SYM["LoadEnemyMonData.storeDVs"]);
        A = dvs >> 8;
        B = dvs & 0xff;
    }

    public void ForceYoloball() {
        ClearText();
        BattleMenu(0, 1);
        ChooseListItem(0);
        RunUntil(SYM["ItemUseBall.loop"] + 0x8);
        A = 1;
    }

    public void ForceTurn(RbyTurn playerTurn, RbyTurn enemyTurn = null, bool speedTieWin = true) {
        bool useItem = Items[playerTurn.Move] != null;
        if(useItem) {
            UseItem(playerTurn.Move, playerTurn.Flags);
        } else if(!BattleMon.ThrashingAbout) {
            if(CurrentMenuType != MenuType.Fight) BattleMenu(0, 0);

            int moveIndex = Array.IndexOf(BattleMon.Moves, Moves[playerTurn.Move]);

            // Reusing 'ChooseMenuItem' code, because the final AdvanceFrame advances past 'SelectEnemyMove.done', 
            // and I don't have a good solution for this problem right now.
            var scroll = CalcMenuScroll(moveIndex);
            for(int i = 0; i < scroll.Amount; i++) {
                MenuPress(scroll.Direction);
            }

            if(CpuRead("hJoyLast") == (byte) Joypad.A) Press(Joypad.None);
            Inject(Joypad.A);
        }
        Hold(Joypad.A, SYM["SelectEnemyMove.done"]);
        if(enemyTurn != null) A = Moves[enemyTurn.Move].Id;

        bool playerFirst;
        int speedtie = RunUntil(SYM["MainInBattleLoop.speedEqual"] + 9, SYM["MainInBattleLoop.enemyMovesFirst"], SYM["MainInBattleLoop.playerMovesFirst"]);
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
        int crit = SYM["CriticalHitTest.SkipHighCritical"] + 3;
        int accuracy = SYM["MoveHitTest.doAccuracyCheck"] + 3;
        int damageRoll = SYM["RandomizeDamage.loop"] + 8;
        int ai = SYM["TrainerAI.getpointer"] + 6;
        int freezeBurnParalyze = SYM["FreezeBurnParalyzeEffect.next2"] + 4;
        int poison = SYM["PoisonEffect.sideEffectTest"] + 3;
        int playerConfusion = SYM["CheckPlayerStatusConditions.IsConfused"] + 0x12;
        int enemyConfusion = SYM["CheckEnemyStatusConditions.isConfused"] + 0x12;
        int thrash = SYM["ThrashPetalDanceEffect.thrashPetalDanceEffect"] + 5;
        int psywave = SYM["ApplyAttackToPlayerPokemon.loop"] + 3;
        int playerTurnDone1 = SYM["MainInBattleLoop.playerMovesFirst"] + 3;
        int playerTurnDone2 = SYM["MainInBattleLoop.AIActionUsedEnemyFirst"] + 0xc;
        int playerTurnDone3 = SYM["HandlePlayerMonFainted"];
        int enemyTurnDone1 = SYM["MainInBattleLoop.enemyMovesFirst"] + 0x11;
        int enemyTurnDone2 = SYM["MainInBattleLoop.playerMovesFirst"] + 0x27;
        int enemyTurnDone3 = SYM["HandleEnemyMonFainted"];

        int ret;
        do {
            while((ret = RunUntil(crit, accuracy, damageRoll, ai, freezeBurnParalyze, poison, playerConfusion, enemyConfusion, thrash, psywave, playerTurnDone1, playerTurnDone2, playerTurnDone3, enemyTurnDone1, enemyTurnDone2, enemyTurnDone3, SYM["ManualTextScroll"])) == SYM["ManualTextScroll"]) {
                Joypad joypad = (Joypad) CpuRead("hJoyLast");
                if(joypad == Joypad.None) joypad = Joypad.A;
                joypad ^= (Joypad.A | Joypad.B);
                Inject(joypad);
                RunFor(1);
            }

            if(ret == accuracy) {
                A = (turn.Flags & Miss) != 0 ? 0xff : 0x00;
            } else if(ret == crit) {
                A = (turn.Flags & Crit) != 0 ? 0x00 : 0xff;
            } else if(ret == damageRoll) {
                int roll = turn.Flags & 0x3f;
                if(roll < 1) roll = 1;
                if(roll > 39) roll = 39;
                A = 216 + roll;
            } else if(ret == ai) {
                A = 0xff; // all AI is ignored for now
            } else if(ret == freezeBurnParalyze || ret == poison) {
                A = (turn.Flags & Effect) != 0 ? 0x00 : 0xff;
            } else if(ret == playerConfusion || ret == enemyConfusion) {
                A = (turn.Flags & Hitself) > 0 ? 0xff : 0x00;
            } else if(ret == thrash) {
                A = (turn.Flags & ThreeTurn) > 0 ? 0 : 1;
            } else if(ret == psywave) {
                A = turn.Flags & 0x3f;
            }

            RunFor(1);
        } while(!(ret == playerTurnDone1 || ret == playerTurnDone2 || ret == playerTurnDone3 || ret == enemyTurnDone1 || ret == enemyTurnDone2 || ret == enemyTurnDone3));
    }

    public void ForceCan() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["GymTrashScript.ok"] + 0xb, SYM["GymTrashScript.trySecondLock"] + 0x7);
        B = A;
        ClearText();
    }

    public override void ChooseMenuItem(int target) {
        Scroll(CalcMenuScroll(target), Joypad.A);
    }

    public override void SelectMenuItem(int target) {
        Scroll(CalcMenuScroll(target), Joypad.Select);
    }

    public override void ChooseListItem(int target) {
        Scroll(CalcListScroll(target), Joypad.A);
    }

    public override void SelectListItem(int target) {
        Scroll(CalcListScroll(target), Joypad.Select);
    }

    public (Joypad Direction, int Amount) CalcMenuScroll(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        return CalcScroll(target, CpuRead("wCurrentMenuItem"), CpuRead("wMaxMenuItem"), CpuRead("wMenuWrappingEnabled") > 0);
    }

    public (Joypad Direction, int Amount) CalcListScroll(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        return CalcScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);
    }

    public void Scroll((Joypad Direction, int Amount) scroll, Joypad finalInput) {
        for(int i = 0; i < scroll.Amount; i++) MenuPress(scroll.Direction);
        MenuPress(finalInput);
    }

    public int ClearTextUntilNaive(Joypad x, params int[] addrs) {
        int[] breakpoints = new int[addrs.Length + 1];
        breakpoints[0] = SYM["ManualTextScroll"];
        Array.Copy(addrs, 0, breakpoints, 1, addrs.Length);

        int ret;
        while((ret = RunUntil(breakpoints)) == SYM["ManualTextScroll"]) {
            Joypad joypad = (Joypad) CpuRead("hJoyLast");
            if(joypad == Joypad.None) joypad = Joypad.A;
            joypad ^= (Joypad.A | Joypad.B);
            Inject(joypad);
            RunFor(1);
        }

        return ret;
    }

    // Temporary non-generic pathfinding code as the generic code had too many issues and became a hassle to maintain.
    public List<Action> TempFindPath(RbyTile start, int stepCost, RbyTile destination) {
        Dictionary<RbyTile, int> costs = new Dictionary<RbyTile, int>();
        Dictionary<RbyTile, RbyTile> previousTiles = new Dictionary<RbyTile, RbyTile>();
        Dictionary<RbyTile, Action> previousActions = new Dictionary<RbyTile, Action>();
        Queue<RbyTile> tilesToCheck = new Queue<RbyTile>();

        bool surfing = CpuRead("wWalkBikeSurfState") == 2;

        costs[start] = 0;
        tilesToCheck.Enqueue(start);

        while(tilesToCheck.Count > 0) {
            RbyTile current = tilesToCheck.Dequeue();
            RbyMap currentMap = current.Map;
            RbyTile[] neighbors = current.Neighbors();

            for(int i = 0; i < neighbors.Length; i++) {
                Action action = (Action) (0x10 << i);
                RbyTile neighbor = neighbors[i];

                // connection check
                RbyConnection connection = null;
                if(action == Action.Right && current.X == currentMap.Width * 2 - 1) connection = currentMap.Connections[0];
                if(action == Action.Left && current.X == 0) connection = currentMap.Connections[1];
                if(action == Action.Down && current.Y == currentMap.Height * 2 - 1) connection = currentMap.Connections[2];
                if(action == Action.Up && current.Y == 0) connection = currentMap.Connections[3];

                if(connection != null) {
                    int xd;
                    int yd;
                    if(action == Action.Down || action == Action.Up) {
                        xd = (current.X + connection.XAlignment) & 0xff;
                        yd = connection.YAlignment;
                    } else {
                        xd = connection.XAlignment;
                        yd = (current.Y + connection.YAlignment) & 0xff;
                    }
                    neighbor = currentMap.Game.Maps[connection.MapId][xd, yd];
                }

                if(neighbor == null) continue;

                RbyMap neighborMap = neighbor.Map;

                RbyTile ledgeHopDest = neighbor.Neighbor(action);
                bool ledgeHop = ledgeHopDest != null && current.IsLedgeHop(neighbor, action);
                bool collided = false;

                if(ledgeHop) {
                    neighbor = ledgeHopDest;
                } else {
                    if(neighborMap.Sprites[neighbor.X, neighbor.Y] != null) collided = true;

                    if(neighbor != destination) {
                        foreach(RbyTrainer trainer in neighborMap.Trainers) {
                            if((CpuRead(trainer.EventFlagAddress) & (1 << trainer.EventFlagBit)) == 0) {
                                RbyTile sprite = trainer.Map[trainer.X, trainer.Y];
                                int range = trainer.SightRange;
                                if(neighbor.Y - sprite.Y == 4 && trainer.Direction == Action.Down) range--; // nice bug
                                for(int j = 0; j < range && sprite != null; j++) {
                                    RbyTile next = sprite.Neighbor(trainer.Direction);
                                    if(next == neighbor) collided = true;
                                    sprite = next;
                                }
                            }
                        }
                    }

                    List<int> tilePairCollisions = surfing ? neighborMap.Tileset.TilePairCollisionsWater : neighborMap.Tileset.TilePairCollisionsLand;
                    PermissionSet permissions = surfing ? neighborMap.Tileset.WaterPermissions : neighborMap.Tileset.LandPermissions;

                    if(tilePairCollisions.Contains(current.Collision << 8 | neighbor.Collision)) collided = true;
                    if(!permissions.IsAllowed(neighbor.Collision)) collided = true;
                    if(current.Map.Tileset.Id == 0x16 && current.Collision == 0x1b && action != Action.Down) collided = true;
                    // BIG NOTE: ^ is a hack around the stairs auto pathing you down, currently both the pathfinding and execution routines
                    //           assume there is another 'Down' action on doors (even though it auto paths you), I have yet to find a solution
                    //           to this issue.
                    if(current.Map.Tileset.Id == 7 && (current.Collision == 0x3c || current.Collision == 0x3d || current.Collision == 0x4c || current.Collision == 0x4d)) collided = true;
                    // No spinning tiles for now
                }

                if(neighbor.Collision == 0x24 && neighbor.Map.Tileset.Id == 0x11 && neighbor.Map.Id != 61) collided = false;
                if(collided) continue;

                bool warped = false;
                RbyWarp warp = currentMap.Warps[neighbor.X, neighbor.Y];
                if(neighbor != destination && warp != null) {
                    RbyMap destMap = currentMap.Game.Maps[warp.DestinationMap];
                    if(destMap != null) {
                        RbyWarp destWarp = destMap.Warps[warp.DestinationIndex];
                        neighbor = destMap[destWarp.X, destWarp.Y];
                        warped = true;
                    }
                }

                int cost = stepCost;
                if(ledgeHop) cost = current.LedgeCost();
                else if(warped) cost = 120;

                int newCost = costs[current] + cost;
                if(!costs.ContainsKey(neighbor) || costs[neighbor] > newCost) {
                    costs[neighbor] = newCost;
                    previousTiles[neighbor] = current;
                    previousActions[neighbor] = action;
                    tilesToCheck.Enqueue(neighbor);
                }
            }
        }

        List<Action> actions = new List<Action>();
        RbyTile c = destination;
        if(!costs.ContainsKey(destination)) {
            // Talk to
            bool foundBonkless = false;
            for(int i = 0; i < 4; i++) {
                Action action = (Action) (0x10 << i);
                RbyTile t1 = destination.Neighbor(action);
                if(t1 != null && costs.ContainsKey(t1)) {
                    RbyTile t2 = t1.Neighbor(action);
                    if(t2 != null && costs.ContainsKey(t2)) {
                        if(costs[t1] - costs[t2] == stepCost) {
                            c = t2;
                            actions.Add(action.Opposite());
                            foundBonkless = true;
                            break;
                        }
                    }
                }
            }

            if(!foundBonkless) {
                c = destination.Neighbors().Where(x => x != null && costs.ContainsKey(x)).OrderBy(x => costs[x]).First();
                actions.Add(c.ActionRequired(destination));
            }
        }

        while(c != start) {
            actions.Add(previousActions[c]);
            c = previousTiles[c];
        }

        actions.Reverse();
        return actions;
    }


    public int MoveTo(int map, int x, int y) {
        return MoveTo(Maps[map][x, y]);
    }

    public int MoveTo(string map, int x, int y) {
        return MoveTo(Maps[map][x, y]);
    }

    public override int MoveTo(int targetX, int targetY) {
        return MoveTo(Map[targetX, targetY]);
    }

    public int MoveTo(RbyTile target) {
        RbyTile current = Map[XCoord, YCoord];
        RbyWarp warp = target.Map.Warps[target.X, target.Y];
        bool original = false;
        if(warp != null) {
            original = warp.Allowed;
            warp.Allowed = true;
        }
        List<Action> path = TempFindPath(current, 17, target);
        if(warp != null) {
            warp.Allowed = original;
        }

        return Execute(path.ToArray());
    }

    public void TalkTo(int map, int x, int y) {
        TalkTo(Maps[map][x, y]);
    }

    public void TalkTo(string map, int x, int y) {
        TalkTo(Maps[map][x, y]);
    }

    public void TalkTo(int targetX, int targetY) {
        TalkTo(Map[targetX, targetY]);
    }

    public void TalkTo(RbyTile target) {
        MoveTo(target);
        Press(Joypad.A);
        ClearText();
    }

    public override int Execute(params Action[] actions) {
        CloseMenu();

        int ret = 0;

        int encounterCheck = SYM["TryDoWildEncounter.CanEncounter"] + 3;
        int spriteUpdate = SYM["TryWalking"];

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    Joypad joypad = (Joypad) action;
                    do {
                        RunUntil("JoypadOverworld");
                        Inject(joypad);
                        while(true) {
                            ret = Hold(joypad, SYM["HandleLedges.foundMatch"], SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["OverworldLoopLessDelay.newBattle"] + 3, encounterCheck, spriteUpdate);
                            if(ret == encounterCheck) {
                                A = 0xff;
                                RunFor(1);
                            } else if(ret == spriteUpdate) {
                                D = 0x00;
                                E = 0x00;
                                RunFor(1);
                            } else {
                                break;
                            }
                        }

                        ret = RunUntil("JoypadOverworld");
                    } while(((CpuRead("wd736") & 0x40) != 0) || ((CpuRead("wd736") & 0x2) != 0 && CpuRead("wJoyIgnore") > 0xfc));
                    break;
                case Action.A:
                    Inject(Joypad.A);
                    AdvanceFrame(Joypad.A);
                    ret = Hold(Joypad.A, "JoypadOverworld", "PrintLetterDelay");
                    break;
                case Action.StartB:
                    Press(Joypad.Start, Joypad.B);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.PokedexFlash:
                    Press(Joypad.Start, Joypad.A, Joypad.B, Joypad.Start);
                    ret = RunUntil("JoypadOverworld");
                    break;
                default:
                    Debug.Assert(false, "Unknown Action: {0}", action);
                    break;
            }
        }

        return ret;
    }

    // FAST   MEDIUM   SLOW     0   1   2
    // ON              OFF      0       1
    // SHIFT           SET      0       1
    public void SetOptions(int textSpeed, int animations, int battleStyle) {
        OpenOptions();
        byte options = CpuRead("wOptions");

        int curTextSpeed = (options & 7) >> 1;
        int curAnimations = options & 0x40;
        int curBattleStyle = options & 0x20;

        if(textSpeed != curTextSpeed) MenuPress(textSpeed > curTextSpeed ? Joypad.Right : Joypad.Left);

        if(animations != curAnimations || battleStyle != curBattleStyle) {
            MenuPress(Joypad.Down);
            if(animations != curAnimations) MenuPress(animations > curAnimations ? Joypad.Right : Joypad.Left);

            if(battleStyle != curBattleStyle) {
                MenuPress(Joypad.Down);
                MenuPress(battleStyle > curBattleStyle ? Joypad.Right : Joypad.Left);
            }
        }
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

            ChooseListItem(Bag.IndexOf(item));
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
        if(CurrentMenuType != MenuType.None) {
            MenuPress(Joypad.B);
        } else if(CurrentMenuType != MenuType.StartMenu) {
            MenuPress(Joypad.Start);
            CurrentMenuType = MenuType.StartMenu;
        }
    }

    public void CloseMenu() {
        if(CurrentMenuType != MenuType.None) {
            if(CurrentMenuType == MenuType.Mart) {
                MenuPress(Joypad.B);
                ClearText();
            } else {
                if(CurrentMenuType != MenuType.StartMenu) MenuPress(Joypad.B);
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
        else ChooseMenuItem(1);  // TODO: Pokedex obtained flag check?

        CurrentMenuType = MenuType.Party;
    }

    private void OpenBag() {
        if(CurrentMenuType == MenuType.Bag) return;
        OpenStartMenu();

        if(InBattle) BattleMenu(0, 1);
        else ChooseMenuItem(2);  // TODO: Pokedex obtained flag check?

        CurrentMenuType = MenuType.Bag;
    }

    private void OpenOptions() {
        if(CurrentMenuType == MenuType.Options) return;
        OpenStartMenu();
        ChooseMenuItem(4);  // TODO: Pokedex obtained flag check?
        CurrentMenuType = MenuType.Options;
    }

    public int PartyIndex(string mon) {
        RbyPokemon[] party = Party;
        return Array.IndexOf(party, party.Where(p => p.Species.Name == mon).First());
    }

    public new void PartySwap(int mon1, int mon2) {
        OpenParty();
        ChooseMenuItem(mon1);
        ChooseMenuItem(1);
        ChooseMenuItem(mon2);
    }

    public void PartySwap(string mon1, string mon2) {
        PartySwap(PartyIndex(mon1), PartyIndex(mon2));
    }

    public void ItemSwap(string item1, string item2) {
        ItemSwap(Bag.IndexOf(item1), Bag.IndexOf(item2));
    }

    public new void ItemSwap(int item1, int item2) {
        OpenBag();
        SelectMenuItem(item1);
        SelectMenuItem(item2);
    }

    public void UseItem(string name, int target1 = -1, int target2 = -1) {
        UseItem(Items[name], target1, target2);
    }

    public void UseItem(string name, string target1, string target2 = "") {
        int partyIndex = PartyIndex(target1);
        int slotIndex = -1;
        if(target2 != "") {
            RbyPokemon mon = Party[partyIndex];
            slotIndex = Array.IndexOf(mon.Moves, mon.Moves.Where(m => m != null && m.Name == target2).First());
        }
        UseItem(Items[name], partyIndex, slotIndex);
    }

    public void UseItem(RbyItem item, int target1 = -1, int target2 = -1) {
        OpenBag();

        ChooseListItem(Bag.IndexOf(item));

        switch(item.ExecutionPointerLabel) {
            case "ItemUseEvoStone": // Can only be used outside of battle
                ChooseMenuItem(0); // USE
                ChooseMenuItem(target1);
                Evolve();
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
                RunUntil("ManualTextScroll");
                Inject(Joypad.B);
                if(!InBattle) {
                    AdvanceFrame(Joypad.B);
                    RunUntil("Joypad");
                }
                break;
            case "ItemUseXAccuracy":
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
        MoveSwap(FindMove(move1), FindMove(move2));
    }

    public int FindMove(string move) {
        return BattleMon.Moves.Where(m => m.Name == move).First();
    }

    public void TeachLevelUpMove(int slot) {
        MenuPress(Joypad.A);
        ClearText();
        ChooseMenuItem(slot);
        ClearText();
    }

    public void Cut() {
        UseOverworldMove("CUT");
        ClearText();
        byte direction = CpuRead("wPlayerDirection");
        switch(direction) {
            case 0x1: Execute("R"); break;
            case 0x2: Execute("L"); break;
            case 0x4: Execute("D"); break;
            case 0x8: Execute("U"); break;
        }
    }

    public void Surf() {
        UseOverworldMove("SURF");
        ClearText();
    }

    public void Strength() {
        UseOverworldMove("STRENGTH");
        ClearText();
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

        int partyIndex = 0;
        int moveIndex = 0;
        for(int i = 0; i < PartySize && partyIndex == 0; i++) {
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

        OpenParty();
        ChooseMenuItem(partyIndex);
        ChooseMenuItem(moveIndex);
        CurrentMenuType = MenuType.None;
    }

    public void Evolve() {
        RunUntil("Evolution_PartyMonLoop.done");
    }

    public void RunAway() {
        BattleMenu(1, 1);
        ClearText();
    }

    public void FallDown() {
        RunUntil("DisableLCD");
    }

    public void ActivateMansionSwitch() {
        Press(Joypad.A);
        ClearText();
        Press(Joypad.A);
        ClearText();
    }

    public void UnlockDoor() {
        Press(Joypad.A);
        ClearText();
    }

    public void BlaineQuiz(Joypad joypad) {
        Press(Joypad.A);
        ClearText();
        Press(joypad);
        ClearText();
    }

    public void PushBoulder(Joypad joypad) {
        int encounterCheck = SYM["TryDoWildEncounter.CanEncounter"] + 3;
        while(Hold(joypad, SYM["AnimateBoulderDust"], encounterCheck) == encounterCheck) {
            A = 0xff;
            RunFor(1);
        }
    }
}